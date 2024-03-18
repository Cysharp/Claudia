using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Util;
using Claudia;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

// credentials is your own
AWSConfigs.AWSProfileName = "";

var bedrock = new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1);


var input = """
        What time is it in Seattle and Tokyo?
        Incidentally multiply 1,984,135 by 9,343,116.
""";
var payload = new JsonObject()
    {
        { "anthropic_version", "bedrock-2023-05-31" },
        { "max_tokens", 1000 },
        { "messages", new JsonArray(new JsonObject{
            { "role", "user" },
            { "content", new JsonArray(new JsonObject{
                { "type", "text" },
                { "text", input }
            })}
        })
        }
    }.ToJsonString();







var response = await bedrock.InvokeModelAsync("anthropic.claude-3-sonnet-20240229-v1:0", new()
{
    Model = "bedrock-2023-05-31",
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude" }]
});

if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
    throw new HttpRequestException("Request Failed", null, response.HttpStatusCode);

var resBody = await JsonNode.ParseAsync(response.Body);
Console.WriteLine(resBody);



