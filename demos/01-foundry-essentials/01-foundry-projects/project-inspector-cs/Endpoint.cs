public static class Endpoint
{
    public static void Run()
    {
        var url = new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!);
        var resource = url.Host.Split('.')[0];
        var project = url.AbsolutePath.TrimEnd('/').Split('/')[^1];

        Console.WriteLine($"Foundry resource : {resource}");
        Console.WriteLine($"Project          : {project}");
        Console.WriteLine($"Host             : {url.Host}");
        Console.WriteLine($"Path             : {url.AbsolutePath}");
        Console.WriteLine($"Responses API    : {url.AbsoluteUri.TrimEnd('/')}/openai/v1/responses");
    }
}
