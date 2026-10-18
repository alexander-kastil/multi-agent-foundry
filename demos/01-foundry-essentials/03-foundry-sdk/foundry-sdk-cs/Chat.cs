using Microsoft.Extensions.AI;

public static class Chat
{
    private static readonly List<ChatMessage> Messages =
    [
        new(ChatRole.System, "You are a helpful assistant that knows about gym, workouts, and fitness."),
        new(ChatRole.User, "What kinds of protein do you know about? Just a short list please.")
    ];

    public static async Task Run(IChatClient client)
    {
        ChatResponse response = await client.GetResponseAsync(Messages);
        Console.WriteLine($"model : {response.ModelId}");
        Console.WriteLine($"answer: {response.Text}");
        Console.WriteLine($"tokens: {response.Usage?.TotalTokenCount}");
    }

    public static async Task Stream(IChatClient client)
    {
        Console.Write("answer: ");
        await foreach (ChatResponseUpdate update in client.GetStreamingResponseAsync(Messages))
        {
            Console.Write(update.Text);
        }
        Console.WriteLine();
    }
}
