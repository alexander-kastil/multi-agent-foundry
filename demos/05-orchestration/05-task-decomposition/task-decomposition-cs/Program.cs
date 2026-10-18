Console.OutputEncoding = System.Text.Encoding.UTF8;
DotNetEnv.Env.TraversePath().Load();

Dictionary<string, (string Title, Func<Task> Strategy)> strategies = new()
{
    ["chain"] = ("Prompt chain with branching and validation", PromptChain.Run),
    ["plan"] = ("Dynamic adaptive decomposition", Planner.Run),
    ["handoff"] = ("Handoff collaboration", HandoffCollaboration.Run),
};

if (args.FirstOrDefault() == "cycle")
{
    await Planner.CycleCheck();
    return;
}

foreach (string key in args.Length > 0 ? args : strategies.Keys.ToArray())
{
    var (title, strategy) = strategies[key];
    Console.WriteLine($"\n{title}");
    await strategy();
}
