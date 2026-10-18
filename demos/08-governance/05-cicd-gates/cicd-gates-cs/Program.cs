DotNetEnv.Env.TraversePath().Load();

Environment.ExitCode = args.FirstOrDefault() switch
{
    "compat" => Compatibility.Run(),
    "canary" => await CanaryGate.Run(args.Skip(1).FirstOrDefault() == "regress"),
    "regression" => await PromptRegression.Run(),
    _ => Usage(),
};

static int Usage()
{
    Console.WriteLine("dotnet run -- compat | canary [regress] | regression");
    return 2;
}
