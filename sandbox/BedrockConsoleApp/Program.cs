using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.BedrockRuntime.Model.Internal.MarshallTransformations;
using Amazon.Runtime.EventStreams.Internal;
using Claudia;
using System.Buffers;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO.Pipelines;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ObjectiveC;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using ThirdParty.Json.LitJson;


// credentials is your own
AWSConfigs.AWSProfileName = "cysharp-sandbox";

var bedrock = new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1);
var anthropic = bedrock.UseAnthropic("anthropic.claude-3-haiku-20240307-v1:0");

//var response = await anthropic.Messages.CreateAsync(new()
//{
//    Model = "bedrock-2023-05-31",
//    MaxTokens = 1024,
//    Messages = [new() { Role = "user", Content = "Hello, Claude" }]
//});

//Console.WriteLine(response);


var stream = anthropic.Messages.CreateStreamAsync(new()
{
    Model = "bedrock-2023-05-31",
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude" }]
});

await foreach (var item in stream)
{
    Console.WriteLine(item);
}


