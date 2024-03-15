# Claudia

Unofficial [Anthropic Claude API](https://www.anthropic.com/api) client for .NET.

We have built a C# API similar to the official [Python SDK](https://github.com/anthropics/anthropic-sdk-python) and [TypeScript SDK](https://github.com/anthropics/anthropic-sdk-typescript). It supports netstandard2.1, net6.0, and net8.0. If you want to use it in Unity, please reference it from [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).

In addition to the pure client SDK, it also includes a C# Source Generator for performing Function Calling, similar to [anthropic-tools](https://github.com/anthropics/anthropic-tools/).

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

Claudia is designed to have a similar look and feel to the official client, particularly the TypeScript SDK. However, it does not use `object`, `dynamic`, or `Dictionary`, and everything is strongly typed. By utilizing [C# 9.0 Target-typed new expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/target-typed-new) and [C# 12 Collection expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-12.0/collection-expressions), it can be written in a simple manner.

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

The Stream returns an `IAsyncEnumerable<IMessageStreamEvent>`, allowing it to be enumerated using `await foreach`. The implementation types of `IMessageStreamEvent` can be found in [IMessageStreamEvent.cs](https://github.com/Cysharp/Claudia/blob/main/src/Claudia/IMessageStreamEvent.cs).

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
    Messages = [new() { Role = Roles.User, Content = "Hello, Claude" }]
};
```

Documentation for each method, request param, and response field are available in docstrings and will appear on hover in most modern editors.

All of MessageRequest definitions are here [MessageRequest.cs](https://github.com/Cysharp/Claudia/blob/main/src/Claudia/MessageRequest.cs) and MessageResponse definitions are here [MessageResponse.cs](https://github.com/Cysharp/Claudia/blob/main/src/Claudia/MessagesResponse.cs).

Also, commonly used constants are defined. For example, `Models.Claude3Opus` is `claude-3-opus-20240229`, and constants like `Roles.User` and `Roles.Assistant` are used for roles like "user" and "assistant". Please refer to [Constant.cs](https://github.com/Cysharp/Claudia/blob/main/src/Claudia/Constant.cs) for all the constants. In addition, the [system prompt used in Claude's official chat UI](https://clutwitter.com/AmandaAskell/status/1765207842993434880) is defined as `SystemPrompts.Claude3`.

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
We automatically send the `anthropic-version` header set to `2023-06-01`.

If you need to, you can override it by setting default headers on a per-request basis.

Be aware that doing so may result in incorrect types and other unexpected or undefined behavior in the SDK.

```csharp
await anthropic.Messages.CreateAsync(new()
{
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude" }],
    Model = "claude-3-opus-20240229"
}, new()
{
    Headers = new() { { "anthropic-version", "My-Custom-Value" } }
});
```

Customizing the HttpClient
---
The Anthropic client uses a standard HttpClient by default for communication. If you want to customize the behavior of the HttpClient, pass an HttpMessageHandler. Additionally, if you don't want to dispose the HttpClient when disposing the Anthropic client, you can set the disposeHandler flag to false.

```csharp
public class Anthropic : IDisposable
{
    public HttpClient HttpClient => httpClient;

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
        this.httpClient.Timeout = System.Threading.Timeout.InfiniteTimeSpan; // Timeout is ignored, Anthropic client uses timeout settings from Timeout(or override per request) property
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }
}
```

Furthermore, you can retrieve the `HttpClient` used for requests via the `HttpClient` property. This allows you to modify settings such as `DefaultRequestHeaders`.

```csharp
// disable keep-alive
anthropic.HttpClient.DefaultRequestHeaders.ConnectionClose = true;
```

You can change the `HttpClient.BaseAddress` to change the API address(e.g., for proxies).

```csharp
// request to http://myproxy/messages instead of https://api.anthropic.com/v1/messages
anthropic.HttpClient.BaseAddress = new Uri("http://myproxy/");
```

Upload File
---
`Message.Content` accepts multiple `Content` objects. However, if a single string is passed, it is automatically converted into an array of text.

```csharp
// this code
Content = "Hello, Claude"
// is convert to following
Content = [new Content
{
    Type = "text",
    Text = "Hello, Claude"
}]
```

When passing an image, set both the image and Text in the Content. 

```csharp
var imageBytes = File.ReadAllBytes(@"dish.jpg");

var anthropic = new Anthropic();
var message = await anthropic.Messages.CreateAsync(new()
{
    Model = "claude-3-opus-20240229",
    MaxTokens = 1024,
    Messages = [new()
    {
        Role = "user",
        Content = [
            new()
            {
                Type = "image",
                Source = new()
                {
                    Type = "base64",
                    MediaType = "image/jpeg",
                    Data = imageBytes
                }
            },
            new()
            {
                Type = "text",
                Text = "Describe this image."
            }
        ]
    }],
});
Console.WriteLine(message);
```

The above code can be simplified. If a string is passed to the Content constructor, it is set as text, and if `ReadOnlyMemory<byte>` is passed, it is set as an image.

```csharp
var message = await anthropic.Messages.CreateAsync(new()
{
    Model = Models.Claude3Opus,
    MaxTokens = 1024,
    Messages = [new()
    {
        Role = Roles.User,
        Content = [
            new(imageBytes, "image/jpeg"),
            "Describe this image."
        ]
    }],
});
Console.WriteLine(message);
```

Currently, there are four types of uploadable binaries: `image/jpeg`, `image/png`, `image/gif`, and `image/webp`. For example, if you want to upload a markdown file, it's best to read its contents and send it as text. If you want to upload a PDF, you can either convert it to text or an image before sending. Presentation files like pptx can also be sent as images, and Claude will interpret the content and extract the text for you.

System and Temperature
---
Other optional properties of `MessageRequest` include `System`, `Metadata`, `StopSequences`, `Temperature`, `TopP`, and `TopK`.

```csharp
var message = await anthropic.Messages.CreateAsync(new()
{
    Model = Models.Claude3Haiku,
    MaxTokens = 1024,
    System = SystemPrompts.Claude3,
    Temperature = 0.4,
    Messages = [
        new() { Role = Roles.User, Content = "Hello, Claude" },
    ],
});
```

`SystemPrompts.Claude3` is a string constant for the System Prompt used in the Official Chat UI. Of course, you can also set any arbitrary System Prompt.

Save / Load
---
All request and response models can be serialized using `System.Text.Json.JsonSerializer`. Additionally, `AnthropicJsonSerialzierContext` has pre-generated serializers available through Source Generator, enabling even higher performance.

```csharp
List<Message> chatMessages;

void Save()
{
    var json = JsonSerializer.Serialize(chatMessages, AnthropicJsonSerialzierContext.Default.Options);
    File.WriteAllText("chat.json", json);
}

void Load()
{
    chatMessages = JsonSerializer.Deserialize<List<Message>>("chat.json", AnthropicJsonSerialzierContext.Default.Options)!;
}
```

Function Calling
---
Claude supports Function Calling. The [Anthropic Cookbook](https://github.com/anthropics/anthropic-cookbook) provides examples of Function Calling. To achieve this, complex XML generation and parsing processing, as well as execution based on the parsed results, are required.

With Claudia, you only need to define static methods annotated with `[ClaudiaFunction]`, and the C# Source Generator automatically generates the necessary code, including parsers and system messages.

```csharp
public static partial class FunctionTools
{
    // Sample of anthropic-tools https://github.com/anthropics/anthropic-tools#basetool

    /// <summary>
    /// Retrieve the current time of day in Hour-Minute-Second format for a specified time zone. Time zones should be written in standard formats such as UTC, US/Pacific, Europe/London.
    /// </summary>
    /// <param name="timeZone">The time zone to get the current time for, such as UTC, US/Pacific, Europe/London.</param>
    [ClaudiaFunction]
    public static string TimeOfDay(string timeZone)
    {
        var time =  TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timeZone);
        return time.ToString("HH:mm:ss");
    }
}
```

The `partial class` includes the generated `.SystemPrompt` and `.InvokeAsync(MessageResponse)`.

Function Calling requires two requests to Claude. The flow is as follows: "Initial request to Claude with available tools in System Prompt -> Execute functions based on the message containing the necessary tools -> Include the results in a new message and send another request to Claude."

```csharp
// `FunctionTools.SystemPrompt` contains the XML used to inform Claude about the available tools.
// This XML is generated from the method's XML documentation comments.
/*
In this environment you have access to a set of tools you can use to answer the user's question.
...
You may call them like this:
...
Here are the tools available:
<tools>
  <tool_description>
    <tool_name>TimeOfDay</tool_name>
    <description>Retrieve the current time of day in Hour-Minute-Second format for a specified time zone. Time zones should be written in standard formats such as UTC, US/Pacific, Europe/London.</description>
    <parameters>
      <parameter>
        <name>timeZone</name>
        <type>string</type>
        <description>The time zone to get the current time for, such as UTC, US/Pacific, Europe/London.</description>
      </parameter>
    </parameters>
  </tool_description>
</tools>
*/
// Console.WriteLine(FunctionTools.SystemPrompt);

