using Azure.AI.Projects;
using Azure.Identity;

public static class ListDeployments
{
    public static void Run()
    {
        AIProjectClient project = new(
            new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
            new DefaultAzureCredential());

        foreach (ModelDeployment deployment in project.Deployments.GetDeployments().OfType<ModelDeployment>())
        {
            if (deployment.ModelName == "model-router")
            {
                Console.WriteLine($"{deployment.Name,-22} {deployment.ModelName} {deployment.ModelVersion} {deployment.Sku.Name} capacity={deployment.Sku.Capacity}");
            }
        }
    }
}
