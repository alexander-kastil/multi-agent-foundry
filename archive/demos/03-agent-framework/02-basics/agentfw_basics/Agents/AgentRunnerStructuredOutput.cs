using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using AgentFwBasics.Models;
using OpenAI.Chat;

namespace AgentFwBasics.Agents;

public class AgentRunnerStructuredOutput(AppConfig config)
{
    // Define structured output model
    public class PersonInfo
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
        public string? Occupation { get; set; }
        public string? City { get; set; }
    }

    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("DEMO: Structured Output with JSON Schema");
        Console.WriteLine(new string('=', 70));

        if (string.IsNullOrWhiteSpace(config.AzureOpenAIEndpoint) || string.IsNullOrWhiteSpace(config.AzureOpenAIApiKey))
        {
            Console.WriteLine("\n❌ Azure OpenAI configuration is missing.");
            Console.WriteLine("Please set AzureOpenAIEndpoint and AzureOpenAIApiKey in appsettings.json");
            return;
        }

        var client = new AzureOpenAIClient(
            new Uri(config.AzureOpenAIEndpoint),
            new AzureKeyCredential(config.AzureOpenAIApiKey)
        );

        var chatClient = client.GetChatClient(config.AzureOpenAIChatDeploymentName);

        Console.WriteLine("\nAgent created with PersonInfo schema");
        Console.WriteLine("Schema: name, age, occupation, city");

        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("Interactive Chat (Type 'quit' to exit)");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("\nTIP: Describe a person and see structured extraction\n");

        while (true)
        {
            Console.Write("You: ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
                continue;

            if (userInput.Trim().ToLower() is "quit" or "exit" or "q")
            {
                Console.WriteLine("\nGoodbye!");
                break;
            }

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("Extract person information from the user's text. Return ONLY a JSON object with fields: name, age, occupation, city. Use null for missing values."),
                new UserChatMessage(userInput)
            };

            Console.WriteLine("\nExtracting structured data...");

            var response = await chatClient.CompleteChatAsync(messages);
            var content = response.Value.Content[0].Text;

            try
            {
                var person = JsonSerializer.Deserialize<PersonInfo>(content);

                if (person != null)
                {
                    Console.WriteLine("\nExtracted Information:");
                    Console.WriteLine($"   Name: {person.Name ?? "N/A"}");
                    Console.WriteLine($"   Age: {person.Age?.ToString() ?? "N/A"}");
                    Console.WriteLine($"   Occupation: {person.Occupation ?? "N/A"}");
                    Console.WriteLine($"   City: {person.City ?? "N/A"}");
                }
                else
                {
                    Console.WriteLine("Could not extract information. Please try rephrasing your request with clearer person information (name, age, occupation, city).");
                }
            }
            catch (JsonException)
            {
                Console.WriteLine($"Response was not valid JSON: {content}");
            }

            Console.WriteLine();
        }
    }
}