var input = new Message { Role = Roles.User, Content = "What time is it in Los Angeles?" };
var message = await anthropic.Messages.CreateAsync(new()
{
    Model = Models.Claude3Haiku,
    MaxTokens = 1024,
    System = FunctionTools.SystemPrompt, // set generated prompt
    StopSequences = [StopSequnces.CloseFunctionCalls], // set </function_calls> as stop sequence
    Messages = [input],
});

// Claude returns xml to invoke tool
/*
<function_calls>
    <invoke>
        <tool_name>TimeOfDay</tool_name>
        <parameters>
            <timeZone>US/Pacific</timeZone>
        </parameters>
    </invoke>
*/
// Console.WriteLine(message);

// `FunctionTools.InvokeAsync`, which is automatically generated, parses the function name and parameters from the `MessageResponse`,
// executes the corresponding function, and generates XML to inform Claude about the function execution results.
var partialAssistantMessage = await FunctionTools.InvokeAsync(message);

// By passing this message to Claude as the beginning of the Assistant's response,
// it will provide a continuation that takes into account the function execution results.
/*
<function_calls>
    <invoke>
        <tool_name>TimeOfDay</tool_name>
        <parameters>
            <timeZone>US/Pacific</timeZone>
        </parameters>
    </invoke>
</function_calls>
<function_results>
    <result>
        <tool_name>TimeOfDay</tool_name>
        <stdout>03:27:03</stdout>
    </result>
</function_results>
*/
// Console.WriteLine(partialAssistantMessage);

