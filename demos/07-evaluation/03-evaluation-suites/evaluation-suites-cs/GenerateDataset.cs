using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

static class GenerateDataset
{
    static readonly Dictionary<string, string> Categories = new()
    {
        ["refund_calculation"] = "customer asks how much money they get back for an item paid with a discount or partial payment",
        ["returns_processing"] = "customer wants to return an item and get a refund",
        ["order_modification"] = "customer wants to change an order or shipping address",
    };

    static readonly string[] Personas = ["first-time buyer", "premium repeat customer"];

    static readonly string[] Adversarial = ["none", "vague request", "policy edge case"];

    const string Prompt = """
        Generate one realistic customer service test case for a chocolate e-commerce company.
        Company policy: {policy}
        Interaction category: {category} ({description})
        Customer persona: {persona}
        Adversarial condition: {adversarial}

        Reply with JSON only and no markdown:
        {
          "customer_request": "the opening message of the customer, with concrete order details such as days since delivery or amount paid",
          "ground_truth": {
            "expected_action": "what a correct agent must do under the policy",
            "must_include": ["facts or amounts the answer must contain"],
            "should_refuse": true or false
          }
        }
        """;

    public static async Task<int> Run()
    {
        IChatClient client = Foundry.ChatClient(Foundry.Model);
        var combinations = (from category in Categories.Keys from persona in Personas from adversarial in Adversarial select (category, persona, adversarial)).ToList();
        JsonNode[] cases = new JsonNode[combinations.Count];

        await Parallel.ForEachAsync(Enumerable.Range(0, combinations.Count), new ParallelOptions { MaxDegreeOfParallelism = 6 }, async (index, token) =>
        {
            var (category, persona, adversarial) = combinations[index];
            string prompt = Prompt.Replace("{policy}", Policy.Text).Replace("{category}", category)
                .Replace("{description}", Categories[category]).Replace("{persona}", persona).Replace("{adversarial}", adversarial);
            ChatResponse reply = await client.GetResponseAsync(prompt, cancellationToken: token);
            JsonObject item = Foundry.ParseJson(reply.Text).AsObject();
            item["id"] = $"case-{index:000}";
            item["interaction_type"] = category;
            item["persona"] = persona;
            item["adversarial"] = adversarial;
            item["canary"] = adversarial == "none";
            cases[index] = item;
        });

        File.WriteAllText("synthetic_test_suite.json", new JsonArray(cases).ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Generated {cases.Length} cases, {cases.Count(c => (bool)c["canary"]!)} canaries");
        return 0;
    }
}
