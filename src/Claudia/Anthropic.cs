using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Claudia;

// https://docs.anthropic.com/claude/reference/getting-started-with-the-api

public interface IMessages
{
    Task<MessageResponse> CreateAsync(MessageRequest request, RequestOptions? overrideOptions = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<IMessageStreamEvent> CreateStreamAsync(MessageRequest request, RequestOptions? overrideOptions = null, CancellationToken cancellationToken = default);
}

public class RequestOptions
{
    public TimeSpan? Timeout { get; set; }

    public int? MaxRetries { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}

public class Anthropic : IMessages, IDisposable
{
#if NETSTANDARD2_1
    readonly Random random = new Random();
#endif

    readonly HttpClient httpClient;

    public string ApiKey { get; init; } = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? "";

    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(10);

    public int MaxRetries { get; init; } = 2;

    public bool IncludeRequestJsonOnInvalidRequestError { get; init; } = false;

    public bool ConfigureAwait { get; set; } = false;

    public HttpClient HttpClient => httpClient;

    /// <summary>
    /// Create a Message.
    /// Send a structured list of input messages with text and/or image content, and the model will generate the next message in the conversation.
    /// The Messages API can be used for for either single queries or stateless multi-turn conversations.
    /// </summary>
    public IMessages Messages => this;

    public Anthropic()
        : this(new HttpClientHandler(), true)
    {
    }

    public Anthropic(HttpMessageHandler handler)
        : this(handler, true)
    {
    }

    public Anthropic(HttpMessageHandler handler, bool disposeHandler)
    {
        this.httpClient = new HttpClient(handler, disposeHandler);
        this.httpClient.Timeout = System.Threading.Timeout.InfiniteTimeSpan; // ignore use Timeout, control manually because requires override Timeout option per request.
    }

    async Task<MessageResponse> IMessages.CreateAsync(MessageRequest request, RequestOptions? overrideOptions, CancellationToken cancellationToken)
    {
        request.Stream = null;
        using var msg = await SendRequestAsync(request, overrideOptions, cancellationToken).ConfigureAwait(ConfigureAwait);
        var result = await RequestWithAsync(msg, cancellationToken, overrideOptions, static (x, ct, _) => x.Content.ReadFromJsonAsync<MessageResponse>(AnthropicJsonSerialzierContext.Default.Options, ct), null).ConfigureAwait(ConfigureAwait);
        return result!;
    }

    async IAsyncEnumerable<IMessageStreamEvent> IMessages.CreateStreamAsync(MessageRequest request, RequestOptions? overrideOptions, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        request.Stream = true;
        using var msg = await SendRequestAsync(request, overrideOptions, cancellationToken).ConfigureAwait(ConfigureAwait);

#if NETSTANDARD
        using var stream = await msg.Content.ReadAsStreamAsync().ConfigureAwait(ConfigureAwait);
#else
        using var stream = await msg.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(ConfigureAwait);
#endif

        using var reader = new StreamMessageReader(stream, ConfigureAwait);

        await foreach (var item in reader.ReadMessagesAsync(cancellationToken))
        {
            yield return item;
        }
    }

    async Task<HttpResponseMessage> SendRequestAsync(MessageRequest request, RequestOptions? overrideOptions, CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(request, AnthropicJsonSerialzierContext.Default.Options);

        Uri requestUri = ApiEndpoints.Messages;
        if (HttpClient.BaseAddress != null)
        {
            requestUri = new Uri("messages", UriKind.Relative);
        }

        // use ResponseHeadersRead to ignore buffering response.
        var msg = await RequestWithAsync((httpClient, (bytes, requestUri, overrideOptions, ApiKey)), cancellationToken, overrideOptions, static (x, ct, _) =>
        {
            // for retry, create new HttpRequestMessage per request.
            var state = x.Item2;

            var message = new HttpRequestMessage(HttpMethod.Post, state.requestUri);
            message.Headers.Add("x-api-key", state.ApiKey);
            message.Headers.Add("anthropic-version", "2023-06-01");
            message.Headers.Add("Accept", "application/json");

            if (state.overrideOptions?.Headers != null)
            {
                foreach (var item in state.overrideOptions.Headers)
                {
                    message.Headers.Remove(item.Key);
                    message.Headers.Add(item.Key, item.Value);
                }
            }

            message.Content = new ByteArrayContent(state.bytes);
            return x.httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, ct);
        }, static response =>
        {
            // Same logic of official sdk's shouldRetry
            // https://github.com/anthropics/anthropic-sdk-typescript/blob/104562c3c2164d50da105fed6cfb400b118503d0/src/core.ts#L521

            var status = (int)response.StatusCode;
            if (status == 200) return null;

            if (response.Headers.TryGetValues("x-should-retry", out var values))
            {
                foreach (var item in values)
                {
                    if (item == "true") goto SHOUD_RETRY;
                    else if (item == "false") return null;
                }
            }

            // Retry on request timeouts.
            if (status == 408) goto SHOUD_RETRY;

            // Retry on lock timeouts.
            if (status == 409) goto SHOUD_RETRY;

            // Retry on rate limits.
            if (status == 429) goto SHOUD_RETRY;

            // Retry internal errors.
            if (status >= 500) goto SHOUD_RETRY;

            return null;

        SHOUD_RETRY:
            // get retry time from header
            // https://github.com/anthropics/anthropic-sdk-typescript/blob/104562c3c2164d50da105fed6cfb400b118503d0/src/core.ts#L551-L569

            var retryTime = TimeSpan.Zero; // when zero, needs calc exponential backoff

            if (response.Headers.TryGetValues("retry-after-ms", out var retryAfterMs))
            {
                foreach (var item in retryAfterMs)
                {
                    if (double.TryParse(item, out var ms))
                    {
                        retryTime = TimeSpan.FromMilliseconds(ms);
                        break;
                    }
                }
            }
            if (retryTime == TimeSpan.Zero && response.Headers.TryGetValues("retry-after", out var retryAfter))
            {
                foreach (var item in retryAfter)
                {
                    if (double.TryParse(item, out var retryAfterSeconds))
                    {
                        retryTime = TimeSpan.FromSeconds(retryAfterSeconds);
                        break;
                    }
                    else if (DateTime.TryParse(item, out var date))
                    {
                        retryTime = date - DateTime.UtcNow;
                        break;
                    }
                }
            }

            if (!(TimeSpan.Zero <= retryTime && retryTime < TimeSpan.FromSeconds(6)))
            {
                retryTime = TimeSpan.Zero;
            }

            return retryTime;
        }).ConfigureAwait(ConfigureAwait);

