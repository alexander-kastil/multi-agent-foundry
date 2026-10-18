DotNetEnv.Env.TraversePath().Load();

switch (args.FirstOrDefault())
{
    case "probe": await FairnessProbe.Run(); break;
    case "report": AuditReport.Run(); break;
    default: Console.WriteLine("dotnet run -- probe | report"); break;
}
