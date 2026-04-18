# MessageSubmissionLambda Design

## Purpose

MessageSubmissionLambda is the HTTP-facing publisher in this proof of concept.
It accepts a message from a Lambda Function URL request and publishes a message-submitted event to Amazon EventBridge.

This implementation is intentionally compact for demo readability.

## Current Design Boundaries

- MessageSubmissionFunction handles HTTP request parsing, minimal business decision flow, logging, and HTTP response shaping.
- EventBridgeMessagePublisher handles EventBridge SDK request creation and publish execution.
- No separate service class exists in the submission function because the business logic is intentionally small.

This keeps the entrypoint easy to narrate while still isolating EventBridge SDK details from the handler.

## File Responsibilities

| File | Responsibility |
|---|---|
| src/functions/MessageSubmissionLambda/MessageSubmissionFunction.cs | Lambda entrypoint, query parsing, business outcome decision, response mapping, and demo logs |
| src/functions/MessageSubmissionLambda/EventBridgeMessagePublisher.cs | Reads EventBridge environment settings and publishes one event entry |

## Lambda Handler Contract

- Handler method: FunctionHandler
- Handler class: MessageSubmissionFunction
- Handler path format: `<Assembly>::MessageSubmissionLambda.MessageSubmissionFunction::FunctionHandler`
- Trigger shape: Lambda Function URL HTTP event bound to ApiRequest

## Request and Response Models

MessageSubmissionFunction contains minimal transport models:

- ApiRequest: RawQueryString
- ApiResponse: StatusCode and Body

These models keep the contract explicit without introducing extra files for this demo.

## End-to-End Flow

1. Receive Function URL invocation and parse `text` from query string.
2. Write an invocation log line with trigger and input values.
3. Compute publish outcome: `null` for invalid input, otherwise publish trimmed text to EventBridge and capture success or failure.
4. Shape HTTP response via switch expression from publish outcome.
5. Write one result log line using the response message.

## Response Mapping

- `true` publish outcome -> `200 OK` and "Message handed off for moderation."
- `false` publish outcome -> `500 Internal Server Error` and "Message handoff did not succeed. Please try again later."
- `null` publish outcome (invalid text) -> `400 Bad Request` and "The 'text' query parameter is required and must not be blank."

## EventBridge Integration

EventBridgeMessagePublisher is the only class in this Lambda that knows EventBridge SDK types.

Environment variables:

- EVENT_BUS_NAME
- EVENT_SOURCE
- EVENT_DETAIL_TYPE

Publish behavior:

- Serialize detail JSON as `{ "Text": "..." }`.
- Create `PutEventsRequest` with one entry.
- Return `true` when no failed entries are reported.
- Return `false` on failed entries or thrown exceptions.

## Logging for Demo Readability

The submission handler emits two prefixed, human-readable log lines:

- `DEMO | MESSAGE SUBMISSION | Invocation received ...`
- `DEMO | MESSAGE SUBMISSION | Processing result: message="..."`

These are designed to stand out from Lambda platform logs in CloudWatch.

## Intentional Non-Goals

This submission Lambda intentionally avoids:

- API Gateway abstractions or web frameworks
- dependency injection containers
- extra layering for tiny business logic
- persistence, retries, idempotency hardening, and production-operational patterns
- third-party libraries outside AWS SDK/Lambda packages

The goal is a clear and short demonstration of Function URL ingress followed by EventBridge handoff.
