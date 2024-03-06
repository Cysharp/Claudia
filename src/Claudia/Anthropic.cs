using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Claudia;

// https://docs.anthropic.com/claude/reference/getting-started-with-the-api

public interface IMessages
{
    Task<MessagesResponse> CreateAsync(MessageRequest request, CancellationToken cancellationToken = default);
}

public class Anthropic : IMessages, IDisposable
{
    internal static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    readonly HttpClient httpClient;

    public required string ApiKey { get; init; }

    /// <summary>
    /// Create a Message.
    /// Send a structured list of input messages with text and/or image content, and the model will generate the next message in the conversation.
    /// The Messages API can be used for for either single queries or stateless multi-turn conversations.
    /// </summary>
    public IMessages Messages => this;

    public Anthropic()
    : this(new HttpClient())
    {
    }

    public Anthropic(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    async Task<MessagesResponse> IMessages.CreateAsync(MessageRequest request, CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(request, DefaultJsonSerializerOptions);

        var message = new HttpRequestMessage(HttpMethod.Post, ApiEndpoints.Messages);
        message.Headers.Add("x-api-key", ApiKey);
        message.Headers.Add("anthropic-version", "2023-06-01");
        message.Headers.Add("Accept", "application/json");
        message.Content = new ByteArrayContent(bytes);

        var msg = await this.httpClient.SendAsync(message);

        var statusCode = (int)msg.StatusCode;

        switch (statusCode)
        {
            case 200:
                return (await msg.Content.ReadFromJsonAsync<MessagesResponse>(DefaultJsonSerializerOptions, cancellationToken).ConfigureAwait(false))!;
            default:
                var shape = await msg.Content.ReadFromJsonAsync<ErrorResponseShape>(DefaultJsonSerializerOptions, cancellationToken).ConfigureAwait(false);
                var error = shape!.ErrorResponse;
                var errorMsg = error.Message;
                var code = (ErrorCode)statusCode;
                if (code == ErrorCode.InvalidRequestError)
                {
                    errorMsg += ". Input: " + JsonSerializer.Serialize(request, DefaultJsonSerializerOptions);
                }
                throw new ClaudiaException((ErrorCode)statusCode, error.Type, errorMsg);
        }
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

    static class ApiEndpoints
    {
        public static readonly Uri Messages = new Uri("https://api.anthropic.com/v1/messages", UriKind.RelativeOrAbsolute);
    }
}



