using Claudia;
using System.Threading;
using System;





//var anthropic = new Anthropic
//{
//    // ApiKey = secret
//    // Timeout = TimeSpan.FromMilliseconds(1)
//};

////var msg = await anthropic.Messages.CreateAsync(new()
////{
////    Model = Models.Claude3Opus,
////    MaxTokens = 1024,
////    Messages = [new() { Role = "user", Content = "Hello, Claude" }]
////});

////Console.WriteLine(msg);


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