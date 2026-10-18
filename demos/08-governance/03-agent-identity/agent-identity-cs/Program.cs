DotNetEnv.Env.TraversePath().Load();

switch (args.FirstOrDefault())
{
    case "identity": await AgentIdentity.Run(); break;
    case "consent": await OAuthConsent.Run(); break;
    default: Console.WriteLine("dotnet run -- identity | consent"); break;
}
