# Kafka Demo Solution

A simple Kafka producer-consumer demonstration project for hands-on learning. The producer generates mock e-commerce order events and publishes them to Kafka topics. The consumer processes these events and stores order data in PostgreSQL. All projects are orchestrated using .NET Aspire for unified observability and management.

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
┌─────────────────┐    Kafka Topic     ┌─────────────────┐    PostgreSQL
│   Producer      │    order-events    │   Consumer      │  ┌─────────────┐
│                 │ ─────────────────► │                 │  │   Orders    │
│ - Swedish Data  │                    │ - JSON Parsing  │─►│   Table     │
│ - Order Events  │                    │ - Order Mapping │  │             │
│ - Publishing    │                    │ - Repository    │  ├─────────────┤
└─────────────────┘                    └─────────────────┘  │  Payments   │
                                                            │   Table     │
                                                            └─────────────┘
```
