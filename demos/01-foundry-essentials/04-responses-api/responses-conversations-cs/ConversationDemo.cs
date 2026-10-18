using Azure.AI.Extensions.OpenAI;
using OpenAI.Responses;

#pragma warning disable OPENAI001

public static class ConversationDemo
{
    public static async Task Run()
    {
        ProjectConversationCreationOptions options = new()
        {
            Items = { ResponseItem.CreateUserMessageItem("I am planning a trip from Vienna.") },
            Metadata = { ["demo"] = "m1-conversation" },
        };
        ProjectConversation conversation = await Session.Conversations.CreateProjectConversationAsync(options);
        Console.WriteLine($"conversation : {conversation.Id}");

        var responses = Session.Responses(conversation.Id);
        foreach (var question in new[] { "Which country is my departure city in?", "And which river runs through it?" })
        {
            ResponseResult response = await responses.CreateResponseAsync(new CreateResponseOptions
            {
                Instructions = "Answer in one short sentence.",
                InputItems = { ResponseItem.CreateUserMessageItem(question) },
            });
            Console.WriteLine($"user         : {question}");
            Console.WriteLine($"assistant    : {response.GetOutputText()}");
        }

        await Session.Conversations.CreateProjectConversationItemsAsync(
            conversation.Id,
            [ResponseItem.CreateUserMessageItem("Add Linz as a second stop.")]);

        Console.WriteLine();
        foreach (AgentResponseItem item in Session.Conversations.GetProjectConversationItems(conversation.Id, order: "asc"))
        {
            var message = item.AsResponseResultItem() as MessageResponseItem;
            Console.WriteLine($"{"message",-10} {message?.Role.ToString().ToLowerInvariant() ?? "-",-10} {item.Id}");
        }

        await Session.Conversations.DeleteConversationAsync(conversation.Id, null);
        Console.WriteLine();
        Console.WriteLine("conversation deleted");
    }
}
