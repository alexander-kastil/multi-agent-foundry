using OpenAI.Responses;

#pragma warning disable OPENAI001

public static class ChainResponses
{
    public static async Task Run()
    {
        var responses = Session.Responses();

        ResponseResult first = await responses.CreateResponseAsync("My favourite city is Graz. Remember that.");
        ResponseResult second = await responses.CreateResponseAsync(new CreateResponseOptions
        {
            PreviousResponseId = first.Id,
            InputItems = { ResponseItem.CreateUserMessageItem("Which city did I say was my favourite?") },
        });
        Console.WriteLine($"chained   : {second.GetOutputText()}");

        ResponseResult statelessFirst = await responses.CreateResponseAsync(new CreateResponseOptions
        {
            StoredOutputEnabled = false,
            InputItems = { ResponseItem.CreateUserMessageItem("My favourite city is Graz. Remember that.") },
        });
        ResponseResult statelessSecond = await responses.CreateResponseAsync(new CreateResponseOptions
        {
            StoredOutputEnabled = false,
            InputItems =
            {
                ResponseItem.CreateUserMessageItem("My favourite city is Graz. Remember that."),
                ResponseItem.CreateAssistantMessageItem(statelessFirst.GetOutputText()),
                ResponseItem.CreateUserMessageItem("Which city did I say was my favourite?"),
            },
        });
        Console.WriteLine($"stateless : {statelessSecond.GetOutputText()}");

        foreach (var id in new[] { first.Id, second.Id })
        {
            await responses.DeleteResponseAsync(id);
        }
    }
}
