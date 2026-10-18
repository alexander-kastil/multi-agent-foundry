using Azure.Identity;
using OpenAI.Chat;
using System.ClientModel.Primitives;
using OpenAI;

#pragma warning disable OPENAI001 // currently required for token based authentication

public class ChatRunner
{
    public void Run(string model, string endpoint)
    {
        BearerTokenPolicy tokenPolicy = new(
            new DefaultAzureCredential(),
            "https://cognitiveservices.azure.com/.default");

        ChatClient client = new(
            model: model,
            authenticationPolicy: tokenPolicy,
            options: new OpenAIClientOptions()
            {
                Endpoint = new Uri(endpoint + "/openai/v1")
            }
        );

        ChatCompletionOptions options = new ChatCompletionOptions
        {
            ReasoningEffortLevel = ChatReasoningEffortLevel.Low,
            MaxOutputTokenCount = 100000
        };

        ChatCompletion completion = client.CompleteChat(
            new DeveloperChatMessage("You are a helpful assistant that knows about gym, workouts, and fitness."),
            new UserChatMessage("What kinds of protein do you know about? Just a short list please.")
        );

        Console.WriteLine($"[ASSISTANT]: {completion.Content[0].Text}");
    }
}
