# Claudia

Unofficial [Anthropic Claude API](https://www.anthropic.com/api) client for .NET.

We have built a C# API similar to the official [Python SDK](https://github.com/anthropics/anthropic-sdk-python) and [TypeScript SDK](https://github.com/anthropics/anthropic-sdk-typescript). It supports netstandard2.1, net6.0, and net8.0. If you want to use it in Unity, please reference it from [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).


Installation
---
This library is distributed via NuGet, supporting .NET Standard 2.1, .NET 6(.NET 7) and .NET 8 or above.

> PM> Install-Package [Claudia](https://www.nuget.org/packages/Claudia)

Usage
---
For details about the API, please check the [official API reference](https://docs.anthropic.com/claude/reference/getting-started-with-the-api).

```csharp
using Claudia;

var anthropic = new Anthropic
{
    ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") // This is the default and can be omitted
};

var message = await anthropic.Messages.CreateAsync(new()
{
    Model = "claude-3-opus-20240229", // you can use Claudia.Models.Claude3Opus string constant
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude" }]
});

Console.WriteLine(message);
```

Streaming Messages
---
We provide support for streaming responses using Server Sent Events (SSE).

```csharp
using Claudia;

var anthropic = new Anthropic();

var stream = anthropic.Messages.CreateStreamAsync(new()
{
    Model = "claude-3-opus-20240229",
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude" }]
});

await foreach (var messageStreamEvent in stream)
{
    Console.WriteLine(messageStreamEvent);
}
```

If you need to cancel a stream, you can pass the `CancellationToken` to `CreateStreamAsync`.

Types of MessageStreamEvents are here [IMessageStreamEvent](https://github.com/Cysharp/Claudia/blob/main/src/Claudia/IMessageStreamEvent.cs).

For example, outputs the text results.

```csharp
await foreach (var messageStreamEvent in stream)
{
    if (messageStreamEvent is ContentBlockDelta content)
    {
        Console.WriteLine(content.Delta.Text);
    }
}
```

Request & Response types
---
This library includes C# definitions for all request params and response fields. You may import and use them like so:

```csharp
using Claudia;

var request = new MessageRequest()
{
    Model = Models.Claude3Opus,
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude" }]
};
```

Documentation for each method, request param, and response field are available in docstrings and will appear on hover in most modern editors.

All of MessageRequest definitions are here [MessageRequest.cs](https://github.com/Cysharp/Claudia/blob/main/src/Claudia/MessageRequest.cs) and MessageResponse definitions are here [MessageResponse.cs](https://github.com/Cysharp/Claudia/blob/main/src/Claudia/MessagesResponse.cs).

Counting Tokens
---
You can see the exact usage for a given request through the usage response property, e.g.

```csharp
var message = await anthropic.Messages.CreateAsync(...)

// Usage { InputTokens = 11, OutputTokens = 18 }
Console.WriteLine(message.Usage);
```

Streaming Helpers
---
By integrating with [R3](https://github.com/Cysharp/R3), the new Reactive Extensions library, it becomes possible to handle Streaming Events in various ways.

```csharp
// convert to array.
var array = await stream.ToObservable().ToArrayAsync();

// filterling and execute.
await stream.ToObservable()
    .OfType<IMessageStreamEvent, ContentBlockDelta>()
    .Where(x => x.Delta.Text != null)
    .ForEachAsync(x =>
    {
        Console.WriteLine(x.Delta.Text);
    });

// branching query
var branch = stream.ToObservable().Publish();

var messageStartTask = branch.OfType<IMessageStreamEvent, MessageStart>().FirstAsync();
var messageDeltaTask = branch.OfType<IMessageStreamEvent, MessageDelta>().FirstAsync();

branch.Connect(); // start consume stream

Console.WriteLine((await messageStartTask));
Console.WriteLine((await messageDeltaTask));

// Sum Usage
var totalUsage = await stream.ToObservable()
    .Where(x => x is MessageStart or MessageDelta)
    .Select(x => x switch
    {
        MessageStart ms => ms.Message.Usage,
        MessageDelta delta => delta.Usage,
        _ => throw new ArgumentException()
    })
    .AggregateAsync((x, y) => new Usage { InputTokens = x.InputTokens + y.InputTokens, OutputTokens = x.OutputTokens + y.OutputTokens });

Console.WriteLine(totalUsage);
```

Handling errors
---
When the library is unable to connect to the API, or if the API returns a non-success status code (i.e., 4xx or 5xx response), a subclass of `ClaudiaException` will be thrown:

```csharp
try
{
    var msg = await anthropic.Messages.CreateAsync(new()
    {
        Model = Models.Claude3Opus,
        MaxTokens = 1024,
        Messages = [new() { Role = "user", Content = "Hello, Claude" }]
    });
}
catch (ClaudiaException ex)
{
    Console.WriteLine((int)ex.Status); // 400(ErrorCode.InvalidRequestError)
    Console.WriteLine(ex.Name);        // invalid_request_error
    Console.WriteLine(ex.Message);     // Field required. Input:...
}
```

Error codes are as followed:

```csharp
public enum ErrorCode
{
    /// <summary>There was an issue with the format or content of your request.</summary>
    InvalidRequestError = 400,
    /// <summary>There's an issue with your API key.</summary>
    AuthenticationError = 401,
    /// <summary>Your API key does not have permission to use the specified resource.</summary>
    PermissionError = 403,
    /// <summary>The requested resource was not found.</summary>
    NotFoundError = 404,
    /// <summary>Your account has hit a rate limit.</summary>
    RateLimitError = 429,
    /// <summary>An unexpected error has occurred internal to Anthropic's systems.</summary>
    ApiError = 500,
    /// <summary>Anthropic's API is temporarily overloaded.</summary>
    OverloadedError = 529
}
```

Retries
---
Certain errors will be automatically retried 2 times by default, with a short exponential backoff. Connection errors (for example, due to a network connectivity problem), 408 Request Timeout, 409 Conflict, 429 Rate Limit, and >=500 Internal errors will all be retried by default.

You can use the `MaxRetries` option to configure or disable this:

```csharp
// Configure the default for all requests:
var anthropic = new Anthropic
{
    MaxRetries = 0, // default is 2
};

// Or, configure per-request:
await anthropic.Messages.CreateAsync(new()
{
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude" }],
    Model = "claude-3-opus-20240229"
}, new()
{
    MaxRetries = 5
});
```

Timeouts
---
Requests time out after 10 minutes by default. You can configure this with a `Timeout` option:

```csharp
// Configure the default for all requests:
var anthropic = new Anthropic
{
    Timeout = TimeSpan.FromSeconds(20) // 20 seconds (default is 10 minutes)
};

// Override per-request:
await anthropic.Messages.CreateAsync(new()
{
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude" }],
    Model = "claude-3-opus-20240229"
}, new()
{
    Timeout = TimeSpan.FromSeconds(5)
});
```

On timeout, an `TimeoutException` is thrown.

Note that requests which time out will be [retried twice by default](#retries).

Default Headers
---
TODO:

Advanced Usage
---
TODO:

### Accessing raw Response data (e.g., headers)


Customizing the HttpClient
---
TODO:


Configuring an HTTP(S) Agent (e.g., for proxies)
---
TODO:

License
---
This library is under the MIT License.
