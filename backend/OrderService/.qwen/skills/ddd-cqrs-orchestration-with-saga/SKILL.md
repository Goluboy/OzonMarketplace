---
name: ddd-cqrs-orchestration-with-saga
description: Implementing command and query handlers in a DDD+CQRS architecture orchestrating domain logic, UoW persistence, and MassTransit saga events.
source: auto-skill
extracted_at: '2026-06-09T19:49:44.845Z'
---

## Approach: DDD/CQRS Orchestration for Microservices

This approach ensures that business logic remains encapsulated in the Domain layer while the Application/UseCases layer focuses solely on orchestration.

### 1. Structure and Layering
- **Commands**: Defined as records containing necessary data. Nested records should be used for item collections.
- **Queries**: Defined as records (e.g., `GetOrderByIdQuery`).
- **Response Models**: Use internal `Models` (e.g., `OrderModel`) in the UseCase layer to return data.
- **DTOs**: Presentation-layer DTOs must reside exclusively in the HTTP layer. Mapping from Internal Models $\rightarrow$ DTOs happens in the Controller.

### 2. Handler Implementation (The generic pattern)
Avoid "interface bloat" by using generic handler interfaces instead of individual ones for every use case:
- **Command Handlers**: Implement `ICommandHandler<TCommand, TResponse>`.
- **Query Handlers**: Implement `IQueryHandler<TQuery, TResponse>`.

### 3. Command Orchestration Logic
The handler should follow a strict sequence to maintain consistency:
1. **Transaction Start**: Begin a transaction via `IUnitOfWork`.
2. **Domain Object Creation**: Use domain factory methods (e.g., `Entity.Create(...)`) to instantiate aggregates, ensuring invariants are validated.
3. **Persistence**: Save the aggregate using the repository (participating in the UoW transaction).
4. **Atomic Commit**: Commit the transaction. Integration events must be published *after* a successful commit to avoid "ghost" events.
5. **Integration Event Publication**:
   - Map Domain aggregate to an `IntegrationEvent` (DTO).
   - Assign the `CorrelationId` (usually Aggregate ID) for Saga Choreography.
   - Publish via `IPublishEndpoint` (MassTransit).

### 4. Query Logic
- Queries should use the repository to retrieve aggregates.
- Map the returned Domain aggregate to an internal `Model` (e.g., `OrderModel`) to isolate the HTTP layer from domain internal changes.

### 5. Idempotency and Correlation
- **Transport Idempotency**: Rely on MassTransit's built-in message headers (MessageId).
- **Saga Coordination**: Explicitly assign the `CorrelationId` in the event body to link to the Saga state.
- **Event Audit**: Generate a unique `EventId` and persist it within the transaction if business-level auditing is required.

### 6. Dependency Injection (DI)
Use assembly scanning (via reflection or tools like Scrutor) to automatically register all handlers:
- Scan for types implementing `ICommandHandler<,>` or `IQueryHandler<,>`.
- Register as `Scoped` lifetime.
- Provide extension methods like `.AddCommands()` and `.AddQueries()` in the UseCase projects for clean registration in the HTTP layer.

### 7. Error Handling
- Wrap logic in a `try-catch` block and call `RollbackAsync()` on the `IUnitOfWork` upon failure to ensure data integrity.
