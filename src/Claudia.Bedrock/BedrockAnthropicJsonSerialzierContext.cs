using System.Text.Json;
using System.Text.Json.Serialization;

namespace Claudia;

internal static class BedrockAnthropicJsonSerialzierContext
{
    public static JsonSerializerOptions Options { get; }

    static BedrockAnthropicJsonSerialzierContext()
    {
        var options = new JsonSerializerOptions(InternalBedrockAnthropicJsonSerialzierContext.Default.Options);
        options.TypeInfoResolverChain.Add(AnthropicJsonSerialzierContext.Default.Options.TypeInfoResolver!);
        options.MakeReadOnly();

        Options = options;
    }
}

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(BedrockMessageRequest))]
internal partial class InternalBedrockAnthropicJsonSerialzierContext : JsonSerializerContext
{
}

// "model" -> "anthropic_version"
internal record class BedrockMessageRequest
{
    /// <summary>
    /// The model that will complete your prompt.
    /// </summary>
    // [JsonPropertyName("model")]
    [JsonPropertyName("anthropic_version")]
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
}