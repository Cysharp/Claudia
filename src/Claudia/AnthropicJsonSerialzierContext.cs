using System.Text.Json.Serialization;

namespace Claudia;

// WhenWritingNull doesn't affect Nullable<T>
// so annnotate [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] to each fields
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(MessageRequest))]
[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(Contents))]
[JsonSerializable(typeof(Content))]
[JsonSerializable(typeof(Metadata))]
[JsonSerializable(typeof(Source))]
[JsonSerializable(typeof(MessageResponse))]
[JsonSerializable(typeof(Usage))]
[JsonSerializable(typeof(ErrorResponseShape))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(Ping))]
[JsonSerializable(typeof(MessageStart))]
[JsonSerializable(typeof(MessageDelta))]
[JsonSerializable(typeof(MessageStop))]
[JsonSerializable(typeof(ContentBlockStart))]
[JsonSerializable(typeof(ContentBlockDelta))]
[JsonSerializable(typeof(ContentBlockStop))]
[JsonSerializable(typeof(MessageStartBody))]
[JsonSerializable(typeof(MessageDeltaBody))]
internal partial class AnthropicJsonSerialzierContext : JsonSerializerContext
{
}