var callResult = await anthropic.Messages.CreateAsync(new()
{
    Model = Models.Claude3Haiku,
    MaxTokens = 1024,
    System = FunctionTools.SystemPrompt,
    Messages = [
        input, // User: "What time is it in Los Angeles?"
        new() { Role = Roles.Assistant, Content = partialAssistantMessage! } // set as Assistant
    ],
});

// The current time in Los Angeles (US/Pacific time zone) is 03:36:04.
Console.WriteLine(callResult);
```

For the initial request, specifying `StopSequences.CloseFunctionCalls` is efficient. Also, if you want to include your own System Prompt, it's a good idea to concatenate it with the generated SystemPrompt.

The return type of `ClaudiaFunction` can also be specified as `Task<T>` or `ValueTask<T>`. This allows you to execute a variety of tasks, such as HTTP requests or database requests. For example, a function that retrieves the content of a specified webpage can be defined as shown above.

```csharp
public static partial class FunctionTools
{
    // ...

    /// <summary>
    /// Retrieves the HTML from the specified URL.
    /// </summary>
    /// <param name="url">The URL to retrieve the HTML from.</param>
    [ClaudiaFunction]
    static async Task<string> GetHtmlFromWeb(string url)
    {
        // When using this in a real-world application, passing the raw HTML might consume too many tokens.
        // You can parse the HTML locally using libraries like AngleSharp and convert it into a compact text structure to save tokens.
        using var client = new HttpClient();
        return await client.GetStringAsync(url);
    }
}
```

```csharp
var input = new Message
{
    Role = Roles.User,
    Content = """
        Could you summarize this page in three line?
        https://docs.anthropic.com/claude/docs/intro-to-claude
"""
};

var message = await anthropic.Messages.CreateAsync(new()
{
    Model = Models.Claude3Haiku,
    MaxTokens = 1024,
    System = FunctionTools.SystemPrompt, // set generated prompt
    StopSequences = [StopSequnces.CloseFunctionCalls], // set </function_calls> as stop sequence
    Messages = [input],
});

var partialAssistantMessage = await FunctionTools.InvokeAsync(message);

var callResult = await anthropic.Messages.CreateAsync(new()
{
    Model = Models.Claude3Haiku,
    MaxTokens = 1024,
    System = FunctionTools.SystemPrompt,
    Messages = [
        input,
        new() { Role = Roles.Assistant, Content = partialAssistantMessage! } // set as Assistant
    ],
});

