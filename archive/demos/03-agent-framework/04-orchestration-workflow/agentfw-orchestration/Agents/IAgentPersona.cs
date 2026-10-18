namespace AFWOrchestration;

public interface IAgentPersona
{
    static abstract string Name { get; }
    static abstract string Instructions { get; }
    static abstract string Description { get; }
}
