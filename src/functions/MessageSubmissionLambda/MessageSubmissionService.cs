namespace MessageSubmissionLambda;

public enum SubmissionStatus
{
    Accepted,
    InvalidRequest,
    PublishFailed
}

public class MessageSubmissionService
{
    private readonly ModerationHandoffPublisher _publisher = new();

    public async Task<SubmissionStatus> SubmitAsync(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return SubmissionStatus.InvalidRequest;
        }

        var normalizedText = text.Trim();

        var published = await _publisher.PublishAsync(normalizedText);

        return published ? SubmissionStatus.Accepted : SubmissionStatus.PublishFailed;
    }
}
