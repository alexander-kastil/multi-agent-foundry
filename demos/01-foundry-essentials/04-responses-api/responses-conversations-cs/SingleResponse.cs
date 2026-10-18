using OpenAI.Responses;

#pragma warning disable OPENAI001

public static class SingleResponse
{
    public static async Task Run()
    {
        var responses = Session.Responses();

        ResponseResult response = await responses.CreateResponseAsync(new CreateResponseOptions
        {
            Instructions = "You are a concise travel assistant.",
            InputItems = { ResponseItem.CreateUserMessageItem("Name one must-see place in Salzburg.") },
        });

        Console.WriteLine($"id      : {response.Id}");
        Console.WriteLine($"status  : {response.Status}");
        Console.WriteLine($"output  : [{string.Join(", ", response.OutputItems.Select(item => item is MessageResponseItem ? "message" : item.GetType().Name))}]");
        Console.WriteLine($"text    : {response.GetOutputText()}");

        ResponseResult stored = await responses.GetResponseAsync(response.Id);
        Console.WriteLine($"stored  : {stored.Id == response.Id}");

        await responses.DeleteResponseAsync(response.Id);
        Console.WriteLine("deleted : True");
    }
}
