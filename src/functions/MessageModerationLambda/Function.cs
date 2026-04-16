using System.Text.Json;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MessageModerationLambda;

public class Function
{
    private readonly ModerationService _service = new();

    public void FunctionHandler(CloudWatchEvent<ModerationEvent> eventBridgeEvent, ILambdaContext context)
    {
        context.Logger.LogInformation($"Event received: source={eventBridgeEvent.Source}, detail-type={eventBridgeEvent.DetailType}");

        var moderationEvent = eventBridgeEvent.Detail;

        var result = _service.Evaluate(moderationEvent.Text);

        context.Logger.LogInformation(JsonSerializer.Serialize(new
        {
            status = result.Status.ToString().ToUpperInvariant(),
            category = "mild-expletives",
            matchedTerms = result.MatchedTerms.Length > 0
                ? string.Join(", ", result.MatchedTerms)
                : "none",
            originalText = result.OriginalText
        }));
    }
}
