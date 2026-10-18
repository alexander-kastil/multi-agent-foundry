using Microsoft.Extensions.AI;
using Azure.Identity;
using OpenAI.Chat;
using OpenAI;
using System.ClientModel.Primitives;

#pragma warning disable OPENAI001   // currently required for token-based authentication against Azure OpenAI

public class ChatRunner
{
    public async Task Run(string model, string endpoint)
    {
        // Create a BearerTokenPolicy using DefaultAzureCredential (Azure AD)
        var tokenPolicy = new BearerTokenPolicy(
            new DefaultAzureCredential(),
            "https://cognitiveservices.azure.com/.default");

        // Create the OpenAI ChatClient pointing at Azure OpenAI's OpenAI-compatible endpoint
        var chatClient = new ChatClient(
            model: model!,
            authenticationPolicy: tokenPolicy,
            options: new OpenAIClientOptions
            {
                // Azure OpenAI compatible path
                Endpoint = new Uri(endpoint!.TrimEnd('/') + "/openai/v1")
            }
        );

        // Adapt the OpenAI ChatClient to Microsoft.Extensions.AI's IChatClient
        // Requires Microsoft.Extensions.AI.OpenAI package (preview)
        IChatClient client = chatClient.AsIChatClient(); // extension method

        // Prepare ME.AI chat messages and options
        var messages = new[]
        {
            new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, "You are a helpful assistant that knows about gym, workouts, and fitness."),
            new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, "What kinds of protein do you know about? Just a short list please.")
        };

        // Optional: set options analogous to your sample (reasoning & max tokens when supported)
        var options = new ChatOptions
        {
            // Not all backends honor every option; you can still set output tokens:
            MaxOutputTokens = 1024
            // Reasoning-specific knobs vary by model/provider; keep minimal here.
        };

        // Get a single response using the unified abstraction
        ChatResponse response = await client.GetResponseAsync(messages, options);

        // Print the assistant content
        Console.WriteLine("[ASSISTANT]: " + response.Messages[0].Text);
    }
}
