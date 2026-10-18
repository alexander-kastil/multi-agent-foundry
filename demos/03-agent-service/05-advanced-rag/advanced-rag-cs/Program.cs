using System.Globalization;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
Console.OutputEncoding = System.Text.Encoding.UTF8;
DotNetEnv.Env.TraversePath().Load();

switch (args.FirstOrDefault())
{
    case "hybrid":
        await HybridDemo.Run();
        break;
    case "routing":
        await RoutingDemo.Run();
        break;
    case "chunking":
        await ChunkingDemo.Run();
        break;
    case "search":
        await SearchDemo.Run();
        break;
    default:
        Console.WriteLine("Usage: dotnet run -- hybrid | routing | chunking | search");
        break;
}
