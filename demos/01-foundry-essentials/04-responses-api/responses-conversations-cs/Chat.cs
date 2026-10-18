using Azure.AI.Extensions.OpenAI;
using OpenAI.Responses;

#pragma warning disable OPENAI001

public static class Chat
{
    public static async Task Run(IEnumerable<string> questions)
    {
        ProjectConversationCreationOptions options = new() { Metadata = { ["demo"] = "m1-chat" } };
        ProjectConversation conversation = await Session.Conversations.CreateProjectConversationAsync(options);
        Console.WriteLine($"conversation: {conversation.Id}");

        var responses = Session.Responses(conversation.Id);
        foreach (var question in questions)
        {
            Console.WriteLine($"\nyou: {question}");
            Console.Write("bot: ");
            await foreach (StreamingResponseUpdate update in responses.CreateResponseStreamingAsync(new CreateResponseOptions
            {
                Instructions = "You are a friendly assistant. Answer in at most two sentences.",
                InputItems = { ResponseItem.CreateUserMessageItem(question) },
            }))
            {
                if (update is StreamingResponseOutputTextDeltaUpdate delta)
                {
                    Console.Write(delta.Delta);
                }
            }
            Console.WriteLine();
        }

        var count = Session.Conversations.GetProjectConversationItems(conversation.Id).Count();
        Console.WriteLine($"\nitems stored: {count}");
        await Session.Conversations.DeleteConversationAsync(conversation.Id, null);
        Console.WriteLine("conversation deleted");
    }
}
