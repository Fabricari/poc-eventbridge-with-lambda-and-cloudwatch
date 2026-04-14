# Event-Driven Text Moderation Demo

This repository contains a proof of concept that demonstrates an event-driven serverless architecture on AWS using a lightweight text moderation scenario.

The project is designed to show how an HTTP-facing service can publish business events without being tightly coupled to downstream processing logic. A separate moderation service consumes those events asynchronously, keeping service responsibilities clear and independent.

## What This Repository Is For

This project exists to illustrate the architectural value of using an event bus for decoupling and asynchronous handoff between services. It is intentionally scoped as a learning and portfolio demo rather than a production moderation platform.

## Technology Summary

- .NET Lambda functions for request intake and moderation processing.
- AWS Lambda Function URL as a simple HTTPS entry point.
- Amazon EventBridge as the event routing layer.
- Amazon CloudWatch Logs for observability of the asynchronous flow.

The implementation is intentionally minimal so the event-driven design remains the focus.
