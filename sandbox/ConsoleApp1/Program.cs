using Claudia;



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


var anthropic = new Anthropic
{
    // ApiKey = secret
};

var msg = await anthropic.Messages.CreateAsync(new()
{
    Model = "claude-3-opus-20240229",
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude" }]
});

Console.WriteLine(msg);