        var statusCode = (int)msg.StatusCode;

        switch (statusCode)
        {
            case 200:
                return msg!;
            default:
                using (msg)
                {
                    var shape = await RequestWithAsync(msg, cancellationToken, overrideOptions, static async (x, ct, configureAwait) =>
                    {
                        // for debuggability when fails deserialize(server returns invalid error data), alloc data first.
#if NETSTANDARD
                        var responseData = await x.Content.ReadAsByteArrayAsync().ConfigureAwait(configureAwait);
#else
                        var responseData = await x.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(configureAwait);
#endif
                        try
                        {
                            return JsonSerializer.Deserialize<ErrorResponseShape>(responseData, AnthropicJsonSerialzierContext.Default.Options);
                        }
                        catch
                        {
                            throw new InvalidOperationException($"Response data is invalid error json. Data: + {Encoding.UTF8.GetString(responseData)}");
                        }
                    }, null).ConfigureAwait(ConfigureAwait);

                    var error = shape!.ErrorResponse;
                    var errorMsg = error.Message;
                    var code = (ErrorCode)statusCode;
                    if (code == ErrorCode.InvalidRequestError && IncludeRequestJsonOnInvalidRequestError)
                    {
                        errorMsg += ". Request: " + JsonSerializer.Serialize(request, AnthropicJsonSerialzierContext.Default.Options);
                    }
                    throw new ClaudiaException((ErrorCode)statusCode, error.Type, errorMsg);
                }
        }
    }

    // with Cancel, Timeout, Retry.
    async Task<TResult> RequestWithAsync<TResult, TState>(TState state, CancellationToken cancellationToken, RequestOptions? overrideOptions, Func<TState, CancellationToken, bool, Task<TResult>> func, Func<TResult, TimeSpan?>? shouldRetry)
    {
        var retriesRemaining = (shouldRetry == null) ? 0 : (overrideOptions?.MaxRetries ?? MaxRetries);
        var timeout = overrideOptions?.Timeout ?? Timeout;
    RETRY:
        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            cts.CancelAfter(timeout);

            try
            {
                var result = await func(state, cts.Token, ConfigureAwait).ConfigureAwait(ConfigureAwait);
                if (shouldRetry != null)
                {
                    var retryTime = shouldRetry(result);
                    if (retryTime != null)
                    {
                        if (retriesRemaining > 0)
                        {
                            if (retryTime.Value == TimeSpan.Zero)
                            {
#if NETSTANDARD2_1
                                var rand = random;
#else
                                var rand = Random.Shared;
#endif
                                var sleep = CalculateDefaultRetryTimeoutMillis(rand, retriesRemaining, MaxRetries);
                                await Task.Delay(TimeSpan.FromMilliseconds(sleep), cancellationToken).ConfigureAwait(ConfigureAwait);
                            }
                            else
                            {
                                await Task.Delay(retryTime.Value, cancellationToken).ConfigureAwait(ConfigureAwait);
                            }
                            retriesRemaining--;
                            goto RETRY;
                        }
                    }
                }
                return result;
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(ex.Message, ex, cancellationToken);
                }
                else
                {
                    throw new TimeoutException($"The request was canceled due to the configured Timeout of {Timeout.TotalSeconds} seconds elapsing.", ex);
                }

                throw;
            }
        }
    }

    // same logic of official client
    static double CalculateDefaultRetryTimeoutMillis(Random random, int retriesRemaining, int maxRetries)
    {
        const double initialRetryDelay = 0.5;
        const double maxRetryDelay = 8.0;

        var numRetries = maxRetries - retriesRemaining;

        // Apply exponential backoff, but not more than the max.
        var sleepSeconds = Math.Min(initialRetryDelay * Math.Pow(2, numRetries), maxRetryDelay);

        // Apply some jitter, take up to at most 25 percent of the retry time.
        var jitter = 1 - random.NextDouble() * 0.25;

        return sleepSeconds * jitter * 1000;
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
