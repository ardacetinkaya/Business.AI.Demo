# Kafka Demo Solution

A simple Kafka producer-consumer demonstration. There are individual projects which are unified orchestration via .NET Aspire.

## Projects

### 🎛️ **Demo.Host** (.NET Aspire Orchestrator)
A .NET Aspire AppHost that orchestrates both Producer and Consumer with unified observability. Also have container run for the Kafka cluster

**To run the complete demo:**
```bash
dotnet run --project Demo.Host
```
*Opens Aspire dashboard with unified logging, metrics, and service management*

### 🚀 **Kafka.Producer**
A dedicated producer application that generates mock e-commerce order events.

**Features:**
- Generates order submitted events every 5-10 seconds
- Publishes to `order-events` topic

**To run individually:**
```bash
dotnet run --project Kafka.Producer
```

### 📥 **Kafka.Consumer**
A consumer application that processes messages from Kafka topics.

**Features:**
- Subscribes to multiple topics including `order-events`
- Processes and logs message details


**To run individually:**
```bash
dotnet run --project Kafka.Consumer
```

## Architecture

```
┌─────────────────┐    Kafka Topic     ┌─────────────────┐
│   Producer      │    order-events    │   Consumer      │
│                 │ ─────────────────► │                 │
│ - Event Gen     │                    │ - Message Proc  │
│ - Order Events  │                    │ - Logging       │
│ - Publishing    │                    │ - Processing    │
└─────────────────┘                    └─────────────────┘
```
