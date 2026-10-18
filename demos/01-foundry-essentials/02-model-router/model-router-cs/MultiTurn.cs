using Azure.AI.Projects;
using Azure.Identity;
using OpenAI.Chat;

public static class MultiTurn
{
    public static async Task Run()
    {
        AIProjectClient project = new(
            new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
            new DefaultAzureCredential());

        ChatClient chat = project.ProjectOpenAIClient.GetChatClient(Environment.GetEnvironmentVariable("MODEL_ROUTER_DEPLOYMENT_NAME")!);

        List<ChatMessage> messages = [new SystemChatMessage("You are a concise travel planner.")];
        string[] turns =
        [
            "Plan a two-day food trip to Vienna.",
            "Swap day two for a day trip to Salzburg.",
            "List the packing essentials as five bullet points.",
        ];

        foreach (var turn in turns)
        {
            messages.Add(new UserChatMessage(turn));
            ChatCompletion completion = await chat.CompleteChatAsync(messages);
            var answer = completion.Content[0].Text;
            messages.Add(new AssistantChatMessage(answer));
            Console.WriteLine($"user      : {turn}");
            Console.WriteLine($"served by : {completion.Model}");
            Console.WriteLine($"answer    : {answer.Replace("\n", " ")[..120]}...");
            Console.WriteLine();
        }
    }
}
