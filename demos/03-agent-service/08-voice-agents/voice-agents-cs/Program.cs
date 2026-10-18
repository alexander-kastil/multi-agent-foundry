using System.Text.Json;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.AI.VoiceLive;
using Azure.Identity;

DotNetEnv.Env.TraversePath().Load();

const string AgentName = "m3-voice-live-agent-cs";
string[] questions = ["Hello, I want to ask about the Summit 4S tent.", "Is it suitable for winter camping?"];

string voiceLiveConfig = JsonSerializer.Serialize(new
{
    session = new
    {
        voice = new { name = "en-US-Ava:DragonHDLatestNeural", type = "azure-standard", temperature = 0.8 },
        input_audio_transcription = new { model = "azure-speech" },
        turn_detection = new { type = "azure_semantic_vad", end_of_utterance_detection = new { model = "semantic_detection_v1_multilingual" } },
        input_audio_noise_reduction = new { type = "azure_deep_noise_suppression" },
        input_audio_echo_cancellation = new { type = "server_echo_cancellation" },
    },
});

Dictionary<string, string> ChunkConfig(string json, int limit = 512)
{
    Dictionary<string, string> metadata = new() { ["microsoft.voice-live.configuration"] = json[..Math.Min(json.Length, limit)] };
    string remaining = json.Length > limit ? json[limit..] : "";
    for (int number = 1; remaining.Length > 0; number++)
    {
        metadata[$"microsoft.voice-live.configuration.{number}"] = remaining[..Math.Min(remaining.Length, limit)];
        remaining = remaining.Length > limit ? remaining[limit..] : "";
    }
    return metadata;
}

AIProjectClient project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());

Console.WriteLine("== Create a text agent with Voice Live settings");
DeclarativeAgentDefinition definition = new(Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!)
{
    Instructions = "You are a voice assistant for Contoso Outdoor. Reply in one or two short spoken sentences. The Summit 4S is a four-season tent rated to minus 25 degrees Celsius.",
};
ProjectsAgentVersionCreationOptions options = new(definition);
foreach ((string key, string value) in ChunkConfig(voiceLiveConfig))
{
    options.Metadata[key] = value;
}
ProjectsAgentVersion agent = project.AgentAdministrationClient.CreateAgentVersion(AgentName, options);
Console.WriteLine($"Created {agent.Name}:{agent.Version}");

try
{
    Console.WriteLine("== Talk to it through the Voice Live API");
    VoiceLiveClient client = new(new Uri(Environment.GetEnvironmentVariable("VOICELIVE_ENDPOINT")!), new DefaultAzureCredential());
    AgentSessionConfig agentConfig = new(agent.Name, Environment.GetEnvironmentVariable("PROJECT_NAME")!) { AgentVersion = agent.Version };
    using VoiceLiveSession session = await client.StartSessionAsync(SessionTarget.FromAgent(agentConfig));

    await session.ConfigureSessionAsync(new VoiceLiveSessionOptions
    {
        InputAudioFormat = InputAudioFormat.Pcm16,
        OutputAudioFormat = OutputAudioFormat.Pcm16,
    });

    Queue<string> pending = new(questions);
    List<byte> audio = [];

    async Task Ask()
    {
        string question = pending.Dequeue();
        Console.WriteLine($"user: {question}");
        await session.SendCommandAsync(BinaryData.FromObjectAsJson(new
        {
            type = "conversation.item.create",
            item = new { type = "message", role = "user", content = new[] { new { type = "input_text", text = question } } },
        }));
        await session.SendCommandAsync(BinaryData.FromObjectAsJson(new { type = "response.create" }));
    }

    await foreach (SessionUpdate update in session.GetUpdatesAsync())
    {
        switch (update)
        {
            case SessionUpdateSessionUpdated:
                Console.WriteLine("session ready");
                await Ask();
                break;
            case SessionUpdateResponseAudioDelta delta when delta.Delta is not null:
                audio.AddRange(delta.Delta.ToArray());
                break;
            case SessionUpdateResponseAudioTranscriptDone transcript:
                Console.WriteLine($"agent: {transcript.Transcript}");
                break;
            case SessionUpdateError error:
                throw new InvalidOperationException(error.Error?.Message);
            case SessionUpdateResponseDone:
                if (pending.Count == 0) goto finished;
                await Ask();
                break;
        }
    }

finished:
    using FileStream file = File.Create("voice-live-reply.wav");
    using BinaryWriter writer = new(file);
    byte[] pcm = audio.ToArray();
    writer.Write("RIFF"u8); writer.Write(36 + pcm.Length); writer.Write("WAVEfmt "u8);
    writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(24000); writer.Write(48000); writer.Write((short)2); writer.Write((short)16);
    writer.Write("data"u8); writer.Write(pcm.Length); writer.Write(pcm);
    Console.WriteLine($"saved voice-live-reply.wav ({pcm.Length} bytes, PCM16 24 kHz mono)");
}
finally
{
    project.AgentAdministrationClient.DeleteAgent(AgentName);
    Console.WriteLine($"Deleted agent {AgentName}");
}
