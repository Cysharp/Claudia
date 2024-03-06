using System.Text.Json.Serialization;

namespace Claudia;

//  https://docs.anthropic.com/claude/reference/errors

public class ClaudiaException : Exception
{
    public ErrorCode ErrorCode { get; }
    public string Type { get; }

    public ClaudiaException(ErrorCode errorCode, string type, string message)
        : base(message)
    {
        this.ErrorCode = errorCode;
        this.Type = type;
    }

    public override string ToString()
    {
        return $"{Type}: {Message}";
    }
}

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

internal record class ErrorResponseShape
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("error")]
    public required ErrorResponse ErrorResponse { get; init; }
}

internal record class ErrorResponse
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    public override string ToString()
    {
        return $"{Type}: {Message}";
    }
}
