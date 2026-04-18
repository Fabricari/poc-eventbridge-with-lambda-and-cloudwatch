# Event-Driven Text Moderation Demo with AWS EventBridge and .NET

## Overview

This proof of concept demonstrates a simple event-driven architecture using two .NET Lambda functions decoupled by Amazon EventBridge.

The scenario is intentionally small and portfolio-friendly:

- An HTTP-facing Lambda receives a message from a browser through a Lambda Function URL.
- That Lambda publishes an event to EventBridge.
- A subscriber Lambda receives the event asynchronously and applies mild, family-friendly moderation rules.
- Both functions write human-readable demo logs to CloudWatch.

The goal is clarity of event flow, not production hardening.

## Purpose

This repository exists to demonstrate architectural decoupling:

- The submission service owns request intake and event publication.
- The moderation service owns moderation policy and evaluation.
- EventBridge owns routing between them.

This is not a production moderation platform. It is a focused demonstration of asynchronous handoff and service boundary separation.

## Architecture at a Glance

![Event-driven moderation architecture](architecture/poc-eventbridge-lambda-cloudwatch.jpg)

*Architecture diagram: Browser -> Function URL -> MessageSubmissionLambda -> EventBridge -> MessageModerationLambda -> CloudWatch Logs.*

- Browser client
- Lambda Function URL
- MessageSubmissionLambda (.NET Lambda publisher)
- Custom EventBridge event bus and rule
- MessageModerationLambda (.NET Lambda subscriber)
- CloudWatch Logs

## Current Component Responsibilities

### MessageSubmissionLambda

- Entry class: MessageSubmissionFunction
- Handler: FunctionHandler
- Handler path format: `<Assembly>::MessageSubmissionLambda.MessageSubmissionFunction::FunctionHandler`
- Responsibilities:
  - Parse query-string text input.
  - Decide publish outcome (invalid input, publish success, publish failure).
  - Shape immediate HTTP response.
  - Write two demo log lines.

Publisher details are isolated in EventBridgeMessagePublisher, which reads EventBridge configuration from environment variables and performs the PutEvents call.

### EventBridge

- Receives the message-submitted event published by MessageSubmissionLambda.
- Evaluates event rule matching.
- Invokes MessageModerationLambda asynchronously.

The publisher does not directly invoke the subscriber.

### MessageModerationLambda

- Entry class: MessageModerationFunction
- Handler: FunctionHandler
- Handler path format: `<Assembly>::MessageModerationLambda.MessageModerationFunction::FunctionHandler`
- Responsibilities:
  - Bind EventBridge envelope and deserialize detail text.
  - Log invocation metadata and moderation outcome.
  - Delegate text evaluation to MessageModerationService.

MessageModerationService contains the moderation rules and models:

- HashSet of mild flagged terms.
- Text splitting, case-insensitive matching, deduping, and sorting.
- ModerationResult with ModerationStatus (Clean or Flagged).

### CloudWatch Logs

CloudWatch is the observable proof of asynchronous execution.

- Submission logs show receipt and handoff result.
- Moderation logs show subscriber invocation and moderation output.

Both functions use consistent prefixed messages so demo lines stand out from platform logs.

## End-to-End Flow

1. A browser calls the Function URL with query parameter text.
2. MessageSubmissionFunction validates input and publishes an event when text is present.
3. MessageSubmissionFunction returns an immediate HTTP response.
4. EventBridge matches and routes the event.
5. MessageModerationFunction is invoked asynchronously.
6. MessageModerationService evaluates text and returns moderation result.
7. Both functions emit demo-prefixed CloudWatch logs.

## Event Contract Boundary

The publisher emits event detail as JSON with a single Text field.

Conceptually:

```yaml
source: <EVENT_SOURCE>
detail-type: <EVENT_DETAIL_TYPE>
detail:
  Text: "example message"
```

The submission and moderation functions remain decoupled at the service level and communicate through this event boundary.

## Related Design Documents

- docs/message-submission-lambda-design.md
- docs/message-moderation-lambda-design.md

## Intentional Non-Goals

This demo intentionally excludes:

- advanced NLP or AI moderation
- persistent result storage
- retry/dead-letter orchestration details
- authentication/authorization deep dive
- broad production hardening and operational controls

Those concerns are valuable in real systems, but they are outside the scope of this architecture-focused proof of concept.
