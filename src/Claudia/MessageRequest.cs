using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Claudia;

// https://docs.anthropic.com/claude/reference/messages_post
public record class MessageRequest
{
    /// <summary>
    /// The model that will complete your prompt.
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    /// <summary>
    /// The maximum number of tokens to generate before stopping.
    /// Note that our models may stop before reaching this maximum.This parameter only specifies the absolute maximum number of tokens to generate.
    /// Different models have different maximum values for this parameter
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public required int MaxTokens { get; set; }

    /// <summary>
    /// Input messages.
    /// </summary>
    [JsonPropertyName("messages")]
    public required Message[] Messages { get; set; }

    // optional parameters

    /// <summary>
    /// System prompt.
    /// A system prompt is a way of providing context and instructions to Claude, such as specifying a particular goal or role.
    /// </summary>
    [JsonPropertyName("system")]
    public string? System { get; set; }

    /// <summary>
    /// An object describing metadata about the request.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Metadata? Metadata { get; set; }

    /// <summary>
    /// Custom text sequences that will cause the model to stop generating.
    /// Our models will normally stop when they have naturally completed their turn, which will result in a response stop_reason of "end_turn".
    /// If you want the model to stop generating when it encounters custom strings of text, you can use the stop_sequences parameter.If the model encounters one of the custom sequences, the response stop_reason value will be "stop_sequence" and the response stop_sequence value will contain the matched stop sequence.
    /// </summary>
    [JsonPropertyName("stop_sequences")]
    public string[]? StopSequences { get; set; }

    /// <summary>
    /// Whether to incrementally stream the response using server-sent events.
    /// </summary>
    [JsonPropertyName("stream")]
    [JsonInclude] // internal so requires Include.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    internal bool? Stream { get; set; }

    /// <summary>
    /// Amount of randomness injected into the response.
    /// Defaults to 1.0. Ranges from 0.0 to 1.0. Use temperature closer to 0.0 for analytical / multiple choice, and closer to 1.0 for creative and generative tasks.
    /// Note that even with temperature of 0.0, the results will not be fully deterministic.
    /// </summary>
    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? Temperature { get; set; }

    /// <summary>
    /// Use nucleus sampling.
    /// In nucleus sampling, we compute the cumulative distribution over all the options for each subsequent token in decreasing probability order and cut it off once it reaches a particular probability specified by top_p.You should either alter temperature or top_p, but not both.
    /// Recommended for advanced use cases only. You usually only need to use temperature.
    /// </summary>
    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? TopP { get; set; }

    /// <summary>
    /// Only sample from the top K options for each subsequent token.
    /// Used to remove "long tail" low probability responses.
    /// Recommended for advanced use cases only. You usually only need to use temperature.
    /// </summary>
    [JsonPropertyName("top_k")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double? TopK { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, AnthropicJsonSerialzierContext.Default.Options);
    }
}
public record class Message
{
    /// <summary>
    /// user or assistant.
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; set; }

    /// <summary>
    /// single string or an array of content blocks.
    /// </summary>
    [JsonPropertyName("content")]
    public required Contents Content { get; set; }
}

public class Contents : Collection<Content>
{
    public static implicit operator Contents(string text)
    {
        var content = new Content
        {
            Type = ContentTypes.Text,
            Text = text
        };
        return new Contents { content };
    }
}

public record class Content
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    // Text or Source

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("source")]
    public Source? Source { get; set; }

    public static implicit operator Content(string text) => new Content(text);

    public Content()
    {
    }

    [SetsRequiredMembers]
    public Content(string text)
    {
        Type = ContentTypes.Text;
        Text = text;
    }

    [SetsRequiredMembers]
    public Content(ReadOnlyMemory<byte> data, string mediaType)
    {
        Type = ContentTypes.Image;
        Source = new Source
        {
            Type = "base64",
            MediaType = mediaType,
            Data = data
        };
    }
}

public record class Metadata
{
    /// <summary>
    /// An external identifier for the user who is associated with the request.
    /// This should be a uuid, hash value, or other opaque identifier.Anthropic may use this id to help detect abuse. Do not include any identifying information such as name, email address, or phone number.
    /// </summary>
    [JsonPropertyName("user_id")]
    public required string UserId { get; set; }
}

public record class Source
{
    /// <summary>
    /// We currently support the base64 source type for images.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "base64";

    /// <summary>
    /// We currently support the base64 source the image/jpeg, image/png, image/gif, and image/webp media types.
    /// </summary>
    [JsonPropertyName("media_type")]
    public required string MediaType { get; set; }

    [JsonPropertyName("data")]
    public required ReadOnlyMemory<byte> Data { get; set; } // Base64
}
