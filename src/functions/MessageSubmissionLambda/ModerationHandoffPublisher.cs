using System.Text.Json;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;

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

    public async Task<bool> PublishAsync(SubmittedMessage message)
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

        try
        {
            var response = await _client.PutEventsAsync(request);

            if (response.FailedEntryCount > 0)
            {
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
