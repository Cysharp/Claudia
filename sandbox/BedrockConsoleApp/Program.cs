using Amazon;
using Amazon.BedrockRuntime;
using Claudia;

// credentials is your own
AWSConfigs.AWSProfileName = "";

var bedrock = new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1);
var anthropic = bedrock.UseAnthropic("anthropic.claude-3-sonnet-20240229-v1:0");

var response = await anthropic.Messages.CreateAsync(new()
{
    Model = "bedrock-2023-05-31",
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude" }]
});

Console.WriteLine(response);