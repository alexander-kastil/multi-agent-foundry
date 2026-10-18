using DotNetEnv;

Env.Load();

switch (args.FirstOrDefault())
{
    case "tools":
        await ListTools.Run();
        break;
    case "read":
        await ReadTools.Run(args.ElementAtOrDefault(1) ?? "m1-router-subset");
        break;
    case "lifecycle":
        await AgentLifecycle.Run();
        break;
}
