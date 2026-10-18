using System.Text.RegularExpressions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

public sealed partial class PreferenceProvider : AIContextProvider
{
    private readonly ProviderSessionState<State> _sessionState = new(_ => new State(), nameof(PreferenceProvider));

    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var preferences = _sessionState.GetOrInitializeState(context.Session).Preferences;
        if (preferences.Count == 0)
        {
            return new ValueTask<AIContext>(new AIContext());
        }

        var known = string.Join("; ", preferences.Select(p => $"{p.Key}: {p.Value}"));
        return new ValueTask<AIContext>(new AIContext { Instructions = $"Known user preferences: {known}." });
    }

    protected override ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        var state = _sessionState.GetOrInitializeState(context.Session);
        foreach (var message in context.RequestMessages)
        {
            foreach (Match match in FavoritePattern().Matches(message.Text ?? string.Empty))
            {
                state.Preferences[match.Groups[1].Value.ToLowerInvariant()] = match.Groups[2].Value.Trim();
            }
        }

        _sessionState.SaveState(context.Session, state);
        return default;
    }

    [GeneratedRegex(@"my favorite (\w+(?: \w+)?) is ([^.!?]+)", RegexOptions.IgnoreCase)]
    private static partial Regex FavoritePattern();

    public sealed class State
    {
        public Dictionary<string, string> Preferences { get; set; } = [];
    }
}
