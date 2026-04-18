# Event-Driven Text Moderation Demo with AWS EventBridge and .NET

## Overview

This proof of concept demonstrates a simple event-driven architecture using two .NET Lambda functions decoupled by Amazon EventBridge.

The scenario is intentionally small and portfolio-friendly:

- An HTTP-facing Lambda receives a message from a browser through a Lambda Function URL.
- That Lambda publishes an event to EventBridge.
- A subscriber Lambda receives the event asynchronously and applies mild, family-friendly moderation rules.
- Both functions write human-readable demo logs to CloudWatch.

The goal is clarity of event flow, not production hardening.

## Architecture at a Glance

![Event-driven moderation architecture](architecture/poc-eventbridge-lambda-cloudwatch.jpg)

*Architecture diagram: Browser -> Function URL -> MessageSubmissionLambda -> EventBridge -> MessageModerationLambda -> CloudWatch Logs.*

At a high level:

- Browser input reaches Lambda through a Function URL.
- Submission Lambda publishes a custom event to EventBridge.
- EventBridge routes the event to the moderation Lambda.
- Both functions write observable logs to CloudWatch.

## Infrastructure Snapshot

These are the key infrastructure values used across the demo:

| Area | Value |
|---|---|
| Publisher Lambda | `MessageSubmissionLambda` |
| Publisher handler | `MessageSubmissionLambda::MessageSubmissionLambda.MessageSubmissionFunction::FunctionHandler` |
| Publisher Function URL auth | `NONE` |
| Subscriber Lambda | `MessageModerationLambda` |
| Subscriber handler | `MessageModerationLambda::MessageModerationLambda.MessageModerationFunction::FunctionHandler` |
| Event bus | `message-moderation-bus` |
| Event rule | `route-to-moderation-lambda` |
| Event pattern source | `message-submission-service` |
| Event pattern detail-type | `MessageSubmitted` |
| Publisher env var `EVENT_BUS_NAME` | `message-moderation-bus` |
| Publisher env var `EVENT_SOURCE` | `message-submission-service` |
| Publisher env var `EVENT_DETAIL_TYPE` | `MessageSubmitted` |

## Event Contract Boundary

The publisher emits one custom event detail field (`Text`) to EventBridge:

```yaml
source: message-submission-service
detail-type: MessageSubmitted
detail:
  Text: "example message"
```

This contract is the decoupling boundary between submission and moderation services.

## End-to-End Flow

1. A browser calls the publisher Function URL with a text query parameter.
2. The publisher Lambda validates input and publishes a custom EventBridge event.
3. The publisher returns an immediate HTTP response to the browser.
4. EventBridge matches and routes the event to the subscriber Lambda.
5. The subscriber Lambda evaluates the message and logs the moderation outcome.

## Related Design Documents

Use these companion documents for implementation detail and manual environment setup beyond this high-level overview.

- [MessageSubmissionLambda Design](message-submission-lambda-design.md) - Publisher Lambda responsibilities, request flow, event publication behavior, and response mapping.
- [MessageModerationLambda Design](message-moderation-lambda-design.md) - Subscriber Lambda event handling, moderation logic, and output semantics.
- [Manual AWS Provisioning](manual-aws-provisioning.md) - AWS console provisioning values for Lambda, EventBridge, and end-to-end verification URLs.

## Intentional Non-Goals

This demo intentionally excludes:

- advanced NLP or AI moderation
- persistent result storage
- retry/dead-letter orchestration details
- authentication/authorization deep dive
- broad production hardening and operational controls

Those concerns are valuable in real systems, but they are outside the scope of this architecture-focused proof of concept.
