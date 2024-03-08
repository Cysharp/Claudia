using Claudia;
using System.Threading;
using System;



//import Anthropic from '@anthropic-ai/sdk';

//const anthropic = new Anthropic({
//  apiKey: 'my_api_key', // defaults to process.env["ANTHROPIC_API_KEY"]
//});

//const msg = await anthropic.messages.create({
//  model: "claude-3-opus-20240229",
//max_tokens: 1024,
//  messages: [{ role: "user", content: "Hello, Claude" }],
//});
//console.log(msg);


//var anthropic = new Anthropic
//{
//    // ApiKey = secret
//    // Timeout = TimeSpan.FromMilliseconds(1)
//};

//var msg = await anthropic.Messages.CreateAsync(new()
//{
//    Model = "claude-3-opus-20240229",
//    MaxTokens = 1024,
//    Messages = [new() { Role = "user", Content = "Hello, Claude" }]
//});

//Console.WriteLine(msg);


// Console.WriteLine(TimeSpan.FromMilliseconds(Anthropic.CalculateDefaultRetryTimeoutMillis(Random.Shared, 0, 4)));


var MaxRetries = 0;
var retriesRemaining = MaxRetries;
RETRY:
try
{
    throw new Exception();
}
catch
{
    if (retriesRemaining > 0)
    {
        //var sleep = CalculateDefaultRetryTimeoutMillis(random, retriesRemaining, MaxRetries);
        //await Task.Delay(TimeSpan.FromMilliseconds(sleep), cancellationToken).ConfigureAwait(false);
        retriesRemaining--;
        Console.WriteLine("RETRY");
        goto RETRY;
    }
    throw;
}
