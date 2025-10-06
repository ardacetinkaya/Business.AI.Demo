# Business AI Integration Demo

This project demonstrates how to integrate AI capabilities into a typical business application. It showcases a common scenario where business events flow through the system and get processed, stored, and then made available for AI-powered interactions.

## What This Project Shows

This is a hands-on learning project that combines:
- **Event-driven architecture** with Kafka for processing business data
- **AI integration** using Model Context Protocol (MCP) to interact with stored data
- **Modern development practices** with .NET Aspire for orchestration
- **.NET Platform** features for building AI applicatıon

The demo simulates a simple e-commerce flow where orders are generated, processed, and stored - then made accessible through an AI chat interface that can answer questions about the business data.

## Projects

### 🎛️ **Demo.Host** (.NET Aspire Orchestrator)
The main orchestrator that manages all services and provides a unified development experience with observability and monitoring.

**To run the complete demo:**
```bash
dotnet run --project Demo.Host
```
*Opens Aspire dashboard with unified logging, metrics, and service management*

### 🚀 **Kafka.Producer**
Generates mock e-commerce order events to simulate real business activity.

**Features:**
- Creates realistic order data every 5-10 seconds
- Publishes events to the `order-events` Kafka topic


### 📥 **Kafka.Consumer**
Processes incoming order events and stores them in the database for later use.

**Features:**
- Consumes messages from Kafka topics
- Stores order and payment data in PostgreSQL
- Handles business logic like fee calculations

### 🤖 **MCP.Server**
Provides AI tools that can access business data stored in the database.

**Features:**
- Exposes business data through MCP protocol
- Allows AI to retrieve recent payments
- Caches frequently accessed data for performance

### 💬 **MCP.Host**
A web interface where users can chat with AI about their business data.

**Features:**
- Interactive chat interface
- AI can answer questions about business related data such payments
- Connects to external AI models through GitHub Models

## Architecture

```
                            ┌─────────────────┐
                            │   🎛️Demo.Host   │
                            │  (.NET Aspire)  │
                            │ - Orchestration │
                            │ - Observability │
                            │ - Configuration │
                            └─────────────────┘
                                     │
                                     │ manages development environment
                                     ▼
 ─────────────────────────────────────────────────────────────────────────────────────────────────────────
              ┌─────────────────┐                    ┌───────────────────┐    PostgreSQL
              │     Producer    │                    │      Consumer     │  ┌─────────────┐
              │                 │                    │                   │  │   Orders    │
              │ - Order Events  │                    │ - Event Processing│─►│   Table     │
              │ - Mock Data     │                    │ - Business Logic  │  │             │
              │ - Publishing    │                    │ - Data Storage    │  ├─────────────┤
              └─────────────────┘                    └───────────────────┘  │  Payments   │
                     │                                         ▲            │   Table     │
                     │           ┌─────────────────┐           │            └─────────────┘
                     │           │     Kafka       │           │                 │
                     │           │                 │           │                 │
                     └─────────► │ - Topics        │ ──────────┘                 │
                                 │   - order-events│                             │
                                 │                 │                             │
                                 └─────────────────┘               provides data │
                                                                                 │ 
                                                                                 │
                                                                                 │
                                                                                 │
              ┌─────────────────┐                                                │
              │    MCP.Server   │                                                │
              │                 │◄───────────────────────────────────────────────┘
              │ - Business APIs │
              │ - Data Caching  │               
              │ - MCP Tools     │             ┌─────────────────┐
              └─────────────────┘────────────►│   Cache(Valkey) │
                       │                      │                 │
                       │                      │                 │
                       │                      │                 │
                       │                      │                 │
                       │                      └─────────────────┘   
                       │
                       │
                       │ MCP protocol
                       ▼
              ┌─────────────────┐        External AI      
              │   MCP.Host(web) │   ┌─────────────────┐
              │                 │───│  GitHub Models  │
              │ - Web Interface │   │                 │
              │ - AI Integration│   │ - LLMs          │
              │ - Chat Features │   │                 │
              └─────────────────┘   └─────────────────┘
```

## How It Works

1. **Data Generation**: The Producer generates realistic order events
2. **Event Processing**: The Consumer processes events and stores business data  
3. **AI Access**: MCP Server exposes business data through standardized APIs
4. **User Interaction**: Users chat with AI through the web interface to get insights about their business data

This demonstrates a practical approach to building AI-powered business applications using modern development tools and patterns.
