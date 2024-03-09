using Claudia;
using System.Threading;
using System;
using R3;
using System.Text;





// Streaming Responses
var anthropic = new Anthropic();

var stream = anthropic.Messages.CreateStreamAsync(new()
{
    Model = Models.Claude3Opus,
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude. Please insert new line after each words." }]
});

await foreach (var messageEvent in stream)
{
    Console.WriteLine(messageEvent);
}







//await msg.ToObservable()
//    .OfType<IMessageStreamEvent, ContentBlockDelta>()
//    .Where(x => x.Delta.Text != null)
//    .ForEachAsync(x =>
//    {
//        Console.WriteLine(x.Delta.Text);
//    });















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