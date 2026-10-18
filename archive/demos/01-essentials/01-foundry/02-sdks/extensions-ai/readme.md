#

Scaffold a new .NET console application

```bash
dotnet new console -n extensions-ai
cd extensions-ai
```

Add packages:

```bash
dotnet add package Microsoft.Extensions.AI --version 9.10.2
dotnet add package Microsoft.Extensions.AI.OpenAI --prerelease
dotnet add package OpenAI
dotnet add package Azure.Identity
```
