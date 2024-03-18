using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using System.Text.Json;

namespace Claudia;

public class BedrockAnthropicClient(AmazonBedrockRuntimeClient client, string modelId) : IMessages
{
    public IMessages Messages => this;

    // currently overrideOptions is not yet supported.
    async Task<MessageResponse> IMessages.CreateAsync(MessageRequest request, RequestOptions? overrideOptions, CancellationToken cancellationToken)
    {
        var response = await client.InvokeModelAsync(modelId, request, cancellationToken);
        return response.GetMessageResponse();
    }

    IAsyncEnumerable<IMessageStreamEvent> IMessages.CreateStreamAsync(MessageRequest request, RequestOptions? overrideOptions, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

public static class BedrockExtensions
{
    public static BedrockAnthropicClient UseAnthropic(this AmazonBedrockRuntimeClient client, string modelId)
    {
        return new BedrockAnthropicClient(client, modelId);
    }

    public static Task<InvokeModelResponse> InvokeModelAsync(this AmazonBedrockRuntimeClient client, string modelId, MessageRequest request, CancellationToken cancellationToken = default)
    {
        return client.InvokeModelAsync(new Amazon.BedrockRuntime.Model.InvokeModelRequest
        {
            ModelId = modelId,
            Accept = "application/json",
            ContentType = "application/json",
            Body = Serialize(request, stream: null),
        }, cancellationToken);
    }

    public static MessageResponse GetMessageResponse(this InvokeModelResponse response)
    {
        if ((int)response.HttpStatusCode == 200)
        {
            return JsonSerializer.Deserialize<MessageResponse>(response.Body, AnthropicJsonSerialzierContext.Default.Options)!;
        }
        else
        {
            var shape = JsonSerializer.Deserialize<ErrorResponseShape>(response.Body, AnthropicJsonSerialzierContext.Default.Options)!;

            var error = shape!.ErrorResponse;
            var errorMsg = error.Message;
            var code = (ErrorCode)response.HttpStatusCode;
            throw new ClaudiaException(code, error.Type, errorMsg);
        }
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
