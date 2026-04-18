# Event-Driven Text Moderation Demo

This repository contains a proof of concept for event-driven decoupling on AWS using a lightweight text moderation scenario.

## Flow Summary

- An HTTP-facing .NET Lambda receives message input through a Lambda Function URL.
- The submission Lambda publishes a custom event to EventBridge.
- A separate .NET moderation Lambda subscribes asynchronously and evaluates the text.
- Both functions write demo logs to CloudWatch.

## Scope

This project is intentionally narrow and portfolio-oriented.

- Focus: architectural decoupling and asynchronous handoff with EventBridge.
- Goal: clear service boundaries and observable event flow.
- Non-goal: production-grade moderation platform.

## Start Here

[Docs Landing Page](docs/README.md) contains the architecture overview, infrastructure snapshot, and event contract boundary.

## Documentation

- [Docs Landing Page](docs/README.md) - architecture overview, key infrastructure values, and event contract.
- [Manual AWS Provisioning](docs/manual-aws-provisioning.md) - console-based setup for Lambda, EventBridge, and verification URLs.
- [MessageSubmissionLambda Design](docs/message-submission-lambda-design.md) - publisher behavior, request/response mapping, and EventBridge publish details.
- [MessageModerationLambda Design](docs/message-moderation-lambda-design.md) - subscriber event handling and moderation logic.
- [Architecture Diagram](docs/architecture/poc-eventbridge-lambda-cloudwatch.jpg) - visual Browser -> Function URL -> EventBridge -> Subscriber flow.
