using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.Core;

// Selects the JSON serializer Lambda uses to bind EventBridge event JSON to handler parameters.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MessageModerationLambda;

public class Function
{
    private readonly ModerationService _service = new();

    // Configure the Lambda handler as <Assembly>::MessageModerationLambda.Function::FunctionHandler; signature must match the EventBridge envelope shape.
    public void FunctionHandler(CloudWatchEvent<ModerationEvent> eventBridgeEvent, ILambdaContext context)
    {
        var moderationEvent = eventBridgeEvent.Detail;

        context.Logger.LogInformation(
            $"DEMO | MESSAGE MODERATION | EventBridge invocation received: source={eventBridgeEvent.Source}; detail-type={eventBridgeEvent.DetailType}; event-id={eventBridgeEvent.Id}; text=\"{moderationEvent.Text}\"");

        var result = _service.Evaluate(moderationEvent.Text);

        context.Logger.LogInformation(
            $"DEMO | MESSAGE MODERATION | Moderation result: status={result.Status}; matched-terms={(result.MatchedTerms.Length > 0 ? string.Join(", ", result.MatchedTerms) : "none")}; original-text=\"{result.OriginalText}\"");
    }
}
