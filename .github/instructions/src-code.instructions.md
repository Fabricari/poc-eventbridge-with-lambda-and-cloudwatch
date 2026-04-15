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
- Treat this as sacrificial architecture: build to demonstrate the EventBridge flow, then support teardown and archival for education.

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

## Implementation Style for This POC

- Avoid extraneous code and unnecessary abstraction.
- Avoid "showcase" patterns and resume-driven design.
- Do not add extension points or future-iteration scaffolding unless explicitly requested.
- Respect user-provided architectural boundaries, then choose the simplest implementation that works.
- Prioritize functionality and demonstrability over generalized design.

## Readability and Naming

- Write code that is self-documenting when read on screen.
- Use human-readable, business-domain names for classes, methods, and variables.
- Avoid magic values and cryptic or "leet" naming.
- Prefer straightforward control flow over clever shorthand.

## Engineering Tradeoffs

- Security, performance, and broad hardening are not primary goals for this POC unless AWS requires a minimum implementation detail.
- Prefer .NET 10 approaches and APIs when they remain intuitive to read.
- If a newer approach hurts readability for demo purposes, choose the clearer option.

## Dependency Policy

- AWS-provided SDKs and libraries are acceptable for typical Lambda/EventBridge implementation.
- Avoid third-party libraries unless explicitly requested.
