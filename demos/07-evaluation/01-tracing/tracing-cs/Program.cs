DotNetEnv.Env.TraversePath().Load();

switch (args.FirstOrDefault())
{
    case "agent": await AgentTrace.Run(); break;
    case "distributed": await Distributed.Run(); break;
    case "anomaly": Anomaly.Run(); break;
    default: Console.WriteLine("dotnet run -- agent | distributed | anomaly"); break;
}
