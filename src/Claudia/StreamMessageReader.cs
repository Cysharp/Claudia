using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Claudia;

// parser of server-sent events
// https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events

internal class StreamMessageReader
{
    readonly PipeReader reader;
    MessageStreamEventKind currentEvent;

    public StreamMessageReader(Stream stream)
    {
        this.reader = PipeReader.Create(stream);
    }

    public async IAsyncEnumerable<IMessageStreamEvent> ReadMessagesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
    READ_AGAIN:
        var readResult = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

        if (!(readResult.IsCompleted | readResult.IsCanceled))
        {
            var buffer = readResult.Buffer;

            while (TryReadData(ref buffer, out var streamEvent))
            {
                yield return streamEvent;
            }

            reader.AdvanceTo(buffer.Start);
            goto READ_AGAIN;
        }
    }

    [SkipLocalsInit] // optimize stackalloc cost
    bool TryReadData(ref ReadOnlySequence<byte> buffer, [NotNullWhen(true)] out IMessageStreamEvent? streamEvent)
    {
        var reader = new SequenceReader<byte>(buffer);
        Span<byte> tempBytes = stackalloc byte[64]; // alloc temp

        while (reader.TryReadTo(out ReadOnlySequence<byte> line, (byte)'\n', (byte)'\\', advancePastDelimiter: true))
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
                var span = line.IsSingleSegment ? line.FirstSpan : tempBytes;
                if (!line.IsSingleSegment)
                {
                    line.CopyTo(tempBytes);
                }

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

public enum MessageStreamEventKind
{
    // Error(internal, use -1)
    Ping,
    MessageStart,
    MessageDelta,
    MessageStop,
    ContentBlockStart,
    ContentBlockDelta,
    ContentBlockStop
}

public interface IMessageStreamEvent
{
    string Type { get; }
    MessageStreamEventKind TypeKind { get; }
}

public record class Ping : IMessageStreamEvent
{
    [JsonIgnore]
    public MessageStreamEventKind TypeKind => MessageStreamEventKind.Ping;

    [JsonPropertyName("type")]
    public required string Type { get; set; }
}

public record class MessageStart : IMessageStreamEvent
{
    [JsonIgnore]
    public MessageStreamEventKind TypeKind => MessageStreamEventKind.MessageStart;

    [JsonPropertyName("type")]
    public required string Type { get; set; }
    [JsonPropertyName("message")]
    public required MessageStartBody Message { get; set; }
}

public record class MessageDelta : IMessageStreamEvent
{
    [JsonIgnore]
    public MessageStreamEventKind TypeKind => MessageStreamEventKind.MessageDelta;

    [JsonPropertyName("type")]
    public required string Type { get; set; }
}

public record class MessageStop : IMessageStreamEvent
{
    [JsonIgnore]
    public MessageStreamEventKind TypeKind => MessageStreamEventKind.MessageStop;

    [JsonPropertyName("type")]
    public required string Type { get; set; }
}

public record class ContentBlockStart : IMessageStreamEvent
{
    [JsonIgnore]
    public MessageStreamEventKind TypeKind => MessageStreamEventKind.ContentBlockStart;

    [JsonPropertyName("type")]
    public required string Type { get; set; }
    [JsonPropertyName("content_block")]
    public required Content ContentBlock { get; set; }
}

public record class ContentBlockDelta : IMessageStreamEvent
{
    [JsonIgnore]
    public MessageStreamEventKind TypeKind => MessageStreamEventKind.ContentBlockDelta;

    [JsonPropertyName("type")]
    public required string Type { get; set; }
    [JsonPropertyName("index")]
    public required int Index { get; set; }
    [JsonPropertyName("delta")]
    public required Content Delta { get; set; }
}

public record class ContentBlockStop : IMessageStreamEvent
{
    [JsonIgnore]
    public MessageStreamEventKind TypeKind => MessageStreamEventKind.ContentBlockStop;

    [JsonPropertyName("type")]
    public required string Type { get; set; }
    [JsonPropertyName("index")]
    public required int Index { get; set; }
}

public record class MessageStartBody
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Object type.
    /// For Messages, this is always "message".
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Conversational role of the generated message.
    /// This will always be "assistant".
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    /// <summary>
    /// The model that handled the request.
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// The reason that we stopped.
    /// </summary>
    [JsonPropertyName("stop_reason")]
    public required string StopReason { get; init; }

    /// <summary>
    /// Which custom stop sequence was generated, if any.
    /// </summary>
    [JsonPropertyName("stop_sequence")]
    public required string? StopSequence { get; init; }

    /// <summary>
    /// Billing and rate-limit usage.
    /// </summary>
    [JsonPropertyName("usage")]
    public required Usage Usage { get; init; }
}

public record class MessageDeltaBody
{
    [JsonPropertyName("stop_reason")]
    public required string StopReason { get; set; }
    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }
    [JsonPropertyName("usage")]
    public required Usage Usage { get; set; }
}
