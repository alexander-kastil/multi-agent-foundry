using Azure.AI.Projects;
using Azure.Identity;

public static class Deployments
{
    public static void Run()
    {
        AIProjectClient project = new(
            new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
            new DefaultAzureCredential());

        foreach (ModelDeployment deployment in project.Deployments.GetDeployments().OfType<ModelDeployment>())
        {
            Console.WriteLine($"{deployment.Name,-28} {deployment.ModelName,-28} {deployment.ModelVersion,-12} {deployment.Sku.Name}");
        }

        Console.WriteLine();
        var connections = project.Connections.GetConnections().ToList();
        Console.WriteLine($"Connections on the project: {connections.Count}");
        foreach (AIProjectConnection connection in connections)
        {
            Console.WriteLine($"{connection.Name,-28} {connection.Type}");
        }
    }
}
