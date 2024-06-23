using Cysharp.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Claudia;

// parser of server-sent events
// https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events

internal class StreamMessageReader : IDisposable
{
    readonly Utf8StreamReader reader;
    readonly bool configureAwait;
    MessageStreamEventKind currentEvent;

    public StreamMessageReader(Stream stream, bool configureAwait)
    {
        this.reader = new Utf8StreamReader(stream, leaveOpen: true) { ConfigureAwait = configureAwait };
        this.configureAwait = configureAwait;
    }

    public async IAsyncEnumerable<IMessageStreamEvent> ReadMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await reader.LoadIntoBufferAsync(cancellationToken).ConfigureAwait(configureAwait))
        {
            while (reader.TryReadLine(out var line))
            {
                var streamEvent = ParseLine(line);
                if (streamEvent == null) continue;

                yield return streamEvent;

                if (streamEvent.TypeKind == MessageStreamEventKind.MessageStop)
                {
                    yield break;
                }
            }
        }
    }

    IMessageStreamEvent? ParseLine(ReadOnlyMemory<byte> line)
    {
        // line is these kinds
        // event: event_name
        // data: json
        // (empty line)

        var span = line.Span;

        if (span.Length == 0)
        {
            return null; // next.
        }
        else if (span[0] == 'e') // event
        {
            // Parse Event.
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

            return null; // continue
        }
        else if (span[0] == 'd') // data
        {
            // Parse Data.
            var jsonReader = new Utf8JsonReader(span.Slice(6)); // skip data: 
            switch (currentEvent)
            {
                case MessageStreamEventKind.Ping:
                    return JsonSerializer.Deserialize<Ping>(ref jsonReader, AnthropicJsonSerializerContext.Default.Options)!;
                case MessageStreamEventKind.MessageStart:
                    return JsonSerializer.Deserialize<MessageStart>(ref jsonReader, AnthropicJsonSerializerContext.Default.Options)!;
                case MessageStreamEventKind.MessageDelta:
                    return JsonSerializer.Deserialize<MessageDelta>(ref jsonReader, AnthropicJsonSerializerContext.Default.Options)!;
                case MessageStreamEventKind.MessageStop:
                    return JsonSerializer.Deserialize<MessageStop>(ref jsonReader, AnthropicJsonSerializerContext.Default.Options)!;
                case MessageStreamEventKind.ContentBlockStart:
                    return JsonSerializer.Deserialize<ContentBlockStart>(ref jsonReader, AnthropicJsonSerializerContext.Default.Options)!;
                case MessageStreamEventKind.ContentBlockDelta:
                    return JsonSerializer.Deserialize<ContentBlockDelta>(ref jsonReader, AnthropicJsonSerializerContext.Default.Options)!;
                case MessageStreamEventKind.ContentBlockStop:
                    return JsonSerializer.Deserialize<ContentBlockStop>(ref jsonReader, AnthropicJsonSerializerContext.Default.Options)!;
                case (MessageStreamEventKind)(-1):
                    var error = JsonSerializer.Deserialize<ErrorResponseShape>(ref jsonReader, AnthropicJsonSerializerContext.Default.Options);
                    throw new ClaudiaException(error!.ErrorResponse.ToErrorCode(), error.ErrorResponse.Type, error.ErrorResponse.Message);
                default:
                    // unknown event, skip
                    return null;
            }
        }

        return null;
    }

    public void Dispose()
    {
        reader.Dispose();
    }
}
