using Microsoft.Extensions.Configuration;

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

var model = configuration["Model"];
var endpoint = configuration["AzureOpenAIEndpoint"];

while (true)
{
    Console.WriteLine("\n=== Extensions.AI Demos ===");
    Console.WriteLine("1. ChatRunner - Basic chat using Microsoft.Extensions.AI");
    Console.WriteLine("2. ChatFunctionCalling - Function calling with Microsoft.Extensions.AI");
    Console.WriteLine("\nPress Ctrl+C to exit");
    Console.Write("\nSelect demo (1-2): ");

    var choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "1":
                Console.WriteLine("\n--- Running ChatRunner ---\n");
                ChatRunner chatRunner = new ChatRunner();
                await chatRunner.Run(model, endpoint);
                break;
            case "2":
                Console.WriteLine("\n--- Running ChatFunctionCalling ---\n");
                ChatFunctionCalling functionCalling = new ChatFunctionCalling();
                functionCalling.Run(model, endpoint);
                break;
            default:
                Console.WriteLine("Invalid selection. Please enter 1 or 2.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error running demo: {ex.Message}");
    }

    Console.WriteLine("\n" + new string('-', 50));
}