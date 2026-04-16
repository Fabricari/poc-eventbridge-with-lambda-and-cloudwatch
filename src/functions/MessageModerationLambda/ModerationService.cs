namespace MessageModerationLambda;

public class ModerationService
{
    private static readonly HashSet<string> FlaggedTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "poop",
        "crap",
        "dang",
        "gosh",
        "darn",
        "heck",
        "frick",
        "butt",
        "crud"
    };

    public ModerationResult Evaluate(string text)
    {
        var words = text.Split([' ', '\t', '\n', '\r', ',', '.', '!', '?', ';', ':'],
            StringSplitOptions.RemoveEmptyEntries);

        var matched = words
            .Where(word => FlaggedTerms.Contains(word))
            .Select(word => word.ToLowerInvariant())
            .Distinct()
            .Order()
            .ToArray();

        return new ModerationResult
        {
            Status = matched.Length > 0 ? ModerationStatus.Flagged : ModerationStatus.Clean,
            MatchedTerms = matched,
            OriginalText = text
        };
    }
}
