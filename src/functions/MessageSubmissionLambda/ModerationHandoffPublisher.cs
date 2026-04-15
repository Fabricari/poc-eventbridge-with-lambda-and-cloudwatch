using System.Text.Json;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.Lambda.Core;

namespace MessageSubmissionLambda;

public class ModerationHandoffPublisher
{
    private readonly string _eventBusName;
    private readonly string _eventSource;
    private readonly string _eventDetailType;
    private readonly AmazonEventBridgeClient _client = new();

    public ModerationHandoffPublisher()
    {
        _eventBusName = Environment.GetEnvironmentVariable("EVENT_BUS_NAME") ?? "";
        _eventSource = Environment.GetEnvironmentVariable("EVENT_SOURCE") ?? "";
        _eventDetailType = Environment.GetEnvironmentVariable("EVENT_DETAIL_TYPE") ?? "";
    }

    public async Task<bool> PublishAsync(SubmittedMessage message, ILambdaLogger logger)
    {
        var detail = JsonSerializer.Serialize(message);

        var request = new PutEventsRequest
        {
            Entries =
            [
                new PutEventsRequestEntry
                {
                    EventBusName = _eventBusName,
                    Source = _eventSource,
                    DetailType = _eventDetailType,
                    Detail = detail
                }
            ]
        };

        logger.LogInformation($"Publishing event to bus '{_eventBusName}' with source '{_eventSource}'");

        try
        {
            var response = await _client.PutEventsAsync(request);

            if (response.FailedEntryCount > 0)
            {
                var entry = response.Entries[0];
                logger.LogError($"Publish failed: {entry.ErrorCode} - {entry.ErrorMessage}");
                return false;
            }

            logger.LogInformation("Publish succeeded");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"Publish failed with exception: {ex.Message}");
            return false;
        }
    }
}
