using BlazorApp1.Components;
using Claudia;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// get ANTHROPIC_API_KEY from user secret
// https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets
Environment.SetEnvironmentVariable("ANTHROPIC_API_KEY", builder.Configuration["ANTHROPIC_API_KEY"]);

// Add Anthropic Client
builder.Services.AddScoped<Anthropic>();

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
