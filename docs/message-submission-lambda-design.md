# MessageSubmissionLambda Design

## Purpose

MessageSubmissionLambda is the HTTP-facing publisher function in this proof of concept.
It receives a message from a Lambda Function URL request and hands it off to Amazon EventBridge for asynchronous moderation.

This component is intentionally small. It prioritizes clarity over extensibility.

## Design Boundaries

- Function.cs knows HTTP request/response handling.
- MessageSubmissionService.cs knows publisher-side business flow.
- ModerationHandoffPublisher.cs knows EventBridge SDK details.
- SubmittedMessage.cs is the EventBridge detail payload contract.

No additional abstraction layers, optional extension points, or shared library contracts are introduced.

## File Responsibilities

| File | Responsibility |
|---|---|
| src/functions/MessageSubmissionLambda/Function.cs | Lambda entry point and HTTP adapter for Function URL requests |
| src/functions/MessageSubmissionLambda/MessageSubmissionService.cs | Validates and normalizes input text, creates SubmittedMessage, and returns submission status |
| src/functions/MessageSubmissionLambda/ModerationHandoffPublisher.cs | Reads EventBridge configuration from environment variables and publishes the event |
| src/functions/MessageSubmissionLambda/SubmittedMessage.cs | Defines the detail payload with a single Text property |

## HTTP Behavior (Function.cs)

The function treats the Lambda Function URL as a direct HTTP endpoint.

Expected input:

- Query string parameter: text

Behavior:

- Logs request received.
- Reads text from the query string.
- Calls MessageSubmissionService.
- Returns inline HTTP response based on service status.

Response mapping:

- Accepted -> 200 OK, message indicates moderation handoff succeeded.
- InvalidRequest -> 400 Bad Request, message indicates text is required.
- PublishFailed -> 500 Internal Server Error, message indicates handoff did not succeed.

## Business Flow (MessageSubmissionService.cs)

MessageSubmissionService owns the publisher-side flow:

1. Validate input (missing/blank is invalid).
2. Normalize input for the demo (trim whitespace).
3. Create SubmittedMessage.
4. Call ModerationHandoffPublisher.
5. Return minimal status.

Status model:

- Accepted
- InvalidRequest
- PublishFailed

## EventBridge Integration (ModerationHandoffPublisher.cs)

ModerationHandoffPublisher is the only class aware of EventBridge SDK request/response types.

Environment variables used:

- EVENT_BUS_NAME
- EVENT_SOURCE
- EVENT_DETAIL_TYPE

Publish behavior:

- Serialize SubmittedMessage as EventBridge detail JSON.
- Build PutEvents request with one entry.
- Log publish attempt.
- Inspect PutEvents response for failed entries.
- Log success or failure.
- Return boolean publish outcome to the service.

The publisher does not perform startup validation for bus existence. If configuration is wrong or the bus does not exist, publish failure is logged and returned as PublishFailed.

## Event Payload Contract (SubmittedMessage.cs)

SubmittedMessage is intentionally minimal for this proof of concept.

Properties:

- Text

No IDs, timestamps, correlation fields, user metadata, or other extra fields are included.

## Logging

The function emits explicit logs for the core demo path:

- request received
- invalid input
- publish attempted
- publish succeeded or failed

Logs are written through standard Lambda logging and appear in CloudWatch Logs.

## Intentional Non-Goals

This design intentionally avoids:

- API Gateway patterns and controllers
- ASP.NET MVC or routing frameworks
- dependency injection containers
- shared project contracts with the moderation Lambda
- third-party libraries
- persistence and production hardening features

The goal is to clearly demonstrate publisher-to-EventBridge handoff with minimal code.
