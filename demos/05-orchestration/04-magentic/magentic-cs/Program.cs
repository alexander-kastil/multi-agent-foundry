Console.OutputEncoding = System.Text.Encoding.UTF8;
DotNetEnv.Env.TraversePath().Load();

switch (args.FirstOrDefault())
{
    case "groupchat":
        await GroupChat.Run();
        break;
    case "magentic":
        await Magentic.Run(useResponses: args.Skip(1).FirstOrDefault() == "responses");
        break;
    default:
        Console.WriteLine("Usage: dotnet run -- groupchat | magentic [responses]");
        break;
}
