using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using System.Text.Json;

namespace Claudia;

public static class BedrockExtensions
{
    public static Task<InvokeModelResponse> InvokeModelAsync(this AmazonBedrockRuntimeClient client, string modelId, MessageRequest request, CancellationToken cancellationToken = default)
    {
        return client.InvokeModelAsync(new Amazon.BedrockRuntime.Model.InvokeModelRequest
        {
            ModelId = modelId,
            Accept = "application/json",
            ContentType = "application/json",
            Body = Serialize(request, stream: null),
        });
    }

    static MemoryStream Serialize(MessageRequest request, bool? stream)
    {
        var ms = new MemoryStream();
        var model = ConvertToBedrockModel(request, stream);

        JsonSerializer.Serialize(ms, model, BedrockAnthropicJsonSerialzierContext.Options);

        ms.Flush();
        ms.Position = 0;
        return ms;
    }

    static BedrockMessageRequest ConvertToBedrockModel(MessageRequest request, bool? stream)
    {
        return new BedrockMessageRequest
        {
            Model = request.Model,
            MaxTokens = request.MaxTokens,
            Messages = request.Messages,
            Metadata = request.Metadata,
            StopSequences = request.StopSequences,
            System = request.System,
            Temperature = request.Temperature,
            TopK = request.TopK,
            TopP = request.TopP,
            Stream = stream
        };
    }
}
