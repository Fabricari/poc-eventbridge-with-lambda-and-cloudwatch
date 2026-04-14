# Event-Driven Text Moderation Demo with AWS EventBridge and .NET

## Overview

This proof of concept demonstrates how AWS EventBridge can be used as an event bus to decouple two small services in a simple serverless architecture.

The demo uses a lightweight text moderation scenario. A user submits a short message through an HTTP-accessible .NET Lambda function. That function performs minimal request-handling logic, then publishes a custom event to an EventBridge event bus. A second .NET Lambda function subscribes to that event, applies a small set of hardcoded moderation rules, and writes the result to CloudWatch Logs.

The moderation rules intentionally focus on mild, humorous, portfolio-safe terms such as "poop," "crap," "dang," "gosh," and similar soft swears. The goal is to make the demo memorable without relying on offensive or sensitive content.

## Purpose

The purpose of this demo is to show the architectural value of an event bus in a way that is more meaningful than a trivial hello-world example while still keeping the implementation small.

This proof of concept is intended to demonstrate that one service can publish an event without knowing which downstream service handles it, that EventBridge can route events between loosely coupled components, and that asynchronous processing changes the interaction model compared to direct request-response communication.

This is not intended to be a production moderation system. The scope is intentionally narrow so that the role of EventBridge remains the focus.

## Scenario

A user submits a message through a browser using a query string.

Example:

```http
GET /submit-message?text=oh+crap+this+demo+is+gosh+darn+interesting
```

The HTTP-facing Lambda accepts the request, validates and normalizes the input, and publishes a `MessageSubmitted` event to EventBridge.

A separate moderation Lambda receives that event asynchronously, scans the message for a hardcoded list of mild expletives, and logs whether the message is `CLEAN` or `FLAGGED`.

Examples:

- "this architecture is delightful" is treated as `CLEAN`.
- "oh crap this demo is gosh darn interesting" is treated as `FLAGGED`.

## Architecture

The architecture is intentionally small:

- Browser
- Lambda Function URL
- HTTP Lambda (.NET)
- EventBridge Event Bus
- Subscriber Lambda (.NET)
- CloudWatch Logs

From the HTTP Lambda:

- Intake and handoff are logged to CloudWatch Logs.
- A custom event is published to EventBridge.

EventBridge evaluates its rules and routes matching events to the subscriber Lambda. The subscriber then writes moderation results to its own CloudWatch Logs.

## Component Responsibilities

### HTTP Lambda

The HTTP Lambda is the request-facing service. Its responsibilities are to:

- Receive the HTTP request.
- Parse and validate the `text` query string parameter.
- Normalize input as needed.
- Publish a custom event to EventBridge.
- Return an immediate response indicating the request was accepted and handed off.

It does not perform moderation. Its role is intake and event publication.

### EventBridge Event Bus

EventBridge is the routing layer between publisher and subscriber. Its responsibilities are to:

- Receive the custom `MessageSubmitted` event.
- Evaluate routing rules.
- Deliver matching events to interested subscribers.

The publisher does not know which consumer receives the event, how many consumers exist, or what each consumer does with it.

### Subscriber Lambda

The subscriber Lambda is the moderation service. Its responsibilities are to:

- Receive the event from EventBridge.
- Inspect submitted text.
- Compare text against a hardcoded list of mild expletives.
- Determine whether the message is `CLEAN` or `FLAGGED`.
- Log the moderation result.

This service owns the moderation logic and can evolve independently from the HTTP-facing service.

### CloudWatch Logs

CloudWatch Logs provide observable proof that the system worked. Each Lambda writes to its own log group, which makes the asynchronous handoff easy to follow:

- HTTP Lambda logs request handling and handoff.
- Subscriber Lambda logs moderation results.

This keeps the demo stateless while still making the downstream effect visible.

## Why These Choices Were Made

### Why EventBridge

The main point of the demo is that the HTTP-facing service does not need to know anything about the moderation service beyond the event contract.

The HTTP Lambda publishes a business event. EventBridge is responsible for routing that event to the appropriate consumer. That decoupling is the key architectural value being demonstrated.

