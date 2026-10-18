using Azure.AI.Projects;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;

// Load configuration from appsettings.json
IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var projectEndpoint = configuration["ProjectEndpoint"];
var evalModel = configuration["EvalModel"];
var azureOpenAIEndpoint = configuration["AzureOpenAIEndpoint"];

if (string.IsNullOrWhiteSpace(projectEndpoint) || string.IsNullOrWhiteSpace(evalModel) ||
    string.IsNullOrWhiteSpace(azureOpenAIEndpoint))
{
    Console.WriteLine("Configure ProjectEndpoint, EvalModel, and AzureOpenAIEndpoint in appsettings.json before running the sample.");
    return;
}

// Resolve credential once so both client and evaluator share it
var credential = new DefaultAzureCredential();

// Create project client (useful when you want to interact with the project later)
var projectClient = new AIProjectClient(new Uri(projectEndpoint), credential);

// Create Azure OpenAI client for evaluators
var azureOpenAIClient = new AzureOpenAIClient(new Uri(azureOpenAIEndpoint), credential);
IChatClient chatClient = azureOpenAIClient.GetChatClient(evalModel).AsIChatClient();

// Quick diagnostics
Console.WriteLine($"[eval] using deployment='{evalModel}' endpoint='{azureOpenAIEndpoint}' api_version='2024-10-21'");

// Example conversation with image URL components
var query = "Can you describe this image?";
var response = "The image shows a person with short dark hair wearing a blue checkered shirt. The background appears to be a wall with shadows cast on it";
var context = "You are an AI assistant that understands images.";

// Create chat messages for evaluation
var messages = new List<ChatMessage>
{
    new ChatMessage(ChatRole.System, context),
    new ChatMessage(ChatRole.User, query)
};

// Create the model response
var modelResponse = new ChatResponse(
    new ChatMessage(ChatRole.Assistant, response)
);

// Create chat configuration for LLM-based evaluators
var chatConfiguration = new ChatConfiguration(chatClient);

// Create evaluators
var groundednessEvaluator = new GroundednessEvaluator();
var relevanceEvaluator = new RelevanceEvaluator();

// Helper to print metric details using reflection (works across SDK versions)
void PrintMetric(object metric)
{
    if (metric == null)
    {
        Console.WriteLine("Metric: <null>");
        return;
    }

#pragma warning disable IL2075 // Reflection on obj.GetType() for dynamic property access
    var t = metric.GetType();

    // Get Value
    var valueProp = t.GetProperty("Value");
    var value = valueProp?.GetValue(metric);
    if (value != null)
    {
        Console.WriteLine($"Score: {value}");
    }

    // Get Reason
    var reasonProp = t.GetProperty("Reason");
    var reason = reasonProp?.GetValue(metric);
    if (reason != null && !string.IsNullOrEmpty(reason.ToString()))
    {
        Console.WriteLine($"Reason: {reason}");
    }

    // Get Interpretation
    var interpProp = t.GetProperty("Interpretation");
    var interp = interpProp?.GetValue(metric);
    if (interp != null)
    {
        var interpType = interp.GetType();
        var ratingProp = interpType.GetProperty("Rating");
        var rating = ratingProp?.GetValue(interp);
        if (rating != null)
        {
            Console.WriteLine($"Rating: {rating}");
        }

        var interpReasonProp = interpType.GetProperty("Reason");
        var interpReason = interpReasonProp?.GetValue(interp);
        if (interpReason != null && !string.IsNullOrEmpty(interpReason.ToString()))
        {
            Console.WriteLine($"Interpretation: {interpReason}");
        }
    }
#pragma warning restore IL2075
}
// Create evaluation context for groundedness (includes the context/grounding data)
var groundingContext = new List<EvaluationContext>
{
    new GroundednessEvaluatorContext(context)
};

// Groundedness evaluation
Console.WriteLine("\n=== Groundedness Evaluation ===");
try
{
    var groundednessResult = await groundednessEvaluator.EvaluateAsync(
        messages: messages,
        modelResponse: modelResponse,
        chatConfiguration: chatConfiguration,
        additionalContext: groundingContext,
        cancellationToken: CancellationToken.None
    );

    // Get the groundedness metric from the result using TryGet
    if (groundednessResult.TryGet(GroundednessEvaluator.GroundednessMetricName, out NumericMetric? groundednessMetric))
    {
        PrintMetric(groundednessMetric);
    }
    else
    {
        Console.WriteLine("Groundedness metric not found in result");
    }

}
catch (Exception ex)
{
    Console.WriteLine($"Groundedness evaluation failed: {ex.Message}");
}

// Relevance evaluation
Console.WriteLine("\n=== Relevance Evaluation ===");
try
{
    var relevanceResult = await relevanceEvaluator.EvaluateAsync(
        messages: messages,
        modelResponse: modelResponse,
        chatConfiguration: chatConfiguration,
        additionalContext: null,
        cancellationToken: CancellationToken.None
    );

    // Get the relevance metric from the result using TryGet
    if (relevanceResult.TryGet(RelevanceEvaluator.RelevanceMetricName, out NumericMetric? relevanceMetric))
    {
        PrintMetric(relevanceMetric);
    }
    else
    {
        Console.WriteLine("Relevance metric not found in result");
    }

}
catch (Exception ex)
{
    Console.WriteLine($"Relevance evaluation failed: {ex.Message}");
}

// Content Safety evaluation
Console.WriteLine("\n=== Content Safety Evaluation ===");
Console.WriteLine("Note: Content safety evaluation requires Azure AI Content Safety configuration.");
Console.WriteLine("To enable content safety evaluation, configure the following in appsettings.json:");
Console.WriteLine("- Azure AI Foundry project endpoint or subscription/resource group/project name");
Console.WriteLine("For more information, see: https://learn.microsoft.com/en-us/azure/ai-foundry/");
Console.WriteLine("\nContent safety would evaluate for:");
Console.WriteLine("- Hate and fairness");
Console.WriteLine("- Sexual content");
Console.WriteLine("- Violence");
Console.WriteLine("- Self-harm");