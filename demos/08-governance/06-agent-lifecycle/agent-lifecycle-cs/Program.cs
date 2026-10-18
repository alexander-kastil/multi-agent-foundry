DotNetEnv.Env.TraversePath().Load();

switch (args.FirstOrDefault())
{
    case "governance": await VersionGovernance.Run(); break;
    case "selector": await VersionSelector.Run(); break;
    case "quota": await QuotaChargeback.Run(); break;
    case "retirement": await Retirement.Run(); break;
    default: Console.WriteLine("dotnet run -- governance | selector | quota | retirement"); break;
}
