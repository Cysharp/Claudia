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
    ApiKey = "my_api_key"
};

var message = await anthropic.Messages.CreateAsync(new()
{
    Model = "claude-3-opus-20240229",
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude" }]
});

Console.WriteLine(message);
```

Streaming Messages
---
Coming Soon.

Handling errors
---
If the API call fails, a `ClaudiaException` will be thrown. You can check the `ErrorCode`, `Type`, and `Message` from the `ClaudiaException`.

License
---
This library is under the MIT License.
