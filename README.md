# Innovation.vNext

[![NuGet Innovation.Api.vNext](https://img.shields.io/nuget/v/Innovation.Api.vNext.svg?label=Innovation.Api.vNext)](https://www.nuget.org/packages/Innovation.Api.vNext)
[![NuGet Innovation.ServiceBus.InProcess.vNext](https://img.shields.io/nuget/v/Innovation.ServiceBus.InProcess.vNext.svg?label=Innovation.ServiceBus.InProcess.vNext)](https://www.nuget.org/packages/Innovation.ServiceBus.InProcess.vNext)
[![NuGet Innovation.Integration.AspNetCore.vNext](https://img.shields.io/nuget/v/Innovation.Integration.AspNetCore.vNext.svg?label=Innovation.Integration.AspNetCore.vNext)](https://www.nuget.org/packages/Innovation.Integration.AspNetCore.vNext)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/louislewis2/innovation-vnext/blob/master/LICENSE)
[![Release Build](https://github.com/louislewis2/innovation-vnext/actions/workflows/publish-nuget.yml/badge.svg)](https://github.com/louislewis2/innovation-vnext/actions/workflows/publish-nuget.yml)

A simple, **performance-focused** framework for implementing **CQRS** in .NET applications with **immediate consistency**.  
Innovation does not implement, and does not attempt to support, Event Sourcing.

Innovation is **CQRS-first** and uses a **mediator-style dispatch pipeline** internally to route commands/queries to handlers and apply cross-cutting behaviors.

> Innovation.vNext is the performance-focused evolution of [Innovation](https://github.com/louislewis2/innovation). See the [Performance](#performance) section below for measured gains.

---

## Quick Start

```bash
dotnet add package Innovation.Api.vNext
dotnet add package Innovation.ServiceBus.InProcess.vNext
```

```csharp
using System.Threading.Tasks;
using Innovation.Api.vNext.Commanding;
using Innovation.Api.vNext.CommandHelpers;
using Innovation.Api.vNext.Dispatching;
using Microsoft.Extensions.DependencyInjection;

// 1. Define a command
public class CreateCustomerCommand : ICommand
{
    public string EventName => nameof(CreateCustomerCommand);

    public string FirstName { get; }
    public string LastName { get; }

    public CreateCustomerCommand(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
}

// 2. Implement a handler for it
public class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand>
{
    public ValueTask<ICommandResult> Handle(CreateCustomerCommand command)
    {
        // ... persist the customer ...
        return ValueTask.FromResult<ICommandResult>(new CommandResult());
    }
}

// 3. Register Innovation - it discovers and registers your handlers automatically
var services = new ServiceCollection();
services.AddInnovationvNext();

var provider = services.BuildServiceProvider();

// 4. Dispatch the command
var dispatcher = provider.GetRequiredService<IDispatcher>();
var result = await dispatcher.Command(new CreateCustomerCommand("Louis", "Lewis"));
```

---

## Why Innovation

- **Performance-first**: vNext delivers order-of-magnitude gains over the original Innovation - see [Performance](#performance).
- **CQRS-first design** for explicit write/read separation.
- **Free forever** (no intent to charge).
- **Simple request/handler model** with low ceremony.
- **Clean architecture alignment** for maintainable boundaries.
- **Extensible pipeline** for validation, logging, transactions, and other cross-cutting concerns.

---

## Performance

Innovation.vNext is a ground-up performance pass over the original [Innovation](https://github.com/louislewis2/innovation) library. Early benchmarks (BenchmarkDotNet) show substantial gains in both speed and allocations:

| Scenario | Legacy Innovation | Innovation.vNext | Improvement |
|---|---:|---:|---:|
| Validate `BlankCommand` (DataAnnotations) | 460.1 ns / 1.07 KB | 40.17 ns / 24 B | **~11.5x faster, ~45x less memory** |
| Validate `InsertCustomer` (DataAnnotations) | 2.478 us / 3.36 KB | 737.2 ns / 1.17 KB | **~3.4x faster, ~2.9x less memory** |
| Dispatch `BlankCommand` (end-to-end, no audit store registered) | 2.032 us / 2.5 KB | 91.81 ns / 24 B | **~22.1x faster, ~104x less memory** |

> Benchmarks are directional: results depend on hardware, runtime, workload shape, and which pipeline behaviors (validation, reactors, interceptors, audit store) are enabled. The Dispatch `BlankCommand` row above is measured with **no IAuditStore registered** - it represents the framework's zero-registration floor. Registering an audit store (a common, realistic setup) adds real, measurable cost - see [Benchmark Results](#benchmark-results) for that comparison. Full methodology and raw results are in [Benchmark Results](#benchmark-results).

---

## Roadmap

### Shipped in vNext

- Improved Command Pipeline Performance
- Implemented CommandResult Errors As Per RFC 7807
- Use ValueTask Over Task To Better Support Synchronous Operations
- Improved Query Pipeline Performance
- Updated To .NET 10
- Added Benchmarks
- Improved Reactor pipeline
- Made Reactor pipeline pluggable
- Expanded benchmark coverage to the full dispatch pipeline (Command/Query/Message/MessageFor, reactors, interceptors, validators, audit store, validation aggregation) - all IO-free
- Fixed the default benchmark provider to not implicitly register an audit store, so "Blank"/baseline benchmarks measure the framework's true zero-registration floor rather than silently including audit-store overhead; audit-store cost is now tracked explicitly (see Audit Store Comparison Tests)

### Planned

- Document breaking changes and how to move from Innovation to Innovation.vNext
- Create wiki
- Code review
- Profile and improve performance of the dispatcher pipeline for both commands and queries

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
| BlankCommandNew | 40.17 ns | 0.081 ns | 0.076 ns | 0.0023 |      24 B |

Operations per second: `1 000 000 000 / 40.17 = 24 894 199`

### DataAnnotationsValidator with `InsertCustomer`
| Method                | Mean     | Error   | StdDev  | Gen0   | Allocated |
|---------------------- |---------:|--------:|--------:|-------:|----------:|
| InsertCustomerCommand | 737.2 ns | 1.51 ns | 1.41 ns | 0.1144 |   1.17 KB |

Operations per second: `1 000 000 000 / 737.2 = 1 356 505`

### Dispatcher with `BlankCommand` (no audit store registered)
| Method               | Mean     | Error   | StdDev  | Gen0   | Allocated |
|--------------------- |---------:|--------:|--------:|-------:|----------:|
| DispatchBlankCommand | 91.81 ns | 0.832 ns | 0.778 ns | 0.0023 |      24 B |

Operations per second: `1 000 000 000 / 91.81 = 10 892 275`

> Note: this benchmark - and every "Blank"/baseline benchmark below - uses no `IAuditStore` registration, since `DependencyBuilderBase`'s default constructor intentionally leaves the pipeline at its zero-registration floor. Earlier revisions of this README quoted ~171 ns / 56 B for this scenario; that number actually included an audit store registration and is now reported separately under **Audit Store Comparison Tests** below.

## All Benchmark Results

### Audit Store Comparison Tests
| Method                  | Mean      | Error    | StdDev   | Ratio        | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |----------:|---------:|---------:|-------------:|--------:|-------:|----------:|------------:|
| AuditStoreNotRegistered |  96.46 ns | 0.689 ns | 0.644 ns |     baseline |         | 0.0023 |      24 B |             |
| AuditStoreRegistered    | 166.73 ns | 0.564 ns | 0.440 ns | 1.73x slower |   0.01x | 0.0052 |      56 B | 2.33x more  |

Registering an `IAuditStore` (a common, realistic setup - most consumers will want auditing) adds ~75 ns and an extra allocation per dispatch: an `AuditContext`, plus `SampleAuditStore`'s per-correlation-id `Dictionary`/`List<IEvent>` bookkeeping.

### BlankCommand DataAnnotations Validator Tests
| Method          | Mean     | Error    | StdDev   | Gen0   | Allocated |
|---------------- |---------:|---------:|---------:|-------:|----------:|
| BlankCommandNew | 40.17 ns | 0.081 ns | 0.076 ns | 0.0023 |      24 B |

### DataAnnotations Validator Tests
| Method                | Mean     | Error   | StdDev  | Gen0   | Allocated |
|---------------------- |---------:|--------:|--------:|-------:|----------:|
| InsertCustomerCommand | 737.2 ns | 1.51 ns | 1.41 ns | 0.1144 |   1.17 KB |

### Dispatcher Command Tests
| Method               | Mean     | Error   | StdDev  | Gen0   | Allocated |
|--------------------- |---------:|--------:|--------:|-------:|----------:|
| DispatchBlankCommand | 91.81 ns | 0.832 ns | 0.778 ns | 0.0023 |      24 B |

### Dispatcher Message Tests
| Method             | Mean     | Error   | StdDev  | Ratio        | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|--------:|--------:|-------------:|--------:|-------:|----------:|------------:|
| DispatchMessage    |  90.90 ns | 0.522 ns | 0.463 ns |     baseline |         | 0.0083 |      88 B |             |
| DispatchMessageFor | 316.23 ns | 0.841 ns | 0.786 ns | 3.48x slower |   0.02x | 0.0701 |     736 B | 8.36x more  |

### Dispatcher Pipeline Comparison Tests
| Method                            | Mean        | Error    | StdDev   | Ratio         | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|---------:|---------:|--------------:|--------:|-------:|-------:|----------:|------------:|
| Blank                             |    88.06 ns | 0.330 ns | 0.308 ns |      baseline |         | 0.0023 |      - |      24 B |             |
| ReactorAndResultReactor           |   375.18 ns | 2.188 ns | 1.827 ns |  4.26x slower |   0.02x | 0.0405 | 0.0200 |     423 B | 17.62x more |
| Interceptor                       |   150.11 ns | 0.599 ns | 0.561 ns |  1.70x slower |   0.01x | 0.0105 |      - |     112 B |  4.67x more |
| DataAnnotationsAndCustomValidator | 1,474.23 ns | 6.032 ns | 5.642 ns | 16.74x slower |   0.08x | 0.1965 |      - |    2064 B | 86.00x more |

### Dispatcher Query Tests
| Method             | Mean     | Error   | StdDev  | Gen0   | Allocated |
|------------------- |---------:|--------:|--------:|-------:|----------:|
| DispatchBlankQuery | 82.23 ns | 0.478 ns | 0.447 ns | 0.0023 |      24 B |

### Validation Aggregation Comparison Tests
| Method    | Mean     | Error     | StdDev    | Ratio        | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------- |---------:|----------:|----------:|-------------:|--------:|-------:|----------:|------------:|
| FailFast  | 2.044 us | 0.0086 us | 0.0077 us |     baseline |         | 0.3777 |   3.88 KB |             |
| Aggregate | 2.157 us | 0.0140 us | 0.0131 us | 1.06x slower |   0.01x | 0.3700 |   3.81 KB | 1.02x less  |

`InsertVendorCommand` fails both DataAnnotations and the custom `InsertVendorAddressValidator` in this benchmark, so `Aggregate` genuinely has two error sets to merge; the ~113 ns difference is the cost of that merge (`AggregateValidationErrors = true`) versus failing fast on the first validator that reports an error (default).

### ValueStopwatch Tests
| Method         | Mean     | Error    | StdDev   | Allocated |
|--------------- |---------:|---------:|---------:|----------:|
| StopWatchUsage | 30.66 ns | 0.012 ns | 0.011 ns |         - |