// The page can be summarized in three lines:
// 1. Claude is a family of large language models developed by Anthropic designed to revolutionize the way you interact with AI.
// 2. This documentation is designed to help you get the most out of Claude, with clear explanations, examples, best practices, and links to additional resources.
// 3. Claude excels at a wide variety of tasks involving language, reasoning, analysis, coding, and more, and the documentation covers key capabilities, getting started with prompting, and using the API.
Console.WriteLine(callResult);
```

Multiple functions can be defined, and they can be executed multiple times in a single request.

```csharp
public static partial class FunctionTools
{
    [ClaudiaFunction]
    public static string TimeOfDay(string timeZone) //...

    // Sample of https://github.com/anthropics/anthropic-cookbook/blob/main/function_calling/function_calling.ipynb

    /// <summary>
    /// Calculator function for doing basic arithmetic. 
    /// Supports addition, subtraction, multiplication
    /// </summary>
    /// <param name="firstOperand">First operand (before the operator)</param>
    /// <param name="secondOperand">Second operand (after the operator)</param>
    /// <param name="operator">The operation to perform. Must be either +, -, *, or /</param>
    [ClaudiaFunction]
    static double DoPairwiseArithmetic(double firstOperand, double secondOperand, string @operator)
    {
        return @operator switch
        {
            "+" => firstOperand + secondOperand,
            "-" => firstOperand - secondOperand,
            "*" => firstOperand * secondOperand,
            "/" => firstOperand / secondOperand,
            _ => throw new ArgumentException("Operation not supported")
        };
    }
}
```

```csharp
var input = new Message
{
    Role = Roles.User,
    Content = """
        What time is it in Seattle and Tokyo?
        Incidentally multiply 1,984,135 by 9,343,116.
"""
};

var message = await anthropic.Messages.CreateAsync(new()
{
    Model = Models.Claude3Haiku,
    MaxTokens = 1024,
    System = FunctionTools.SystemPrompt, // set generated prompt
    StopSequences = [StopSequnces.CloseFunctionCalls], // set </function_calls> as stop sequence
    Messages = [input],
});

var partialAssistantMessage = await FunctionTools.InvokeAsync(message);

var callResult = await anthropic.Messages.CreateAsync(new()
{
    Model = Models.Claude3Haiku,
    MaxTokens = 1024,
    System = FunctionTools.SystemPrompt,
    Messages = [
        input,
        new() { Role = Roles.Assistant, Content = partialAssistantMessage! } // set as Assistant
    ],
});

// The time in Seattle (US/Pacific time zone) is 8:06:53.
// The time in Tokyo (Asia/Tokyo time zone) is 00:06:53.
// The result of multiplying 1,984,135 by 9,343,116 is 18,524,738,326,760.
Console.WriteLine(callResult);
```

Note that the allowed parameter types are `bool`, `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `decimal`, `float`, `double`, `string`, `DateTime`, `DateTimeOffset`, `Guid`, and `TimeSpan`.

The return value can be of any type, but it will be converted to a string using `ToString()`. If you want to return a custom string, make the return type `string` and format the string within the function.

Blazor Sample
---
By using Claudia with Blazor, you can easily create a Chat UI like the one shown below.

