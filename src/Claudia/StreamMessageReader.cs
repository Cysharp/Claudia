using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Claudia;

// parser of server-sent events
// https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events

internal class StreamMessageReader
{
    readonly PipeReader reader;
    readonly bool configureAwait;
    MessageStreamEventKind currentEvent;

    public StreamMessageReader(Stream stream, bool configureAwait)
    {
        this.reader = PipeReader.Create(stream);
        this.configureAwait = configureAwait;
    }

    public async IAsyncEnumerable<IMessageStreamEvent> ReadMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
    READ_AGAIN:
        var readResult = await reader.ReadAsync(cancellationToken).ConfigureAwait(configureAwait);

        if (!(readResult.IsCompleted | readResult.IsCanceled))
        {
            var buffer = readResult.Buffer;

            while (TryReadData(ref buffer, out var streamEvent))
            {
                yield return streamEvent;
                if (streamEvent.TypeKind == MessageStreamEventKind.MessageStop)
                {
                    yield break;
                }
            }

            reader.AdvanceTo(buffer.Start, buffer.End); // examined is important
            goto READ_AGAIN;
        }
    }

    [SkipLocalsInit] // optimize stackalloc cost
    bool TryReadData(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out IMessageStreamEvent? streamEvent)
    {
        var reader = new SequenceReader<byte>(buffer);
        Span<byte> tempBytes = stackalloc byte[64]; // alloc temp

        while (reader.TryReadTo(out ReadOnlySequence<byte> line, (byte)'\n', advancePastDelimiter: true))
        {
            // line is these kinds
            // event: event_name
            // data: json
            // (empty line)

            if (line.Length == 0)
            {
                continue; // next.
            }
            else if (line.FirstSpan[0] == 'e') // event
            {
                // Parse Event.
                if (!line.IsSingleSegment)
                {
                    line.CopyTo(tempBytes);
                }
                var span = line.IsSingleSegment ? line.FirstSpan : tempBytes.Slice(0, (int)line.Length);

                var first = span[7]; // "event: [c|m|p|e]"

                if (first == 'c') // content_block_start/delta/stop
                {
                    switch (span[23]) // event: content_block_..[]
                    {
                        case (byte)'a': // st[a]rt
                            currentEvent = MessageStreamEventKind.ContentBlockStart;
                            break;
                        case (byte)'o': // st[o]p
                            currentEvent = MessageStreamEventKind.ContentBlockStop;
                            break;
                        case (byte)'l': // de[l]ta
                            currentEvent = MessageStreamEventKind.ContentBlockDelta;
                            break;
                        default:
                            break;
                    }
                }
                else if (first == 'm') // message_start/delta/stop
                {
                    switch (span[17]) // event: message_..[]
                    {
                        case (byte)'a': // st[a]rt
                            currentEvent = MessageStreamEventKind.MessageStart;
                            break;
                        case (byte)'o': // st[o]p
                            currentEvent = MessageStreamEventKind.MessageStop;
                            break;
                        case (byte)'l': // de[l]ta
                            currentEvent = MessageStreamEventKind.MessageDelta;
                            break;
                        default:
                            break;
                    }
                }
                else if (first == 'p')
                {
                    currentEvent = MessageStreamEventKind.Ping;
                }
                else if (first == 'e')
                {
                    currentEvent = (MessageStreamEventKind)(-1);
                }
                else
                {
                    // Unknown Event, Skip.
                    // throw new InvalidOperationException("Unknown Event. Line:" + Encoding.UTF8.GetString(line.ToArray()));
                    currentEvent = (MessageStreamEventKind)(-2);
                }

                continue;
            }
            else if (line.FirstSpan[0] == 'd') // data
            {
                // Parse Data.
                Utf8JsonReader jsonReader;
                if (line.IsSingleSegment)
                {
                    jsonReader = new Utf8JsonReader(line.FirstSpan.Slice(6)); // skip data: 
                }
                else
                {
                    jsonReader = new Utf8JsonReader(line.Slice(6)); // ReadOnlySequence.Slice is slightly slow
                }

                switch (currentEvent)
                {
                    case MessageStreamEventKind.Ping:
                        streamEvent = JsonSerializer.Deserialize<Ping>(ref jsonReader, AnthropicJsonSerialzierContext.Default.Options)!;
                        break;
                    case MessageStreamEventKind.MessageStart:
                        streamEvent = JsonSerializer.Deserialize<MessageStart>(ref jsonReader, AnthropicJsonSerialzierContext.Default.Options)!;
                        break;
                    case MessageStreamEventKind.MessageDelta:
                        streamEvent = JsonSerializer.Deserialize<MessageDelta>(ref jsonReader, AnthropicJsonSerialzierContext.Default.Options)!;
                        break;
                    case MessageStreamEventKind.MessageStop:
                        streamEvent = JsonSerializer.Deserialize<MessageStop>(ref jsonReader, AnthropicJsonSerialzierContext.Default.Options)!;
                        break;
                    case MessageStreamEventKind.ContentBlockStart:
                        streamEvent = JsonSerializer.Deserialize<ContentBlockStart>(ref jsonReader, AnthropicJsonSerialzierContext.Default.Options)!;
                        break;
                    case MessageStreamEventKind.ContentBlockDelta:
                        streamEvent = JsonSerializer.Deserialize<ContentBlockDelta>(ref jsonReader, AnthropicJsonSerialzierContext.Default.Options)!;
                        break;
                    case MessageStreamEventKind.ContentBlockStop:
                        streamEvent = JsonSerializer.Deserialize<ContentBlockStop>(ref jsonReader, AnthropicJsonSerialzierContext.Default.Options)!;
                        break;
                    case (MessageStreamEventKind)(-1):
                        var error = JsonSerializer.Deserialize<ErrorResponseShape>(ref jsonReader, AnthropicJsonSerialzierContext.Default.Options);
                        throw new ClaudiaException(error!.ErrorResponse.ToErrorCode(), error.ErrorResponse.Type, error.ErrorResponse.Message);
                    default:
                        // unknown event, skip
                        goto END;
                }

                buffer = buffer.Slice(reader.Consumed);
                return true;
            }
        }
    END:
        streamEvent = default;
        buffer = buffer.Slice(reader.Consumed);
        return false;
    }
}
