# Innovation.vNext

A simple framework to implement **CQRS** in .NET applications with **immediate consistency**.  
Innovation does not implement, and does not attempt to support, Event Sourcing.

Innovation is **CQRS-first** and uses a **mediator-style dispatch pipeline** internally to route commands/queries to handlers and apply cross-cutting behaviors.

---

## Why Innovation

- **CQRS-first design** for explicit write/read separation.
- **Free forever** (no intent to charge).
- **Simple request/handler model** with low ceremony.
- **Clean architecture alignment** for maintainable boundaries.
- **Extensible pipeline** for validation, logging, transactions, and other cross-cutting concerns.

---

## vNext Version Objectives

1. Improve Command Pipeline Performance
1. Implement CommandResult Errors As Per RFC 7807
1. Use ValueTask Over Task To Better Support Synchronous Operations
1. Improve Query Pipeline Performance
1. Update To .Net 10
1. Add Benchmarks
1. Improve Reactor pipeline
1. Make Reactor pipeline pluggable

## Tasks Remaining

1. Update readme
1. Document breaking changes and how to move from Innovation to Innovation.vNext
1. Create wiki
1. Code review
1. Profile and improve performance of the dispatcher pipeline for both commands and queries

## Alpha Warning

Please note, at this point this is a work in progress, therefore it is considered alpha grade software.
This code base and the api surface may still change.

## External Dependencies

1. MiniValidation by Damian Edwards [Link](https://github.com/DamianEdwards/MiniValidation). This replaces the outdated self written recursive validator

---

## CQRS vs Mediator (Important Terminology)

- **CQRS** is the architectural pattern: commands (writes) and queries (reads) are separated.
- **Mediator** is the dispatch mechanism: requests are routed through a central pipeline to handlers.
- **Innovation**: CQRS is the primary model; mediator-style dispatch is how execution is coordinated.

---

## Clean Architecture Alignment

Innovation naturally supports Clean Architecture boundaries:

- **Presentation layer** (API/UI): creates and dispatches commands/queries.
- **Application layer**: contains command/query handlers, validators, interceptors.
- **Domain layer**: contains business rules, isolated from transport concerns.
- **Infrastructure layer**: provides persistence, external integrations, auditing.

This keeps endpoints/controllers thin and use-case logic explicit and testable.

---

## Dispatcher Command Pipeline

Commands:  
`Dispatcher -> Command Reactors -> Command Interceptors -> Command Validators -> Command Handler -> Command Result Reactors -> Audit Store -> Return Result`

---

## Framework Components

### Command Reactors

First step in command dispatch. Useful for logging or priming external/internal services about an incoming command.

- Command is passed by reference (mutation here is discouraged).
- Reactors do not influence pipeline execution.
- Run in parallel on a background thread.
- Must implement `ICommandReactor`

### Command Interceptors

Second step in command dispatch.

- Can modify commands/properties where required.
- Run sequentially (one after another, not in parallel).
- Must implement `ICommandInterceptor`

### Command Validators

Third step in command dispatch (separation of concerns).

- Validate command input before handler execution.
- If validation fails, handler is not called.
- If multiple validators exist, processing stops after first error-producing validator.
- Must implement `IValidator`

### Command Validation (Fallback)

If custom command validators are not registered, the framework validates using:

1. `System.ComponentModel.DataAnnotations.Validator`
2. `System.ComponentModel.DataAnnotations.IValidatableObject` (if implemented)

On failure, handler execution is skipped and validation errors are returned.

### Commands Handlers

Fourth step in command dispatch.

- Used to alter state.
- Must implement `ICommand`.
- Exactly one handler per command.

### Command Result Reactors

Fifth step in command dispatch.

- Useful for logging and auditing side effects.
- Do not influence execution outcome.
- Run in parallel on a background thread.
- Must implement `ICommandResultReactor`

### Audit Store

Final step in command dispatch.

Supports centralized auditing of commands, queries, and messages.

- Implement `IAuditStore`
- Register with DI
- If present, audit hooks are called; if absent, skipped.

## Query Pipeline

Queries:  
`Dispatcher -> Audit Store -> Query Handler  -> Return Result`

---

### Queries

- Used to read/load data.
- Must implement `IQuery`.
- Exactly one handler per query.

### Query Results

- Returned by query handlers.
- Must implement `IQueryResult`.
- Interface is framework-tracking oriented; no required fields/properties.

### Messages

Messages can broadcast to multiple handlers.

### Correlation

Dispatcher can create or consume an incoming correlation ID.

- ASP.NET Core implementation available using `X-Correlation-ID`.
- Handlers can implement `ICorrelationAware`.
- Dispatcher sets `CorrelationId` before `Handle` is called.

### SearchLocations

The Innovation loader can load assemblies from specified locations to support modular architectures.

---

## Dispatcher Context

> Reserved section (recommended: document how correlation, audit, and dispatch metadata are carried per request).

---

## Supported .NET Frameworks

1. .NET Standard 2.0
2. .NET 10.0

---

## Samples

- `Innovation.Sample.Console`
- `Innovation.Sample.Web`

---

## Tests

One primary test project plus two additional test-directory projects used to validate loading behavior.

---

## Building

1. Visual Studio 2026 >= 18.8.1
2. Latest .NET SDK  
   [Download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
3. Latest .NET Runtime  
   [Download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

---

## Benchmark Results

### Unit reference
- 1 second = 1,000 ms
- 1 second = 1,000,000 us
- 1 second = 1,000,000,000 ns

### DataAnnotationsValidator with `BlankCommand`
| Method          | Mean     | Error    | StdDev   | Gen0   | Allocated |
|---------------- |---------:|---------:|---------:|-------:|----------:|
| BlankCommandNew | 43.02 ns | 0.110 ns | 0.092 ns | 0.0023 |      24 B |

Operations per second: `1 000 000 000 / 43.02 = 23 245 002`

### DataAnnotationsValidator with `InsertCustomer`
| Method                | Mean     | Error   | StdDev  | Gen0   | Allocated |
|---------------------- |---------:|--------:|--------:|-------:|----------:|
| InsertCustomerCommand | 705.8 ns | 2.58 ns | 2.29 ns | 0.1144 |   1.17 KB |

Operations per second: `1 000 000 000 / 705.8 = 1 416 831`

### Dispatcher with `BlankCommand`
| Method               | Mean     | Error   | StdDev  | Gen0   | Allocated |
|--------------------- |---------:|--------:|--------:|-------:|----------:|
| DispatchBlankCommand | 171.0 ns | 3.43 ns | 3.04 ns | 0.0052 |      56 B |

Operations per second: `1 000 000 000 / 172.2 = 5 807 200`