![blazorclauderec](https://github.com/Cysharp/Claudia/assets/46207/dfcad512-4cf1-4af0-ba03-901dc7ce36a6)

All the code can be found in [BlazorApp1](https://github.com/Cysharp/Claudia/tree/main/sandbox/BlazorApp1).

The key parts are the setup in `Program.cs` and `Home.razor.cs`.

```csharp
// Program.cs

// get ANTHROPIC_API_KEY from user secret
// https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets
Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", builder.Configuration["ANTHROPIC_API_KEY"]);

// Add Anthropic Client
builder.Services.AddSingleton<Anthropic>();

var app = builder.Build();
```

```csharp
// Home.razor.cs

using Claudia;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorApp1.Components.Pages;

public partial class Home
{
    [Inject]
    public required Anthropic Anthropic { get; init; }

    double temperature = 1.0;
    string textInput = "";
    string systemInput = SystemPrompts.Claude3;
    List<Message> chatMessages = new();

    bool running = false;

    async Task SendClick()
    {
        if (running) return;
        if (string.IsNullOrWhiteSpace(textInput)) return;

        running = true;
        try
        {
            chatMessages.Add(new() { Role = Roles.User, Content = textInput });

            var stream = Anthropic.Messages.CreateStreamAsync(new()
            {
                Model = Models.Claude3Opus,
                MaxTokens = 1024,
                Temperature = temperature,
                System = string.IsNullOrWhiteSpace(systemInput) ? null : systemInput,
                Messages = chatMessages.ToArray()
            });

            var currentMessage = new Message { Role = Roles.Assistant, Content = "" };
            chatMessages.Add(currentMessage);

            textInput = ""; // clear input.
            StateHasChanged();

            await foreach (var messageStreamEvent in stream)
            {
                if (messageStreamEvent is ContentBlockDelta content)
                {
                    currentMessage.Content[0].Text += content.Delta.Text;
                    StateHasChanged();
                }
            }
        }
        finally
        {
            running = false;
        }
    }
}
```

If you need to store the chat message history, you can serialize `List<Message> chatMessages` to JSON and save it to a file or database.

Unity
---
Minimum supported Unity version is `2022.3.12f1`. You need to install from NuGet. We recommend using [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).

1. Install NuGetForUnity
2. Open Window from NuGet -> Manage NuGet Packages, Search "Claudia" and Press Install.

With this, you can use the `Anthropic` client in both the Editor and at Runtime.

```csharp
using Claudia;
using System;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    async void Start()
    {
        var anthropic = new Anthropic()
        {
            ApiKey = "YOUR API KEY"
        };

        Debug.Log("Start Simple Call in Unity");

        var message = await anthropic.Messages.CreateAsync(new()
        {
            Model = Models.Claude3Opus,
            MaxTokens = 1024,
            Messages = new Message[] { new() { Role = "user", Content = "Hello, Claude" } }
        });

        Debug.Log("User: Hello, Claude");
        Debug.Log("Assistant: " + message);
    }
}
```

![image](https://github.com/Cysharp/Claudia/assets/46207/1bd9395b-1595-4034-9c40-8e37aa750284)

Source Generators for Function Calling are also supported, but additional work is required.

1. Setup the C# compiler for unity. 
    - Add a text file named `csc.rsp` with the following contents under your Assets/.
        - ```
          -langVersion:10 -nullable
          ```

2. Setup the C# compiler for your IDE. 
    - Install [CsprojModifier](https://github.com/Cysharp/CsprojModifier) 
    - Add a text file named LangVersion.props with the following contents
        - ```xml
          <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
            <PropertyGroup>
              <LangVersion>10</LangVersion>
              <Nullable>enable</Nullable>
            </PropertyGroup>
          </Project>
          ``` 
    - Open Project Settings and [C# Project Modifier] section under the [Editor].
    - Add the .props file you just created, to the list of [Additional project imports].
    - Note:
        - If you are using assembly definition, add your additional csproj in the list of [The project to be addef for import].

```csharp
using Claudia;
using System;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    async void Start()
    {
        var anthropic = new Anthropic()
        {
            ApiKey = "YOUR API KEY"
        };

        Debug.Log("Start Function Calling Demo in Unity");

        var input = new Message
        {
            Role = Roles.User,
            Content = "Multiply 1,984,135 by 9,343,116"
        };

        var message = await anthropic.Messages.CreateAsync(new()
        {
            Model = Models.Claude3Haiku,
            MaxTokens = 1024,
            System = FunctionTools.SystemPrompt,
            StopSequences = new[] { StopSequnces.CloseFunctionCalls },
            Messages = new[] { input },
        });

        var partialAssistantMessage = await FunctionTools.InvokeAsync(message);

        var callResult = await anthropic.Messages.CreateAsync(new()
        {
            Model = Models.Claude3Haiku,
            MaxTokens = 1024,
            System = FunctionTools.SystemPrompt,
            Messages = new[]{
                input,
                new() { Role = Roles.Assistant, Content = partialAssistantMessage! }
            },
        });

        Debug.Log("User: Multiply 1,984,135 by 9,343,116");
        Debug.Log("Assistant: " + callResult.ToString().Trim());
    }
}

public static partial class FunctionTools
{
    /// <summary>
    /// Calculator function for doing basic arithmetic. 
    /// Supports addition, subtraction, multiplication
    /// </summary>
    /// <param name="firstOperand">First operand (before the operator)</param>
    /// <param name="secondOperand">Second operand (after the operator)</param>
    /// <param name="operator">The operation to perform. Must be either +, -, *, or /</param>
    [ClaudiaFunction]
    static double DoPairwiseArithmetic(double firstOperand, double secondOperand, string @operator)
    {
        return @operator switch
        {
            "+" => firstOperand + secondOperand,
            "-" => firstOperand - secondOperand,
            "*" => firstOperand * secondOperand,
            "/" => firstOperand / secondOperand,
            _ => throw new ArgumentException("Operation not supported")
        };
    }
}
```

License
---
This library is under the MIT License.