This also leaves room for future evolution. The same `MessageSubmitted` event could later be routed to other consumers, such as an audit logger, a metrics collector, or a different moderation engine, without changing the publisher.

### Why Lambda for Both Services

Lambda keeps the proof of concept focused on the event bus rather than operational overhead.

Using two small serverless functions makes it easy to demonstrate a clear service boundary, asynchronous handoff, and independent responsibilities without introducing unnecessary infrastructure.

The goal is not to simulate a full microservices platform. The goal is to isolate and highlight the value of EventBridge.

### Why a Lambda Function URL Instead of API Gateway

A Lambda Function URL provides a simple HTTPS entry point without adding extra API infrastructure.

For this demo, the interesting part is the event-driven handoff, not advanced HTTP routing, authorization, or API management. A Function URL keeps the front door small and easy to explain.

### Why Logs Instead of Persistent State

This proof of concept is intentionally stateless.

Because the workflow is asynchronous, the browser caller does not wait for the subscriber Lambda to complete. That means some external effect is needed to prove downstream processing occurred.

CloudWatch Logs are the simplest way to make that effect visible without introducing a database, S3 bucket, or another stateful component.

## Event Flow

1. A user submits a message from a browser using the Lambda Function URL.
2. The HTTP Lambda receives the request.
3. The HTTP Lambda validates and normalizes the text.
4. The HTTP Lambda publishes a `MessageSubmitted` event to EventBridge.
5. The HTTP Lambda logs the handoff and returns an immediate HTTP response.
6. EventBridge matches the event to a rule.
7. EventBridge invokes the subscriber Lambda asynchronously.
8. The subscriber Lambda evaluates the message against hardcoded moderation rules.
9. The subscriber Lambda logs the moderation outcome to CloudWatch Logs.

## Event Contract

The publisher emits a custom event with a shape similar to this:

```yaml
source: demo.message-submission
detail-type: MessageSubmitted
detail:
  messageText: "oh crap this demo is gosh darn interesting"
  submittedAt: "2026-04-14T18:30:00Z"
```

This event contract is the boundary between the two services.

- The publisher only needs to know how to publish the event correctly.
- The subscriber only needs to know how to interpret it correctly.

## Example HTTP Response

The HTTP Lambda can return a response like this:

```yaml
status: HANDED_OFF
message: Message accepted and submitted for moderation.
submittedText: oh crap this demo is gosh darn interesting
```

This response confirms that the request was received and handed off, but it does not wait for moderation to complete. That distinction reinforces that the architecture is event-driven, not direct request-response between the two services.

## Example Subscriber Output

The subscriber Lambda may write a structured log entry such as:

```yaml
status: FLAGGED
category: mild-expletives
matchedTerms: crap, gosh, darn
originalText: oh crap this demo is gosh darn interesting
```

A clean message might produce:

```yaml
status: CLEAN
category: mild-expletives
matchedTerms: none
originalText: this architecture is delightful
```

## What This Demo Proves

This proof of concept shows that:

- An HTTP-facing service can publish events instead of directly invoking downstream logic.
- EventBridge can route those events without the publisher knowing the consumer.
- Responsibilities can be separated cleanly across two services.
- Asynchronous processing can be observed without storing application state.

## Non-Goals

This demo does not attempt to solve:

- Real content moderation.
- Natural language processing.
- Durable result storage.
- Retries and dead-letter handling.
- Authentication and authorization.
- Production hardening.

These are important concerns in real systems, but they would distract from the narrow purpose of this proof of concept.

## Why This Use Case Works

The moderation scenario works because it gives the subscriber a believable, independent responsibility.

If the HTTP Lambda performed moderation directly, EventBridge would add little value. By separating message submission from moderation, the demo shows why an event bus can be useful:

- The publisher owns request intake.
- The subscriber owns moderation policy.
- The event bus owns routing between them.

That separation is simple enough for a demo and realistic enough to justify the architecture.

## Possible Future Extensions

This proof of concept could be extended later by adding new subscribers without changing the publisher. Examples include:

- A second Lambda that records moderation metrics.
- An SQS queue for audit processing.
- A notification workflow for flagged messages.
- A more advanced moderation engine.

These extensions would further reinforce the value of the event bus model.
