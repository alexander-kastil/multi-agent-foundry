DotNetEnv.Env.TraversePath().Load();

switch (args.FirstOrDefault())
{
    case "scan": ScanProbe.Run(); break;
    case "defense": await InjectionDefense.Run(); break;
    default: Console.WriteLine("dotnet run -- scan | defense"); break;
}
