# MessageModerationLambda Design

## Purpose

MessageModerationLambda is the asynchronous subscriber in this proof of concept.
It receives message-submitted events from Amazon EventBridge and evaluates message text against a mild, family-friendly flagged-term list.

This implementation is intentionally compact for demo readability.

## Current Design Boundaries

- MessageModerationFunction handles EventBridge envelope binding, demo logging, and invocation of moderation logic.
- MessageModerationService handles text evaluation and moderation decision logic.
- No persistence, database write, or downstream callback is performed in this Lambda.

This keeps the handler focused on integration concerns while keeping moderation rules explicit and easy to explain.

## File Responsibilities

| File | Responsibility |
|---|---|
| src/functions/MessageModerationLambda/MessageModerationFunction.cs | Lambda entrypoint, EventBridge contract handling, and demo logs |
| src/functions/MessageModerationLambda/MessageModerationService.cs | Flagged-term rule set, text evaluation, and moderation result models |

## Lambda Handler Contract

- Handler method: FunctionHandler
- Handler class: MessageModerationFunction
- Handler path format: `<Assembly>::MessageModerationLambda.MessageModerationFunction::FunctionHandler`
- Trigger shape: EventBridge envelope bound as CloudWatchEvent<ModerationEvent>

## Event Payload Model

MessageModerationFunction contains a minimal detail model for EventBridge deserialization:

- ModerationEvent: Text

The moderation function only depends on the Text field from event detail.

## End-to-End Flow

1. Receive EventBridge invocation with event metadata and detail text.
2. Write one invocation log line with trigger metadata and text.
3. Call MessageModerationService.Evaluate on the incoming text.
4. Write one result log line with status, matched terms, and original text.

## Moderation Logic

MessageModerationService performs straightforward deterministic rules:

- Uses a case-insensitive HashSet of mild flagged terms.
- Splits incoming text into words using basic whitespace and punctuation delimiters.
- Matches terms case-insensitively.
- Normalizes matched terms to lowercase.
- De-duplicates and sorts matches alphabetically for stable output.
- Returns:
  - ModerationStatus.Clean when no terms match
  - ModerationStatus.Flagged when at least one term matches

The service returns a ModerationResult model containing:

- Status
- MatchedTerms
- OriginalText

## Logging for Demo Readability

The moderation handler emits two prefixed, human-readable log lines:

- `DEMO | MESSAGE MODERATION | Invocation received ...`
- `DEMO | MESSAGE MODERATION | Processing result: status=...`

These are designed to stand out from Lambda platform logs in CloudWatch.

## Intentional Non-Goals

This moderation Lambda intentionally avoids:

- external persistence of moderation results
- advanced NLP, ML inference, or contextual moderation
- dynamic rule management or runtime configuration stores
- third-party moderation libraries
- production hardening patterns beyond basic demo behavior

The goal is a clear and short demonstration of EventBridge subscriber processing and observable moderation outcomes.
