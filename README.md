# Innovation.vNext

[![NuGet Innovation.Api.vNext](https://img.shields.io/nuget/v/Innovation.Api.vNext.svg?label=Innovation.Api.vNext)](https://www.nuget.org/packages/Innovation.Api.vNext)
[![NuGet Innovation.ServiceBus.InProcess.vNext](https://img.shields.io/nuget/v/Innovation.ServiceBus.InProcess.vNext.svg?label=Innovation.ServiceBus.InProcess.vNext)](https://www.nuget.org/packages/Innovation.ServiceBus.InProcess.vNext)
[![NuGet Innovation.Integration.AspNetCore.vNext](https://img.shields.io/nuget/v/Innovation.Integration.AspNetCore.vNext.svg?label=Innovation.Integration.AspNetCore.vNext)](https://www.nuget.org/packages/Innovation.Integration.AspNetCore.vNext)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/louislewis2/innovation-vnext/blob/master/LICENSE)
[![Release Build](https://github.com/louislewis2/innovation-vnext/actions/workflows/publish-nuget.yml/badge.svg)](https://github.com/louislewis2/innovation-vnext/actions/workflows/publish-nuget.yml)

A simple, **performance-focused** framework for implementing **CQRS** in .NET applications with **immediate consistency**.  
Innovation does not implement, and does not attempt to support, Event Sourcing.

Innovation is **CQRS-first** and uses a **mediator-style dispatch pipeline** internally to route commands/queries to handlers and apply cross-cutting behaviors.

> Innovation.vNext is the performance-focused evolution of [Innovation](https://github.com/louislewis2/innovation). See [Performance](#performance) for measured gains.

> Wondering how Innovation compares to other in-process mediator libraries? See **[BENCHMARKS.md](https://github.com/louislewis2/innovation-vnext/blob/master/BENCHMARKS.md#comparison-with-mediatr-and-mediator)** for a reproducible, side-by-side comparison against MediatR and Mediator.

> [!WARNING]
> **Innovation.vNext is alpha software.** This is a work in progress. The API surface may still
> change, and the packages currently target **.NET 10 only**. Please weigh that before adopting it.

---

## Table of Contents

- [Quick Start](#quick-start)
- [Why Innovation](#why-innovation)
- [Performance](#performance)
- [Roadmap](#roadmap)
- [External Dependencies](#external-dependencies)
- [CQRS vs Mediator (Important Terminology)](#cqrs-vs-mediator-important-terminology)
- [Clean Architecture Alignment](#clean-architecture-alignment)
- [Dispatcher Command Pipeline](#dispatcher-command-pipeline)
- [Framework Components](#framework-components)
- [Query Pipeline](#query-pipeline)
- [Supported .NET Frameworks](#supported-net-frameworks)
- [Samples](#samples)
- [Tests](#tests)
- [Building](#building)
- [Benchmarks](#benchmarks)
- [Versioning](#versioning)
- [Contributing](#contributing)
- [Related Projects](#related-projects)
- [License](#license)

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
- **Simple request/handler model** with low ceremony.
- **Clean architecture alignment** for maintainable boundaries.
- **Extensible pipeline** for validation, logging, transactions, and other cross-cutting concerns.
- **Built-in auditing** through a pluggable `IAuditStore`.
- **MIT licensed**, free and open source.

---

## Performance

Innovation.vNext is a ground-up performance pass over the original [Innovation](https://github.com/louislewis2/innovation) library. Early benchmarks (BenchmarkDotNet) show substantial gains in both speed and allocations:

| Scenario | Legacy Innovation | Innovation.vNext | Improvement |
|---|---:|---:|---:|
| Validate `BlankCommand` (DataAnnotations) | 460.1 ns / 1.07 KB | 46.53 ns / 24 B | **~9.9x faster, ~46x less memory** |
| Validate `InsertCustomer` (DataAnnotations) | 2.478 us / 3.36 KB | 775.5 ns / 1.3 KB | **~3.2x faster, ~2.6x less memory** |
| Dispatch `BlankCommand` (end-to-end, no audit store registered) | 2.032 us / 2.5 KB | 63.15 ns / 24 B | **~32.2x faster, ~107x less memory** |

Registering an audit store (a common, realistic setup) adds real, measurable cost - see [BENCHMARKS.md](https://github.com/louislewis2/innovation-vnext/blob/master/BENCHMARKS.md#audit-store) for that comparison, and for full methodology and raw results.

> Note on the two `DataAnnotations` rows: those benchmarks construct `DataAnnotationsValidator`/MiniValidation directly and never enter the dispatcher, so they measure validation cost in isolation rather than dispatch cost.

### How Innovation compares to other libraries

The table above measures Innovation.vNext against its own predecessor. If you want to see how it compares to other in-process mediator libraries, there is a separate, self-contained report:

**[Comparison with MediatR and Mediator](https://github.com/louislewis2/innovation-vnext/blob/master/BENCHMARKS.md#comparison-with-mediatr-and-mediator)** - Innovation vNext measured against [MediatR](https://github.com/LuckyPennySoftware/MediatR) and [Mediator](https://github.com/martinothamar/Mediator).

It covers command, query, message and pipeline-loaded dispatch, the handler-lifetime tradeoff that shapes the results, what the benchmarks deliberately do not equalize, and full methodology so you can reproduce every number yourself.

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
- Dispatch-path optimization pass, with no API or behavior change: the dispatcher's built-in logging is guarded by a single `ILogger.IsEnabled` check per dispatch rather than one per call site, `GetServices` resolutions no longer make a redundant array copy, the memoized audit-store fast path extends from `Command` to `Query`/`Message`/`QueryFor`/`MessageFor`, the command-bits lookup is frozen after configuration, and `CorrelationId` is generated lazily
- `ICorrelationAware` is now consistent across the pipeline: the correlation ID is set on command handlers, query handlers and reactors, never on the command or query itself. Commands that implement `ICorrelationAware` are reported at startup with guidance on where to move the interface

### Planned

- Document breaking changes and how to move from Innovation to Innovation.vNext
- Create wiki
- Code review

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
- Command handlers, query handlers and reactors can implement `ICorrelationAware`.
- Dispatcher sets `CorrelationId` before `Handle` is called, and before `React` is called.

### SearchLocations

The Innovation loader can load assemblies from specified locations to support modular architectures.

---

## Supported .NET Frameworks

Innovation.vNext targets **.NET 10 only**. All three packages are built for `net10.0`:

| Package | Target framework |
|---|---|
| `Innovation.Api.vNext` | .NET 10 |
| `Innovation.ServiceBus.InProcess.vNext` | .NET 10 |
| `Innovation.Integration.AspNetCore.vNext` | .NET 10 |

There is currently no .NET Standard or down-level .NET target. If you are on an earlier runtime,
Innovation.vNext will not restore.

---

## Samples

The [`samples/`](https://github.com/louislewis2/innovation-vnext/tree/master/samples) folder
contains a layered sample application laid out along Clean Architecture boundaries:

| Project | Role |
|---|---|
| `Innovation.Sample.Api` | Commands, queries, criteria and view models - the application contract |
| `Innovation.Sample.BaseModule` | Command and query handlers, reactors and result reactors |
| `Innovation.Sample.Data` | EF Core contexts, persistence models and an `IAuditStore` implementation |
| `Innovation.Sample.Infrastructure` | Configuration and settings |
| `Innovation.Sample.Web` | ASP.NET Core controllers that dispatch commands and queries |
| `Innovation.Sample.Console` | Minimal console host |

For a realistic end-to-end setup, start with
[`Innovation.Sample.Web`](https://github.com/louislewis2/innovation-vnext/tree/master/samples/Innovation.Sample.Web)
and follow a command through `Innovation.Sample.BaseModule` into `Innovation.Sample.Data`.

---

## Tests

| Project | Role |
|---|---|
| `Innovation.ServiceBus.InProcess.Tests` | Primary test suite (MSTest, 31 tests) |
| `Innovation.ApiSample` | Shared command, query and message definitions used by tests and benchmarks |
| `Innovation.SampleApi.Consumer` | Handlers, validators, interceptors, reactors and a sample audit store |
| `Innovation.Benchmarks` | Innovation's own BenchmarkDotNet suite |
| `Innovation.Benchmarks.Comparison` | Comparison benchmarks against MediatR and Mediator |
| `Innovation.Benchmarks.Comparison.MediatorScoped` | Mediator `Scoped`-lifetime benchmarks, in a separate assembly because the lifetime is a compile-time setting |
| `Innovation.CaptiveDependency.Demo` | Standalone console app demonstrating the DI scope rules discussed in [BENCHMARKS.md](https://github.com/louislewis2/innovation-vnext/blob/master/BENCHMARKS.md#handler-lifetime-and-what-it-costs) |

```bash
dotnet test
```

---

## Building

1. Visual Studio 2026 >= 18.8.1
2. Latest .NET SDK  
   [Download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
3. Latest .NET Runtime  
   [Download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

---

## Benchmarks

Full benchmark results are published in
[BENCHMARKS.md](https://github.com/louislewis2/innovation-vnext/blob/master/BENCHMARKS.md),
covering both Innovation's own pipeline benchmarks (dispatch, audit store, reactors, interceptors,
validators, validation aggregation) and the side-by-side comparison against MediatR and Mediator.

---

## Versioning

Innovation.vNext follows [semantic versioning](https://semver.org/):

- **Major** for breaking changes
- **Minor** for backward-compatible new features
- **Patch** for bug fixes

While the packages are pre-1.0 and marked alpha, breaking changes may land in minor versions. Once
1.0 ships, breaking changes will be reserved for major versions. Release notes are published on the
[releases page](https://github.com/louislewis2/innovation-vnext/releases).

---

## Contributing

Contributions are welcome - issues, discussions and pull requests alike. See
[CONTRIBUTING.md](https://github.com/louislewis2/innovation-vnext/blob/master/CONTRIBUTING.md) for
how to build, test and benchmark the project locally.

If you find an error in the benchmarks, or in anything this repository states about another
library, please open an issue. Corrections are genuinely appreciated.

---

## Related Projects

There are several good in-process messaging and mediator libraries for .NET. Depending on what you
need, one of these may suit your application better than Innovation:

- [MediatR](https://github.com/LuckyPennySoftware/MediatR) - the original and most widely used .NET
  mediator implementation. Reflection-based, in-memory only. Much of the vocabulary the rest of us
  use comes from here.
- [Mediator](https://github.com/martinothamar/Mediator) by martinothamar - source-generator based,
  with a MediatR-like API, full Native AOT support and built-in OpenTelemetry metrics and tracing.
- [Foundatio.Mediator](https://github.com/FoundatioFx/Foundatio.Mediator) - a conventions-based API,
  also source-generator based, in-memory only.
- [Wolverine](https://wolverinefx.net/) - conventions-based, and a larger framework that also covers
  asynchronous and distributed messaging.
- [MassTransit](https://masstransit.io/) - distributed messaging, which also offers an in-memory
  mediator implementation.
- Innovation.vNext (this library) - CQRS-first rather than mediator-first, with interceptors,
  validators, reactors and an audit store hook built into the dispatch pipeline rather than composed
  from pipeline behaviors. Resolves from the DI container per dispatch. .NET 10 only.

If you are weighing Innovation against MediatR or Mediator specifically,
[BENCHMARKS.md](https://github.com/louislewis2/innovation-vnext/blob/master/BENCHMARKS.md#comparison-with-mediatr-and-mediator)
measures all three in the same harness, and documents what those measurements do and do not cover.

---

## License

Innovation.vNext is licensed under the
[MIT License](https://github.com/louislewis2/innovation-vnext/blob/master/LICENSE).
