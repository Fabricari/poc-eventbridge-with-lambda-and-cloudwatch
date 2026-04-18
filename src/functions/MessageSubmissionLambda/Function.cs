using System.Net;
using System.Web;
using Amazon.Lambda.Core;

// Selects the JSON serializer Lambda uses to bind input events to handler parameters and serialize return values.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MessageSubmissionLambda;

public class Function
{
    private readonly MessageSubmissionService _service = new();

    // Configure the Lambda handler as <Assembly>::MessageSubmissionLambda.Function::FunctionHandler; name is flexible, but signature must match the trigger JSON contract.
    public async Task<ApiResponse> FunctionHandler(ApiRequest request, ILambdaContext context)
    {
        context.Logger.LogInformation("Request received");

        var queryString = request.RawQueryString ?? "";
        var queryParams = HttpUtility.ParseQueryString(queryString);
        var text = queryParams["text"];

        var status = await _service.SubmitAsync(text, context.Logger);

        return status switch
        {
            SubmissionStatus.Accepted => new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "Message handed off for moderation."
            },
            SubmissionStatus.InvalidRequest => new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Body = "The 'text' query parameter is required and must not be blank."
            },
            SubmissionStatus.PublishFailed => new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = "Message handoff did not succeed. Please try again later."
            },
            _ => new ApiResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = "Unexpected error."
            }
        };
    }
}

// Minimal JSON-mapped input model so the serializer can bind the Function URL event field we use.
public class ApiRequest
{
    public string? RawQueryString { get; set; }
}

// Minimal JSON-mapped output model so the serializer can produce HTTP status/body in the Lambda response payload.
public class ApiResponse
{
    public int StatusCode { get; set; }
    public string Body { get; set; } = "";
}
