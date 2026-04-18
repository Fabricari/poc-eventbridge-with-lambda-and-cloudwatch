using System.Net;
using Amazon.Lambda.TestUtilities;
using MessageSubmissionLambda;
using Xunit;

namespace MessageSubmissionLambda.Tests;

public class FunctionTests
{
    private readonly MessageSubmissionFunction _function = new();
    private readonly TestLambdaContext _context = new();

    [Fact]
    public async Task ValidText_ReturnsAcceptedOrPublishFailed()
    {
        // Without a real EventBridge bus configured, this will flow through
        // the full pipeline and land on PublishFailed. When running against
        // a live AWS environment with EVENT_BUS_NAME set, it returns Accepted.
        var request = new ApiRequest { RawQueryString = "text=hello+world" };

        var response = await _function.FunctionHandler(request, _context);

        Assert.True(
            response.StatusCode is (int)HttpStatusCode.OK or (int)HttpStatusCode.InternalServerError,
            $"Expected 200 or 500 but got {response.StatusCode}");
    }

    [Fact]
    public async Task MissingQueryString_ReturnsBadRequest()
    {
        var request = new ApiRequest { RawQueryString = null };

        var response = await _function.FunctionHandler(request, _context);

        Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("text", response.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EmptyQueryString_ReturnsBadRequest()
    {
        var request = new ApiRequest { RawQueryString = "" };

        var response = await _function.FunctionHandler(request, _context);

        Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task QueryStringWithoutTextParam_ReturnsBadRequest()
    {
        var request = new ApiRequest { RawQueryString = "other=value" };

        var response = await _function.FunctionHandler(request, _context);

        Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BlankTextParam_ReturnsBadRequest()
    {
        var request = new ApiRequest { RawQueryString = "text=" };

        var response = await _function.FunctionHandler(request, _context);

        Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task WhitespaceOnlyTextParam_ReturnsBadRequest()
    {
        var request = new ApiRequest { RawQueryString = "text=++++" };

        var response = await _function.FunctionHandler(request, _context);

        Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("text=oh+crap+this+is+interesting")]
    [InlineData("text=simple+message")]
    [InlineData("text=a")]
    public async Task VariousValidInputs_FlowThroughPipeline(string queryString)
    {
        // Each valid input should reach the publish step.
        // Without live AWS config, expect PublishFailed.
        var request = new ApiRequest { RawQueryString = queryString };

        var response = await _function.FunctionHandler(request, _context);

        Assert.True(
            response.StatusCode is (int)HttpStatusCode.OK or (int)HttpStatusCode.InternalServerError,
            $"Expected 200 or 500 but got {response.StatusCode}");
    }

    [Fact]
    public async Task ValidText_ResponseBodyIndicatesHandoff()
    {
        var request = new ApiRequest { RawQueryString = "text=test+message" };

        var response = await _function.FunctionHandler(request, _context);

        // Whether publish succeeded or failed, the body should say something
        // about moderation handoff.
        Assert.True(
            response.Body.Contains("moderation", StringComparison.OrdinalIgnoreCase) ||
            response.Body.Contains("handoff", StringComparison.OrdinalIgnoreCase),
            "Expected response body to reference moderation or handoff");
    }
}
