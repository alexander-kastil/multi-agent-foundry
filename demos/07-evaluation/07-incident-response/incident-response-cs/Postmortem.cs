record ActionItem(string Item, string Owner, string Due);

record Incident(
    string Title,
    string Summary,
    string[] Timeline,
    string RootCause,
    string[] ContributingFactors,
    string[] WentWell,
    string[] WentBadly,
    ActionItem[] Actions);

static class Postmortem
{
    static string Bullet(IEnumerable<string> items) => string.Join("\n", items.Select(item => $"- {item}"));

    public static string Write(string path, Incident incident)
    {
        string content = $"""
            # Postmortem: {incident.Title}

            ## Incident summary
            {incident.Summary}

            ## Timeline
            {Bullet(incident.Timeline)}

            ## Root cause
            {incident.RootCause}

            ## Contributing factors
            {Bullet(incident.ContributingFactors)}

            ## What went well
            {Bullet(incident.WentWell)}

            ## What did not go well
            {Bullet(incident.WentBadly)}

            ## Action items
            {Bullet(incident.Actions.Select(action => $"{action.Item} (owner: {action.Owner}, due: {action.Due})"))}

            """;
        File.WriteAllText(path, content);
        return content;
    }
}
