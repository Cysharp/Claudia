using Amazon.BedrockRuntime;
using Claudia;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Text.Json;

namespace BedrockBlazorApp1.Components.Pages;

public partial class Home
{
    [Inject]
    public required AmazonBedrockRuntimeClient BedrockClient { get; init; }
    private BedrockAnthropicClient anthropic = default!;

    double temperature = 1.0;
    string textInput = "";
    string systemInput = SystemPrompts.Claude3;
    List<Message> chatMessages = new();

    bool running = false;

    protected override void OnInitialized()
    {
        anthropic = BedrockClient.UseAnthropic("anthropic.claude-3-haiku-20240307-v1:0");
        base.OnInitialized();
    }

    async Task SendClick()
    {
        if (running) return;
        if (string.IsNullOrWhiteSpace(textInput)) return;

        running = true;
        try
        {
            chatMessages.Add(new() { Role = Roles.User, Content = textInput });

            var stream = anthropic.Messages.CreateStreamAsync(new()
            {
                Model = "bedrock-2023-05-31",
                MaxTokens = 1024,
                Temperature = temperature,
                System = string.IsNullOrWhiteSpace(systemInput) ? null : systemInput,
                Messages = chatMessages.ToArray()
            });

            var currentMessage = new Message { Role = Roles.Assistant, Content = "" };
            chatMessages.Add(currentMessage);

            textInput = ""; // clear input.
            StateHasChanged();

            await foreach (var messageStreamEvent in stream)
            {
                if (messageStreamEvent is ContentBlockDelta content)
                {
                    currentMessage.Content[0].Text += content.Delta.Text;
                    StateHasChanged();
                }
            }
        }
        finally
        {
            running = false;
        }
    }
}