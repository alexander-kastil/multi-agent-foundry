using Azure.AI.Projects.Agents;

static class VersionSelector
{
    const string Name = "m8h-life-selector";
    const string Question = "Is eval(user_input) a vulnerability?";

    static string Flat(string text, int length)
    {
        string flat = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return flat[..Math.Min(length, flat.Length)];
    }

    public static async Task Run()
    {
        ProjectsAgentVersion stable = await Foundry.CreateAgent(Name, File.ReadAllText("prompts/security-analyzer-v5.1.txt"), new() { ["prompt_version"] = "security-prompt-v5.1" });
        Console.WriteLine($"created v{stable.Version}, endpoint selector: {await Foundry.ServedVersion(Name)}");
        Console.WriteLine($"  endpoint answers: {Flat(await Foundry.AskEndpoint(Name, Question), 110)}");

        await Foundry.PinEndpoint(Name, stable.Version);
        ProjectsAgentVersion candidate = await Foundry.CreateAgent(Name, File.ReadAllText("prompts/security-analyzer-v5.2.txt"), new() { ["prompt_version"] = "security-prompt-v5.2" });
        Console.WriteLine($"\npinned to v{stable.Version}, then created v{candidate.Version}, endpoint selector: {await Foundry.ServedVersion(Name)}");
        Console.WriteLine($"  endpoint answers:  {Flat(await Foundry.AskEndpoint(Name, Question), 110)}");
        Console.WriteLine($"  candidate v{candidate.Version} tested by reference: {Flat(await Foundry.Ask(candidate, Question), 110)}");

        await Foundry.PinEndpoint(Name, candidate.Version);
        Console.WriteLine($"\npromoted, endpoint selector: {await Foundry.ServedVersion(Name)}");
        Console.WriteLine($"  endpoint answers: {Flat(await Foundry.AskEndpoint(Name, Question), 110)}");

        await Foundry.PinEndpoint(Name, stable.Version);
        Console.WriteLine($"\nrolled back, endpoint selector: {await Foundry.ServedVersion(Name)}");
        Console.WriteLine($"  endpoint answers: {Flat(await Foundry.AskEndpoint(Name, Question), 110)}");

        await Foundry.DeleteAgent(candidate);
        await Foundry.DeleteAgent(stable);
    }
}
