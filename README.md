# Distributed Task Processing Framework

Production-grade distributed background processing framework for .NET 8 workloads.

## Goals

- Reliability-first, horizontally scalable background processing.
- At-least-once delivery with idempotent execution support.
- Multi-tenant-aware metadata and correlation propagation.
- Clean Architecture boundaries across Core, Application, Infrastructure, and Worker host.

## Architecture

```text
src/
  DistributedTaskFramework.Core                  # Domain contracts and models
  DistributedTaskFramework.Application           # Dispatcher orchestration + retry policy
  DistributedTaskFramework.Infrastructure.Redis  # Idempotency + distributed locking
  DistributedTaskFramework.Infrastructure.RabbitMq # Scheduling, DLQ, serialization
  DistributedTaskFramework.Worker                # Host process and message consumption
tests/
  DistributedTaskFramework.Tests                 # Reliability-focused unit tests
```

## Reliability Capabilities

- **At-least-once processing** via RabbitMQ consumer ack/nack flow.
- **Idempotency gate** via `IIdempotencyStore` (Redis implementation included).
- **Exponential backoff retry** with jitter and max retry poison detection.
- **Dead-letter handling** for terminal processing failures.
- **Delayed/scheduled jobs** using RabbitMQ TTL + DLX scheduling flow.
- **Distributed lock abstraction** for cross-node mutual exclusion.
- **Correlation + tracing hooks** via OpenTelemetry activity tags.

## Local infrastructure (Docker)

This repository includes a `docker-compose.yml` for local dependencies:

- RabbitMQ (management UI)
- Redis
- SQL Server (optional persistence bootstrap)

```bash
docker compose up -d
```

RabbitMQ UI: <http://localhost:15672> (`guest` / `guest`)

## Build / test

```bash
dotnet build DistributedTaskFramework.sln
dotnet test DistributedTaskFramework.sln
```

> Note: the execution environment used by this agent did not have the `dotnet` SDK installed, so these commands could not be run here.
