---
applyTo: "src/**"
description: "Use when creating or modifying source code under src for the EventBridge decoupling demo."
---

# Src Code Instructions

Use these conventions for code generated under src.

## Architecture Intent

- Keep the solution focused on demonstrating event-driven decoupling with Amazon EventBridge.
- Treat publisher and subscriber as separate services with independent responsibilities.
- Keep the proof of concept intentionally small and portfolio-friendly.

## Source Layout

- Keep source under src.
- Keep the solution file as .NET 10 convention: Poc.EventBridgeModeration.slnx.
- Keep Lambda projects under src/functions:
  - MessageSubmissionLambda
  - MessageModerationLambda
- Do not introduce shared project dependencies between these two functions unless explicitly requested.

## Service Boundary Decisions

- MessageSubmissionLambda is the HTTP-facing publisher.
- Use Lambda Function URL for ingress.
- Do not introduce API Gateway unless explicitly requested.
- MessageModerationLambda is the asynchronous subscriber invoked by EventBridge.

## Event and Moderation Framing

- Keep moderation examples mild and portfolio-safe.
- Focus on demonstrating event publication, routing, and asynchronous processing.
- Do not position this repository as production-ready content moderation.
