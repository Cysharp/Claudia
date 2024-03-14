using Claudia;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

// function calling
// https://github.com/anthropics/anthropic-cookbook/blob/main/function_calling/function_calling.ipynb

var anthropic = new Anthropic();

var userInput = """
Translate and summarize this Japanese site to English.
https://scrapbox.io/hadashiA/ZLogger_v2%E3%81%AE%E6%96%B0%E3%82%B9%E3%83%88%E3%83%A9%E3%82%AF%E3%83%81%E3%83%A3%E3%83%BC%E3%83%89%E3%83%AD%E3%82%AE%E3%83%B3%E3%82%B0%E4%BD%93%E9%A8%93
""";

var message = await anthropic.Messages.CreateAsync(new()
{
    Model = Models.Claude3Haiku,
    MaxTokens = 1024,
    System = SystemPrompts.Claude3 + "\n" + FunctionTools.SystemPrompt,
    StopSequences = [StopSequnces.CloseFunctionCalls],
    Messages = [
        new() { Role = Roles.User, Content = userInput },
    ],
});

var partialAssistantMessage = await FunctionTools.InvokeAsync(message);

var callResult = await anthropic.Messages.CreateAsync(new()
{
    Model = Models.Claude3Haiku,
    MaxTokens = 1024,
    System = SystemPrompts.Claude3 + "\n" + FunctionTools.SystemPrompt + "\n" + "Return message from assistant should be humanreadable so don't use xml tags, <result></result> and json.",
    Messages = [
        new() { Role = Roles.User, Content = userInput },
        new() { Role = Roles.Assistant, Content = partialAssistantMessage! },
    ],
});

Console.WriteLine(callResult);
























//var systemPrompt = """
//In this environment you have access to a set of tools you can use to answer the user's question.

//You may call them like this:
//<function_calls>
//    <invoke>
//        <tool_name>$TOOL_NAME</tool_name>
//        <parameters>
//            <$PARAMETER_NAME>$PARAMETER_VALUE</$PARAMETER_NAME>
//            ...
//        </parameters>
//    </invoke>
//</function_calls>

//Here are the tools available:
//<tools>
//    <tool_description>
//        <tool_name>calculator</tool_name>
//        <description>
//            Calculator function for doing basic arithmetic. 
//            Supports addition, subtraction, multiplication
//        </description>
//        <parameters>
//            <parameter>
//                <name>first_operand</name>
//                <type>int</type>
//                <description>First operand (before the operator)</description>
//            </parameter>
//            <parameter>
//                <name>second_operand</name>
//                <type>int</type>
//                <description>Second operand (after the operator)</description>
//            </parameter>
//            <parameter>
//                <name>operator</name>
//                <type>str</type>
//                <description>The operation to perform. Must be either +, -, *, or /</description>
//            </parameter>
//        </parameters>
//    </tool_description>
//</tools>
//""";


//var message = await anthropic.Messages.CreateAsync(new()
//{
//    Model = Models.Claude3Opus,
//    MaxTokens = 1024,
//    System = systemPrompt,
//    StopSequences = ["\n\nHuman:", "\n\nAssistant", "</function_calls>"],
//    Messages = [new() { Role = "user", Content = "Multiply 1,984,135 by 9,343,116" }],
//});

//// Result XML::


//var text = message.Content[0].Text!;
//var tagStart = text.IndexOf("<function_calls>");
//var xmlResult = XElement.Parse(text.Substring(tagStart) + message.StopSequence);
//var parameters = xmlResult.Descendants("parameters").Elements();

//var first = (double)parameters.First(x => x.Name == "first_operand");
//var second = (double)parameters.First(x => x.Name == "second_operand");
//var operation = (string)parameters.First(x => x.Name == "operator");

//var result = DoPairwiseArithmetic(first, second, operation);

//Console.WriteLine(result);
















//var imageBytes = File.ReadAllBytes(@"dish.jpg");

//var anthropic = new Anthropic();

//var message = await anthropic.Messages.CreateAsync(new()
//{
//    Model = "claude-3-opus-20240229",
//    MaxTokens = 1024,
//    Messages = [new()
//    {
//        Role = "user",
//        Content = [
//            new()
//            {
//                Type = "image",
//                Source = new()
//                {
//                    Type = "base64",
//                    MediaType = "image/jpeg",
//                    Data = imageBytes
//                }
//            },
//            new()
//            {
//                Type = "text",
//                Text = "Describe this image."
//            }
//        ]
//    }],
//});
//Console.WriteLine(message);

