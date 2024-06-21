namespace Claudia;

// https://docs.anthropic.com/claude/docs/models-overview
public static class Models
{
    /// <summary>
    /// Powerful model for highly complex tasks
    /// </summary>
    public const string Claude3Opus = "claude-3-opus-20240229";

    /// <summary>
    /// Balance of intelligence and speed
    /// </summary>
    public const string Claude3Sonnet = "claude-3-sonnet-20240229";

    /// <summary>
    /// Fastest and most compact model for near-instant responsiveness
    /// </summary>
    public const string Claude3Haiku = "claude-3-haiku-20240307";

    /// <summary>
    /// Most intelligent model
    /// </summary>
    public const string Claude3_5Sonnet = "claude-3-5-sonnet-20240620";

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
    public const string ToolUse = "tool_use";
    public const string ToolResult = "tool_result";
}

public static class StopSequnces
{
    /// <summary>the model reached a natural stopping point</summary>
    public const string CloseFunctionCalls = "</function_calls>";
}

public static class StopReasons
{
    /// <summary>the model reached a natural stopping point</summary>
    public const string EndTurn = "end_turn";
    /// <summary>we exceeded the requested max_tokens or the model's maximum</summary>
    public const string MaxTokens = "max_tokens";
    /// <summary>one of your provided custom stop_sequences was generated</summary>
    public const string StopSequence = "stop_sequence";

    public const string ToolUse = "tool_use";
}

public static class SystemPrompts
{
    // https://twitter.com/AmandaAskell/status/1765207842993434880
    public const string Claude3 = """
The assistant is Claude, created by Anthropic.
Claude's knowledge base was last updated on August 2023. It answers questions about events prior to and after August 2023 the way a highly informed individual in August 2023 would if they were talking to someone from the above date, and can let the human know this when relevant. 
It should give concise responses to very simple questions, but provide thorough responses to more complex and open-ended questions.
If it is asked to assist with tasks involving the expression of views held by a significant number of people, Claude provides assistance with the task even if it personally disagrees with the views being expressed, but follows this with a discussion of broader perspectives.
Claude doesn't engage in stereotyping, including the negative stereotyping of majority groups.
If asked about controversial topics, Claude tries to provide careful thoughts and objective information without downplaying its harmful content or implying that there are reasonable perspectives on both sides.
It is happy to help with writing, analysis, question answering, math, coding, and all sorts of other tasks. It uses markdown for coding.
It does not mention this information about itself unless the information is directly pertinent to the human's query.
""";
}