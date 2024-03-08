using System.Text.Json.Serialization;

namespace Claudia;

//  https://docs.anthropic.com/claude/reference/errors

public class ClaudiaException : Exception
{
    public ErrorCode Status { get; }
    public string Name { get; }

    public ClaudiaException(ErrorCode errorCode, string type, string message)
        : base(message)
    {
        this.Status = errorCode;
        this.Name = type;
    }

    public override string ToString()
    {
        return $"{Name}: {Message}";
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

    public ErrorCode ToErrorCode()
    {
        switch (Type)
        {
            case "invalid_request_error": return ErrorCode.InvalidRequestError;
            case "authentication_error": return ErrorCode.AuthenticationError;
            case "permission_error": return ErrorCode.PermissionError;
            case "not_found_error": return ErrorCode.NotFoundError;
            case "rate_limit_error": return ErrorCode.RateLimitError;
            case "api_error": return ErrorCode.ApiError;
            case "overloaded_error": return ErrorCode.OverloadedError;
            default: return (ErrorCode)0;
        }
    }

    public override string ToString()
    {
        return $"{Type}: {Message}";
    }
}
