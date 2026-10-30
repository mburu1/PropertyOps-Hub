# PropertyOps Hub - Architecture Specification

## Overview

**PropertyOps Hub** is an enterprise-grade multi-unit hospitality operations platform designed to bridge the operational divide between Point of Sale (POS), Property Management Systems (PMS), Material Control / Inventory, and back-office Enterprise Resource Planning (ERP) accounting engines.

The application follows **Clean Architecture** principles and a **Light CQRS** pattern, decoupled via `MediatR` and validated by `FluentValidation`.

---

## Architectural Layers

```mermaid
graph TD
    UI["Web Presentation (Angular 19+)"] --> API["API Layer (ASP.NET Core Web API)"]
    API --> Application["Application Layer (Commands, Queries, Behaviors, DTOs)"]
    API -.-> Infrastructure["Infrastructure Layer (EF Core, SQL Server, Dapper, Integrations)"]
    Infrastructure --> Application
    Application --> Domain["Domain Layer (Entities, Value Objects, Domain Events, Enums)"]
    Infrastructure -.-> Domain
    
    subgraph Core ["Domain-Driven Core"]
        Domain
        Application
    end
    
    subgraph External ["External Boundaries"]
        Infrastructure
        API
        UI
    end
```

### 1. Domain Layer (`PropertyOpsHub.Domain`)
The core domain logic, enterprise rules, and model definitions. Free from external libraries or frameworks (except lightweight primitives).
- **Common**: `Entity`, `AggregateRoot`, `ValueObject`, `IDomainEvent`
- **Organization**: `Group`, `Property`, `Outlet`
- **MaterialControl**: `Item`, `UnitOfMeasure`, `UomConversion`, `Recipe`, `PurchaseOrder`, `GoodsReceipt`, `StockCount`, `StockMovement`, `WastageRecord`
- **Sales**: `SalesCheck`, `SalesLine`, `Tender`, `RevenueCenter`
- **Finance**: `GlAccount`, `CostCenter`, `MappingRule`, `JournalBatch`, `JournalLine`
- **ItOps**: `Interface`, `InterfaceHealthLog`, `Incident`, `AccessReview`

### 2. Application Layer (`PropertyOpsHub.Application`)
Orchestrates business use cases using CQRS.
- Command Handlers, Query Handlers, and Pipeline Behaviors (Logging, Validation, Transaction).
- Port interfaces (`IAppDbContext`, `ICurrentUser`, `IPosAdapter`, `IPmsClient`, `IErpJournalExporter`).

### 3. Infrastructure Layer (`PropertyOpsHub.Infrastructure`)
Adapters for persistence, external interfaces, and platform capabilities.
- **Persistence**: EF Core with SQL Server 2022, Dapper read models, configurations, interceptors (Audit, Soft Delete, Outbox).
- **Identity & Auth**: ASP.NET Core Identity, JWT authentication with refresh tokens, claims-based authorization.
- **Integrations**: `SimulatedPosAdapter`, `CsvPosAdapter`, `SimulatedPmsClient`, `CsvJournalExporter`, `NetSuiteJsonExporter`.
- **Background Jobs**: Hangfire scheduler with persistent SQL storage.

### 4. API Layer (`PropertyOpsHub.Api`)
Host and composition root for HTTP and WebSocket communication.
- Controllers, SignalR Hubs (`InterfaceHealthHub`).
- Global Exception Handler (RFC 7807 ProblemDetails), Multi-Tenant Resolution Middleware, OpenTelemetry telemetry.

### 5. Web Client (`PropertyOpsHub.Web`)
Angular SPA with standalone components, Signals, Material Design 3, and reactive dashboard visualisations.

---

## End-to-End Operational Lifecycle

The flow below represents a complete hotel business cycle: stock receipt, POS sales ingestion, guest room billing bridge, daily cost-of-sales calculation, and ERP journal export.

