using System.Text.Json;

Console.OutputEncoding = System.Text.Encoding.UTF8;
DotNetEnv.Env.TraversePath().Load();

const string Query = "Prepare a research report on adding equities exposure to the Contoso growth portfolio.";
const string WeakReport = "We looked at the market. Equities seem fine.";
TimeSpan Seconds(double value) => TimeSpan.FromSeconds(value);

Dictionary<string, TimeSpan> healthy = new() { ["market_analysis"] = Seconds(90), ["risk_assessment"] = Seconds(90), ["esg_evaluation"] = Seconds(90) };
Dictionary<string, TimeSpan> oneSlow = new() { ["market_analysis"] = Seconds(90), ["risk_assessment"] = Seconds(90), ["esg_evaluation"] = Seconds(0.01) };
Dictionary<string, TimeSpan> degraded = new() { ["market_analysis"] = Seconds(0.01), ["risk_assessment"] = Seconds(90), ["esg_evaluation"] = Seconds(0.01) };

Dictionary<string, Func<Task>> stages = new()
{
    ["hub"] = async () => Console.WriteLine(await Hub.HubAndSpoke(Query)),
    ["quorum"] = async () =>
    {
        Console.WriteLine("All spokes healthy");
        Console.WriteLine(JsonSerializer.Serialize(await Quorum.ParallelResearch("Review MSFT for the growth portfolio.", healthy)));
        Console.WriteLine("ESG times out, two of three is enough");
        Console.WriteLine(JsonSerializer.Serialize(await Quorum.ParallelResearch("Review MSFT for the growth portfolio.", oneSlow)));
        Console.WriteLine("Market analysis and ESG time out");
        Console.WriteLine(JsonSerializer.Serialize(await Quorum.ParallelResearch("Review MSFT for the growth portfolio.", degraded)));
    },
    ["circuit"] = () => Quorum.TripCircuit("compliance_check"),
    ["supervisor"] = async () =>
    {
        var (approved, attempts) = await Supervisor.Supervise(WeakReport);
        Console.WriteLine($"Approved: {approved} after {attempts} attempt(s)");
    },
};

foreach (string key in args.Length > 0 ? args : stages.Keys.ToArray())
{
    Console.WriteLine($"\n{key}");
    await stages[key]();
}
