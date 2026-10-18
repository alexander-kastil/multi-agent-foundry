using DotNetEnv;

Env.Load();

switch (args.FirstOrDefault())
{
    case "endpoint":
        Endpoint.Run();
        break;
    case "deployments":
        Deployments.Run();
        break;
    case "identity":
        Identity.Run();
        break;
    case "smoke":
        await Smoke.Run();
        break;
}
