using System.Globalization;
using DotNetEnv;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
Env.Load();

switch (args.FirstOrDefault())
{
    case "budget":
        await Budget.Run();
        break;
    case "trim":
        await Trim.Run();
        break;
    case "compact":
        await Compact.Run();
        break;
    case "server":
        await ServerCompaction.Run();
        break;
    case "cache":
        await Caching.Run();
        break;
}
