using System.Text.Json.Serialization;

namespace Claudia;

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

    [JsonPropertyName("delta")]
    public required MessageDeltaBody Delta { get; set; }

    [JsonPropertyName("usage")]
    public required Usage Usage { get; set; }
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
    public required string Id { get; set; }

    /// <summary>
    /// Object type.
    /// For Messages, this is always "message".
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Conversational role of the generated message.
    /// This will always be "assistant".
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    /// <summary>
    /// The model that handled the request.
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    /// <summary>
    /// The reason that we stopped.
    /// </summary>
    [JsonPropertyName("stop_reason")]
    public required string StopReason { get; set; }

    /// <summary>
    /// Which custom stop sequence was generated, if any.
    /// </summary>
    [JsonPropertyName("stop_sequence")]
    public required string? StopSequence { get; set; }

    /// <summary>
    /// Billing and rate-limit usage.
    /// </summary>
    [JsonPropertyName("usage")]
    public required Usage Usage { get; set; }
}

public record class MessageDeltaBody
{
    [JsonPropertyName("stop_reason")]
    public required string StopReason { get; set; }
    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }
}