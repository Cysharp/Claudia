namespace Claudia;

// https://docs.anthropic.com/claude/docs/models-overview
public static class Models
{
    /// <summary>
    /// Most powerful model for highly complex tasks
    /// </summary>
    public const string Claude3Opus = "claude-3-opus-20240229";

    /// <summary>
    /// Ideal balance of intelligence and speed for enterprise workloads
    /// </summary>
    public const string Claude3Sonnet = "claude-3-sonnet-20240229";

    // public const string Claude3Haiku = ""; Coming soon

    /// <summary>Updated version of Claude 2 with improved accuracy</summary>
    public const string Claude2_1 = "claude-2.1";

    /// <summary>Predecessor to Claude 3, offering strong all-round performance</summary>
    public const string Claude2_0 = "claude-2.0";

    /// <summary>Our cheapest small and fast model, a predecessor of Claude Haiku.</summary>
    public const string Claude1_2 = "claude-instant-1.2";
}

public static class MediaTypes
{
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string Gif = "image/gif";
    public const string Webp = "image/webp";
}

public static class Roles
{
    public const string User = "user";
    public const string Assistant = "assistant";
}

public static class ContentTypes
{
    public const string Text = "text";
    public const string Image = "image";
}

public static class StopReasons
{
    /// <summary>the model reached a natural stopping point</summary>
    public const string EndTurn = "end_turn";
    /// <summary>we exceeded the requested max_tokens or the model's maximum</summary>
    public const string MaxTokens = "max_tokens";
    /// <summary>one of your provided custom stop_sequences was generated</summary>
    public const string StopSequence = "stop_sequence";
}