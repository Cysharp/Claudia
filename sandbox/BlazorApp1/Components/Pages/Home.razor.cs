using Claudia;
using Microsoft.AspNetCore.Components;

namespace BlazorApp1.Components.Pages;

public partial class Home
{
    [Inject]
    public required Anthropic Anthropic { get; init; }

    double temperature = 1.0;
    string textInput = "";
    string systemInput = SystemPrompts.Claude3;
    List<Message> chatMessages = new();

    bool running = false;

    async void SendClick()
    {
        if (running) return;

        running = true;
        try
        {
            chatMessages.Add(new() { Role = Roles.User, Content = textInput });

            var stream = Anthropic.Messages.CreateStreamAsync(new()
            {
                Model = Models.Claude3Opus,
                MaxTokens = 1024,
                Temperature = temperature,
                System = string.IsNullOrWhiteSpace(systemInput) ? null : systemInput,
                Messages = chatMessages.ToArray()
            });

            chatMessages.Add(new Message { Role = Roles.Assistant, Content = "" });

            textInput = ""; // clear input.
            StateHasChanged();

            await foreach (var messageStreamEvent in stream)
            {
                if (messageStreamEvent is ContentBlockDelta content)
                {
                    var lastMessage = chatMessages[^1];
                    var newMessage = lastMessage with
                    {
                        Content = lastMessage.Content[0].Text + content.Delta.Text
                    };
                    chatMessages[^1] = newMessage;
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