//var simple = await anthropic.Messages.CreateAsync(new()
//{
//    Model = Models.Claude3Opus,
//    MaxTokens = 1024,
//    Messages = [new()
//    {
//        Role = Roles.User,
//        Content = [
//            new(imageBytes, "image/jpeg"),
//            "Describe this image."
//        ]
//    }],
//});
//Console.WriteLine(simple);

//// convert to array.
//var array = await stream.ToObservable().ToArrayAsync();

//// filterling and execute.
//await stream.ToObservable()
//    .OfType<IMessageStreamEvent, ContentBlockDelta>()
//    .Where(x => x.Delta.Text != null)
//    .ForEachAsync(x =>
//    {
//        Console.WriteLine(x.Delta.Text);
//    });

//// branching query
//var branch = stream.ToObservable().Publish();

//var messageStartTask = branch.OfType<IMessageStreamEvent, MessageStart>().FirstAsync();
//var messageDeltaTask = branch.OfType<IMessageStreamEvent, MessageDelta>().FirstAsync();

//branch.Connect(); // start consume stream

//Console.WriteLine((await messageStartTask));
//Console.WriteLine((await messageDeltaTask));







//Console.WriteLine("---");

//Console.WriteLine(sb.ToString());

// Counting Tokens
//var anthropic = new Anthropic();

//var msg = await anthropic.Messages.CreateAsync(new()
//{
//    Model = Models.Claude3Opus,
//    MaxTokens = 1024,
//    Messages = [new() { Role = "user", Content = "Hello, Claude." }]
//});

//// Usage { InputTokens = 11, OutputTokens = 18 }
//Console.WriteLine(msg.Usage);



//Messages = [new() { Role = "user", Content = "Hello, Claude. Responses, please break line after each word." }]


//// error

//try
//{
//    var msg = await anthropic.Messages.CreateAsync(new()
//    {
//        Model = Models.Claude3Opus,
//        MaxTokens = 1024,
//        Messages = [new() { Role = "user", Content = "Hello, Claude" }]
//    });
//}
//catch (ClaudiaException ex)
//{
//    Console.WriteLine((int)ex.Status); // 400(ErrorCode.InvalidRequestError)
//    Console.WriteLine(ex.Name);        // invalid_request_error
//    Console.WriteLine(ex.Message);     // Field required. Input:...
//}

// retry

// Configure the default for all requests:
//var anthropic = new Anthropic
//{
//    MaxRetries = 0, // default is 2
//};

//// Or, configure per-request:
//await anthropic.Messages.CreateAsync(new()
//{
//    MaxTokens = 1024,
//    Messages = [new() { Role = "user", Content = "Hello, Claude" }],
//    Model = "claude-3-opus-20240229"
//}, new()
//{
//    MaxRetries = 5
//});

// timeout

//// Configure the default for all requests:
//var anthropic = new Anthropic
//{
//    Timeout = TimeSpan.FromSeconds(20) // 20 seconds (default is 10 minutes)
//};

//// Override per-request:
//await anthropic.Messages.CreateAsync(new()
//{
//    MaxTokens = 1024,
//    Messages = [new() { Role = "user", Content = "Hello, Claude" }],
//    Model = "claude-3-opus-20240229"
//}, new()
//{
//    Timeout = TimeSpan.FromSeconds(5)
//});


public static partial class FunctionTools
{
    /// <summary>
    /// Date of target location.
    /// </summary>
    /// <param name="timeZoneId">TimeZone of localtion like 'Tokeyo Standard Time', 'Eastern Standard Time', etc.</param>
    /// <returns></returns>
    [ClaudiaFunction]
    public static DateTime Today(string timeZoneId)
    {
        return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timeZoneId);
    }

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

    /// <summary>
    /// Get html from target url.
    /// </summary>
    /// <param name="url">Url of public internet for get html.</param>
    [ClaudiaFunction]
    static async Task<string> GetHtmlFromWeb(string url)
    {
        using var client = new HttpClient();
        return await client.GetStringAsync(url);
    }
}



