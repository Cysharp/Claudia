using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Claudia;

public class BedrockAnthropicClient(IAmazonBedrockRuntime client, string modelId) : IMessages
{
    public IMessages Messages => this;

    // currently overrideOptions is not yet supported.
    async Task<MessageResponse> IMessages.CreateAsync(MessageRequest request, RequestOptions? overrideOptions, CancellationToken cancellationToken)
    {
        var response = await client.InvokeModelAsync(modelId, request, cancellationToken);
        return response.GetMessageResponse();
    }

    async IAsyncEnumerable<IMessageStreamEvent> IMessages.CreateStreamAsync(MessageRequest request, RequestOptions? overrideOptions, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var response = await client.InvokeModelWithResponseStreamAsync(modelId, request, cancellationToken);
        await foreach (var item in response.GetMessageResponseAsync(cancellationToken))
        {
            yield return item;
        }
    }
}

public static class BedrockExtensions
{
    public static BedrockAnthropicClient UseAnthropic(this IAmazonBedrockRuntime client, string modelId)
    {
        return new BedrockAnthropicClient(client, modelId);
    }

    public static Task<InvokeModelResponse> InvokeModelAsync(this IAmazonBedrockRuntime client, string modelId, MessageRequest request, CancellationToken cancellationToken = default)
    {
        return client.InvokeModelAsync(new Amazon.BedrockRuntime.Model.InvokeModelRequest
        {
            ModelId = modelId,
            Accept = "application/json",
            ContentType = "application/json",
            Body = Serialize(request),
        }, cancellationToken);
    }

    public static Task<InvokeModelWithResponseStreamResponse> InvokeModelWithResponseStreamAsync(this IAmazonBedrockRuntime client, string modelId, MessageRequest request, CancellationToken cancellationToken = default)
    {
        return client.InvokeModelWithResponseStreamAsync(new Amazon.BedrockRuntime.Model.InvokeModelWithResponseStreamRequest
        {
            ModelId = modelId,
            Accept = "application/json",
            ContentType = "application/json",
            Body = Serialize(request),
        }, cancellationToken);
    }

    public static MessageResponse GetMessageResponse(this InvokeModelResponse response)
    {
        if ((int)response.HttpStatusCode == 200)
        {
            return JsonSerializer.Deserialize<MessageResponse>(response.Body, AnthropicJsonSerializerContext.Default.Options)!;
        }
        else
        {
            var shape = JsonSerializer.Deserialize<ErrorResponseShape>(response.Body, AnthropicJsonSerializerContext.Default.Options)!;

            var error = shape!.ErrorResponse;
            var errorMsg = error.Message;
            var code = (ErrorCode)response.HttpStatusCode;
            throw new ClaudiaException(code, error.Type, errorMsg);
        }
    }

    public static IAsyncEnumerable<IMessageStreamEvent> GetMessageResponseAsync(this InvokeModelWithResponseStreamResponse response, CancellationToken cancellationToken = default)
    {
        return ResponseStreamReader.ToAsyncEnumerable(response.Body, cancellationToken);
    }

    static MemoryStream Serialize(MessageRequest request)
    {
        var ms = new MemoryStream();
        var model = ConvertToBedrockModel(request);

        JsonSerializer.Serialize(ms, model, BedrockAnthropicJsonSerializerContext.Options);

        ms.Flush();
        ms.Position = 0;
        return ms;
    }

    static BedrockMessageRequest ConvertToBedrockModel(MessageRequest request)
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
        };
    }
}
