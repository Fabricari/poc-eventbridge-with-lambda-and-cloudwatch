# Event-Driven Text Moderation Demo

This repository contains a proof of concept that demonstrates an event-driven serverless architecture on AWS using a lightweight text moderation scenario.

The demo shows a decoupled flow:

- An HTTP-facing .NET Lambda receives message input through a Lambda Function URL.
- The submission Lambda publishes a custom event to EventBridge.
- A separate .NET moderation Lambda subscribes asynchronously and evaluates the text.
- Both functions write demo-focused logs to CloudWatch.

## Scope

This project is intentionally narrow and portfolio-oriented.

- Focus: architectural decoupling and asynchronous handoff with EventBridge.
- Goal: clear service boundaries and observable event flow.
- Non-goal: production-grade moderation platform.

## Technology Summary

- .NET Lambda functions for submission and moderation.
- AWS Lambda Function URL as the HTTPS ingress.
- Amazon EventBridge as the routing layer.
- Amazon CloudWatch Logs for asynchronous observability.

## Documentation

- Project overview: [docs/project-overview.md](docs/project-overview.md)
- Submission lambda design: [docs/message-submission-lambda-design.md](docs/message-submission-lambda-design.md)
- Moderation lambda design: [docs/message-moderation-lambda-design.md](docs/message-moderation-lambda-design.md)
- Architecture diagram: [docs/architecture/poc-eventbridge-lambda-cloudwatch.jpg](docs/architecture/poc-eventbridge-lambda-cloudwatch.jpg)
