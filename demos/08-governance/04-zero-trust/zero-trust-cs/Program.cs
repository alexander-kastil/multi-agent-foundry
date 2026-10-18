DotNetEnv.Env.TraversePath().Load();

switch (args.FirstOrDefault())
{
    case "flows": AuthFlows.Run(); break;
    case "isolation": await TenantIsolation.Run(); break;
    default: Console.WriteLine("dotnet run -- flows | isolation"); break;
}
