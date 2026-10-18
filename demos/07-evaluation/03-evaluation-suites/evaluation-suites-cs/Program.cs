DotNetEnv.Env.TraversePath().Load();

Environment.ExitCode = args.FirstOrDefault() switch
{
    "generate" => await GenerateDataset.Run(),
    "calibrate" => await Calibrate.Run(),
    "gate" => await RegressionGate.Run(args.Skip(1).ToArray()),
    "agent-eval" => await AgentEval.Run(),
    _ => Usage(),
};

static int Usage()
{
    Console.WriteLine("dotnet run -- generate | calibrate | gate [--save-baseline] [--candidate v1] | agent-eval");
    return 2;
}
