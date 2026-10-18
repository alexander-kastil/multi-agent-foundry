DotNetEnv.Env.TraversePath().Load();

switch (args.FirstOrDefault())
{
    case "guarded": await GuardedAgent.Run(); break;
    case "tool": await ToolResponse.Run(); break;
    case "shields": await PromptShields.Run(); break;
    case "adherence": await TaskAdherence.Run(); break;
    default: Console.WriteLine("dotnet run -- guarded | tool | shields | adherence"); break;
}