```mermaid
sequenceDiagram
    autonumber
    actor StoreKeeper as Store Controller
    actor Guest as Restaurant Guest
    actor Cashier as Outlet Cashier / POS
    participant POS as POS Adapter
    participant PMS as PMS Bridge
    participant Hub as PropertyOps Hub Core
    participant Cost as Cost Control Engine
    participant Fin as Finance Module
    participant ERP as Group ERP (NetSuite/Oracle)

    StoreKeeper->>Hub: Receive Goods (PO -> Goods Receipt GRN)
    Hub->>Hub: Update Weighted Average Cost (WAC) & Append StockMovement

    Guest->>Cashier: Order Meal & Charge to Room
    Cashier->>POS: Close Check (Tender: Room Charge)
    POS->>Hub: Ingest POS Sales Checks
    Hub->>PMS: Post Folio Charge (simulated room posting)
    PMS-->>Hub: Post Acknowledged & Reconciled

    Note over Hub,Cost: End of Day / Night Audit Cycle
    Hub->>Cost: Compute Theoretical vs Actual Usage
    Cost->>Cost: Calc Recipe BOM Deductions vs Stock Count / Wastage
    Cost-->>Hub: Variance Top-N and Waste Metrics

    Hub->>Fin: Generate Daily Journal Batches (deterministic key)
    Fin->>Fin: Validate Double-Entry Balance (Debits == Credits)
    Fin->>ERP: Export Balanced Journal (CSV / NetSuite JSON)
    ERP-->>Fin: 200 OK Accepted
```

---

## Multi-Tenancy Architecture

PropertyOps Hub enforces multi-unit hierarchy: **Group → Property → Outlet**.

```mermaid
graph TD
    Group["Group Entity (Parent Company / Brand)"]
    Property1["Property 1 (e.g. Resort & Spa)"]
    Property2["Property 2 (e.g. City Hotel)"]
    Outlet1["Outlet: All-Day Dining"]
    Outlet2["Outlet: Pool Bar"]
    Outlet3["Outlet: Rooftop Lounge"]

    Group --> Property1
    Group --> Property2
    Property1 --> Outlet1
    Property1 --> Outlet2
    Property2 --> Outlet3

    subgraph DataIsolation ["Data Isolation Principle"]
        TenantFilter["EF Core Global Query Filter: where PropertyId == CurrentPropertyId"]
        TokenCheck["JWT Claim: PropertyClaims & AllowedOutlets"]
        TenantFilter -.-> Property1
        TokenCheck -.-> TenantFilter
    end
```

Every query and write operation is scoped to the resolved tenant:
- Read queries automatically append `WHERE PropertyId = @tenantId`.
- Write commands enforce tenant validation via `TenantResolutionMiddleware` and domain handlers.
- Group-level roles (e.g., `Group Finance`) can bypass or parameterize cross-property aggregations.

---

## Transactional Outbox Pattern

To prevent distributed transaction failures during integration syncing (PMS folio charges, ERP export, IT interface health alerts), changes are committed in a single local transaction using an outbox table.

```mermaid
flowchart LR
    Command[Application Command] -->|1. Write changes + OutboxMessage| DB[(SQL Server Transaction)]
    DB -->|2. Local Commit| DB
    Hangfire[Hangfire Outbox Processor] -->|3. Poll Unprocessed Messages| DB
    Hangfire -->|4. Dispatch with Retries| External[External Interface / SignalR]
    External -->|5. Success Ack| Hangfire
    Hangfire -->|6. Mark ProcessedUtc| DB
```

---

## Integration Interfaces & IT Ops Monitoring

The platform continuously monitors interfaces with upstream and downstream systems.

```mermaid
stateDiagram-v2
    [*] --> Green: Normal Heartbeat / Sync within threshold
    Green --> Amber: Missed 1 heartbeat or warning latency
    Amber --> Green: Sync Successful
    Amber --> Red: Heartbeat timed out / Auth Failure
    Red --> Amber: Retry In Progress
    Red --> IncidentLogged: Auto-Create IT Ops Incident Ticket
    IncidentLogged --> SignalRNotification: Broadcast to IT Board (SignalR)
```
