using Amazon;
using Amazon.BedrockRuntime;
using BedrockBlazorApp1.Components;

AWSConfigs.AWSProfileName = "";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Anthropic Client
builder.Services.AddSingleton<AmazonBedrockRuntimeClient>(new AmazonBedrockRuntimeClient(RegionEndpoint.USEast1));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
