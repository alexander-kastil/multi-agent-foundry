using System.Globalization;
using DotNetEnv;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
Env.Load();

switch (args.FirstOrDefault())
{
    case "list":
        ListDeployments.Run();
        break;
    case "route":
        await RouteOnce.Run();
        break;
    case "compare":
        await Compare.Run(args.Skip(1));
        break;
    case "multi-turn":
        await MultiTurn.Run();
        break;
}
