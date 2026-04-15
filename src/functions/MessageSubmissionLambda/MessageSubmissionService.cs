using Amazon.Lambda.Core;

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

    public async Task<SubmissionStatus> SubmitAsync(string? text, ILambdaLogger logger)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogWarning("Invalid input: text is missing or blank");
            return SubmissionStatus.InvalidRequest;
        }

        var normalizedText = text.Trim();

        var message = new SubmittedMessage { Text = normalizedText };

        var published = await _publisher.PublishAsync(message, logger);

        return published ? SubmissionStatus.Accepted : SubmissionStatus.PublishFailed;
    }
}
