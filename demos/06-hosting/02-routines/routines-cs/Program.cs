using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;

DotNetEnv.Env.TraversePath().Load();

string agentName = Environment.GetEnvironmentVariable("AGENT_NAME")!;
AIProjectClient project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());
AIProjectRoutines routines = project.Routines;

switch (args.FirstOrDefault())
{
    case "agent":
        ProjectsAgentVersion agent = await project.AgentAdministrationClient.CreateAgentVersionAsync(
            agentName: agentName,
            options: new(new DeclarativeAgentDefinition(Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!)
            {
                Instructions = "You write the Contoso support desk digest. Given a request, answer with a three line digest: open tickets, risks, next action.",
            }));
        Console.WriteLine($"agent {agent.Name} version {agent.Version}");
        break;

    case "routines":
        ProjectsRoutineOptions scheduleOptions = new(
            action: new AgentResponsesApiRoutineAction
            {
                AgentName = agentName,
                Input = BinaryData.FromObjectAsJson("Write the support digest for the last 24 hours."),
            },
            description: "Weekday morning support digest.",
            enabled: false);
        scheduleOptions.Triggers.Add("weekday-morning", new ScheduleRoutineTrigger(cronExpression: "0 7 * * 1-5", timeZone: "Europe/Vienna"));
        ProjectsRoutine schedule = await routines.CreateOrUpdateAsync("m6h3-weekday-digest", scheduleOptions);
        Console.WriteLine($"routine {schedule.Name} enabled={schedule.IsEnabled}");

        DateTimeOffset fireAt = DateTimeOffset.UtcNow.AddMinutes(2);
        ProjectsRoutineOptions timerOptions = new(
            action: new AgentResponsesApiRoutineAction
            {
                AgentName = agentName,
                Input = BinaryData.FromObjectAsJson("Write the one-time support digest."),
            },
            description: "One-time digest two minutes from now.",
            enabled: true);
        timerOptions.Triggers.Add("once", new TimerRoutineTrigger { At = fireAt });
        ProjectsRoutine timer = await routines.CreateOrUpdateAsync("m6h3-one-shot", timerOptions);
        Console.WriteLine($"routine {timer.Name} enabled={timer.IsEnabled} fires at {fireAt:yyyy-MM-ddTHH:mm:sszzz}");
        break;

    case "dispatch":
    case "history":
        if (args[0] == "dispatch")
        {
            DispatchRoutineResult result = await routines.DispatchAsync(
                name: "m6h3-weekday-digest",
                payload: new AgentResponsesApiDispatchPayload(BinaryData.FromObjectAsJson("Run the digest now for a test.")));
            Console.WriteLine($"dispatch_id: {result.DispatchId}");
            await Task.Delay(TimeSpan.FromSeconds(20));
        }

        await foreach (ProjectsRoutine routine in routines.GetRoutinesAsync())
        {
            Console.WriteLine($"{routine.Name}  enabled={routine.IsEnabled}  triggers=[{string.Join(", ", routine.Triggers.Keys)}]");
            await foreach (RoutineRun run in routines.GetRoutineRunsAsync(routine.Name))
            {
                Console.WriteLine($"  {run.Id}  phase={run.Phase}  source={run.AttemptSource}  started={run.StartedOn:yyyy-MM-dd HH:mm:sszzz}");
                Console.WriteLine($"    dispatch={run.DispatchId}  response={run.ResponseId ?? "None"}");
            }
        }
        break;

    case "cleanup":
        foreach (string name in new[] { "m6h3-weekday-digest", "m6h3-one-shot", "m6h3-azd-digest" })
        {
            await routines.DeleteAsync(name);
            Console.WriteLine($"deleted routine {name}");
        }
        await project.AgentAdministrationClient.DeleteAgentAsync(agentName);
        Console.WriteLine($"deleted agent {agentName}");
        break;
}
