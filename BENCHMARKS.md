# Benchmarks

This document reports every published benchmark result for Innovation.vNext. All numbers are
produced by BenchmarkDotNet from projects in this repository, and every one of them can be
reproduced by running those projects yourself.

It covers two independent sets of measurements:

- **[Comparison with MediatR and Mediator](#comparison-with-mediatr-and-mediator)** - Innovation
  vNext measured alongside two other in-process mediator libraries in narrow, IO-free dispatch
  scenarios.
- **[Innovation's own pipeline benchmarks](#innovations-own-pipeline-benchmarks)** - what each part
  of Innovation's own pipeline costs: dispatch, audit store, reactors, interceptors, validators and
  validation aggregation.

---

## Comparison with MediatR and Mediator

This section measures **Innovation vNext** alongside **MediatR** and **Mediator** by martinothamar
(source-generator based), using the `Innovation.Benchmarks.Comparison` project.

It is not an attempt to rank the three libraries. It exists so that a reader can see what each
one costs in a set of narrow, IO-free dispatch scenarios, what each configuration requires of the
surrounding application, and exactly how those numbers were produced. Every figure below can be
reproduced by running the projects yourself - see [Reproducing these
results](#reproducing-these-results).

**These numbers are a point-in-time snapshot of specific package versions**, not a standing claim
about any library. The versions measured are [MediatR
12.5.0](https://github.com/LuckyPennySoftware/MediatR/releases) and [Mediator
3.0.2](https://github.com/martinothamar/Mediator/releases). Both projects are actively developed and
later versions may perform differently - Mediator 3.1, for example, adds a `CachingMode` option that
governs when handler wrappers are initialized, which is exactly the kind of thing these benchmarks
measure. Check the current releases before drawing conclusions about today's packages.
### TL;DR

Steady-state dispatch of a single IO-free command, query or message. Lower is better in both
columns.

| Scenario | Innovation | MediatR | Mediator (Singleton) | Mediator (Scoped) |
|---|---:|---:|---:|---:|
| Command, no pipeline behaviors | 57.98 ns / 24 B | 70.81 ns / 272 B | 15.31 ns / 0 B | 45.31 ns / 64 B |
| Query, no pipeline behaviors | 53.38 ns / 24 B | 76.11 ns / 272 B | 15.28 ns / 0 B | not measured |
| Message, two handlers | 50.37 ns / 56 B | 117.95 ns / 440 B | 16.20 ns / 0 B | not measured |
| Command, validation + logging on all | 99.44 ns / 80 B | 157.08 ns / 640 B | 39.61 ns / 0 B | 93.75 ns / 256 B |
| Command, resolved per request | 218.7 ns / 512 B | not measured | not measured | 239.8 ns / 632 B |

- Innovation is faster than MediatR in all four steady-state scenarios (by 1.22x to 2.34x) and
  allocates 7.9x to 11.3x less.
- Mediator in its default **Singleton** configuration is faster than Innovation in all four
  (by 2.51x to 3.79x) and allocates nothing. Singleton handlers cannot constructor-inject a
  Scoped service such as an EF Core `DbContext`; see [Handler lifetime and what it
  costs](#handler-lifetime-and-what-it-costs).
- Mediator in **Scoped** configuration, which removes that restriction, is 1.28x faster than
  Innovation with no behaviors and 1.06x faster with an equivalent validation + logging pipeline,
  while allocating 2.7x and 3.2x more respectively. With a scope and entry point resolved per
  dispatch, Innovation is 1.10x faster and allocates 1.23x less - though that particular row is
  the least directly comparable measurement in this document, for reasons given
  [below](#per-request-resolution).

### What each library is doing in these benchmarks

| Capability | Innovation | MediatR | Mediator |
|---|---|---|---|
| Command / Query / Message dispatch | Yes | Yes | Yes |
| Pipeline behaviors (cross-cutting) | Built in (interceptors, validators, reactors) | Opt-in (`IPipelineBehavior<,>`) | Opt-in (`IPipelineBehavior<,>`) |
| Audit store hook | Built in | Not provided | Not provided |
| Compile-time source-generated dispatch | No (runtime DI resolution) | No (runtime DI resolution) | Yes |
| Default/typical handler & behavior DI lifetime | Scoped/Transient (your choice) | Transient (your choice) | Singleton (recommended, configurable) |

These are the capabilities this document measures, and the one - compile-time source-generated
dispatch - that explains a large part of why the numbers fall where they do. **It is not a feature
comparison.** All three libraries do things not listed here, and each project documents its own
capabilities far better than this page could:
[MediatR](https://github.com/LuckyPennySoftware/MediatR),
[Mediator](https://github.com/martinothamar/Mediator). The table deliberately does not weight these
capabilities against each other, since their value depends entirely on the consuming application.

### What was measured

Three sets of benchmarks:

1. **Micro** (`CommandComparisonTests`, `QueryComparisonTests`, `MessageComparisonTests`) - one
   command/query/message, a handler that does no real work, and **no pipeline behaviors registered
   on any of the three libraries**. This isolates the dispatch mechanism itself: resolve the
   handler, invoke it.
2. **Loaded** (`LoadedCommandComparisonTests`) - the same command and handler, with an equivalent
   validation behavior and logging behavior registered on **all three** libraries. The validation
   step is a no-op check (`IValidator<T>` on Innovation, `IPipelineBehavior<,>` on the other two).
   The logging step resolves an `ILogger<T>` and calls it before and after the handler, guarded by
   `ILogger.IsEnabled(LogLevel.Debug)`. The minimum log level is `Warning` on all three providers,
   so on every library that guard is exercised and evaluates to `false`, and no message is
   formatted or written.
3. **Per-request** (`LoadedCommandPerRequestComparisonTests`) - the loaded scenario, but creating a
   DI scope and resolving the entry point on every iteration rather than once.

#### What these benchmarks do not equalize

**Innovation performs its own built-in logging on every dispatch; MediatR and Mediator perform
none.** This is part of `Dispatcher.cs`, not an opt-in behavior. On a `Command` dispatch it covers
an entered-dispatcher log, a command-detail (Trace) log, a handler-found log, a
returning-from-dispatcher log, an audit-store-found log when an audit store is registered, and a
context-not-set warning for commands implementing `IContextAware` with no context set. Those call
sites are collectively guarded by a single `ILogger.IsEnabled(LogLevel.Debug)` check per dispatch,
so at the `Warning` minimum level used here the measured cost is one `IsEnabled` call rather than
one per statement. It is not zero. The micro benchmarks are therefore not a "no cross-cutting work
on any of the three" comparison - Innovation is doing something there that the other two are not.

**No `IAuditStore` is registered on Innovation in these comparison benchmarks.** The audit store is
an Innovation-only concept with no counterpart in MediatR or Mediator, so there is nothing to
compare it against and registering one would have made the comparison less equal, not more. The
consequence is that every Innovation figure in the tables below is its zero-audit floor. An
application that adopts Innovation *for* its auditing pays roughly 80 ns and an extra allocation per
dispatch on top of these numbers - that cost is measured, against Innovation alone, under
[Innovation's own pipeline benchmarks](#innovations-own-pipeline-benchmarks).

**`IsValidationEnabled` is `false` on `InnovationLoadedProvider`.** That option controls only
Innovation's built-in reflection-based `DataAnnotationsValidator`/MiniValidation pass, which
MediatR and Mediator have no equivalent of. Leaving it off keeps the loaded comparison to one
equivalent no-op validation step per library. The registered `IValidator<LoadedCommand>` - the
actual counterpart to the other two libraries' `ValidationBehavior` - runs regardless of this
option.

**Neither library pays per-dispatch scope creation in the micro or loaded tables.** `Dispatcher`
creates its own `IServiceScope` once, in its constructor, and the benchmarks resolve it once in
`GlobalSetup`, so every iteration reuses that scope. Mediator's generated `Scoped`-mode code
resolves through `mediator.Services`, fixed to the `IServiceProvider` that constructed it, and
`MediatorScopedProvider` likewise resolves `IMediator` once from the root container in
`GlobalSetup`. The [per-request](#per-request-resolution) benchmark measures the opposite case
deliberately.

#### Environment

All numbers come from a single point-in-time run per project on the machine below, using
BenchmarkDotNet's default job. Different hardware or a different .NET SDK patch version will
produce different absolute numbers.

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
Intel Core i9-10900K CPU 3.70GHz, 1 CPU, 20 logical and 10 physical cores
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
```

Mediator's `ServiceLifetime` is a compile-time, assembly-wide source-generator setting, so its
`Scoped` rows cannot share an assembly - and therefore cannot share a benchmark job - with its
`Singleton` rows. Every `Mediator (Scoped)` figure in this document was measured in a separate
process, from the standalone `Innovation.Benchmarks.Comparison.MediatorScoped` project. Those rows
are marked with an asterisk. Where a ratio is given against Innovation for such a row, it was
calculated by hand from the two means for readability; BenchmarkDotNet did not compute it, and it
carries the additional uncertainty of a cross-process comparison.

### Results

#### Command, no pipeline behaviors

| Method             | Mean     | Error    | StdDev   | Ratio        | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|---------:|-------------:|--------:|-------:|----------:|------------:|
| Command_Innovation | 57.98 ns | 0.136 ns | 0.113 ns |     baseline |         | 0.0023 |      24 B |             |
| Command_MediatR    | 70.81 ns | 0.303 ns | 0.268 ns | 1.22x slower |   0.01x | 0.0260 |     272 B | 11.33x more |
| Command_Mediator   | 15.31 ns | 0.042 ns | 0.039 ns | 3.79x faster |   0.01x |      - |         - |          NA |
| Command_Mediator_Scoped* | 45.31 ns | 0.198 ns | 0.176 ns | 1.28x faster |   0.01x | 0.0061 |      64 B |  2.67x more |

#### Query, no pipeline behaviors

| Method           | Mean     | Error    | StdDev   | Ratio        | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|---------:|-------------:|--------:|-------:|----------:|------------:|
| Query_Innovation | 53.38 ns | 0.248 ns | 0.232 ns |     baseline |         | 0.0023 |      24 B |             |
| Query_MediatR    | 76.11 ns | 0.330 ns | 0.292 ns | 1.43x slower |   0.01x | 0.0260 |     272 B | 11.33x more |
| Query_Mediator   | 15.28 ns | 0.029 ns | 0.025 ns | 3.49x faster |   0.02x |      - |         - |          NA |

#### Message, two handlers

| Method             | Mean      | Error    | StdDev   | Ratio        | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|-------------:|--------:|-------:|----------:|------------:|
| Message_Innovation |  50.37 ns | 0.075 ns | 0.070 ns |     baseline |         | 0.0053 |      56 B |             |
| Message_MediatR    | 117.95 ns | 0.464 ns | 0.434 ns | 2.34x slower |   0.01x | 0.0420 |     440 B |  7.86x more |
| Message_Mediator   |  16.20 ns | 0.014 ns | 0.011 ns | 3.11x faster |   0.00x |      - |         - |          NA |

#### Command, with equivalent validation + logging on all three

| Method                     | Mean      | Error    | StdDev   | Ratio         | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|---------:|---------:|--------------:|--------:|-------:|----------:|------------:|
| Command_Innovation_Loaded  |  99.44 ns | 0.219 ns | 0.194 ns |      baseline |         | 0.0076 |      80 B |             |
| Command_MediatR_Loaded     | 157.08 ns | 0.820 ns | 0.641 ns |  1.58x slower |   0.01x | 0.0610 |     640 B |  8.00x more |
| Command_Mediator_Loaded    |  39.61 ns | 0.067 ns | 0.060 ns |  2.51x faster |   0.01x |      - |         - |          NA |
| Command_Mediator_Scoped_Loaded* | 93.75 ns | 0.967 ns | 0.905 ns | 1.06x faster |   0.01x | 0.0244 |     256 B | 3.20x more |

#### Per-request resolution

The tables above resolve the dispatcher or mediator once and reuse it, which isolates dispatch
cost. ASP.NET Core instead creates a DI scope per HTTP request and resolves the entry point from
it. This benchmark models that shape: create a scope, resolve the entry point, dispatch one loaded
command, dispose the scope.

| Method                                     | Mean     | Error   | StdDev  | Gen0   | Allocated |
|------------------------------------------- |---------:|--------:|--------:|-------:|----------:|
| Command_Innovation_Loaded_PerRequest        | 218.7 ns | 0.84 ns | 0.79 ns | 0.0489 |     512 B |
| Command_Mediator_Scoped_Loaded_PerRequest*  | 239.8 ns | 1.02 ns | 0.95 ns | 0.0601 |     632 B |

Treat this pair with more caution than the tables above it. The two rows were measured in separate
processes rather than as a single BenchmarkDotNet job, and the gap between them (roughly 9% in
time) is far smaller than in any of the same-job comparisons. It is reported because the per-request
shape is closer to how a web application actually behaves than the steady-state tables are, not
because the margin is large.

MediatR is not represented here; no equivalent per-request benchmark was written for it.

### Handler lifetime and what it costs

The largest differences in the tables above track the DI lifetime the library expects handlers to
use, so it is worth setting out what that choice involves.

**A Singleton cannot constructor-inject a Scoped service.** This is a rule of
`Microsoft.Extensions.DependencyInjection`, not a property of any mediator library. There are two
possible outcomes, depending on how the provider is configured.

With `ServiceProviderOptions.ValidateOnBuild` enabled - which the ASP.NET Core host does in the
Development environment - it fails when the provider is built, before a single request is served:

```
Error while validating the service descriptor 'ServiceType: ScopedDemo.SingletonConsumer
Lifetime: Singleton ImplementationType: ScopedDemo.SingletonConsumer': Cannot consume scoped
service 'ScopedDemo.ScopedService' from singleton 'ScopedDemo.SingletonConsumer'.
```

With only `ValidateScopes` enabled, nothing is checked up front and the same message is thrown as an
`InvalidOperationException` the first time the singleton is resolved.

With scope validation disabled, nothing throws at all. A Singleton is always resolved from the root
provider, so its Scoped dependency is created in the **root scope** and kept for the lifetime of the
application. Every scope that uses the singleton shares that one instance - and it is not the
instance belonging to that scope. This is the "captive dependency" case, and it is the quieter of
the two failure modes: a handler holding a `DbContext` this way is not holding the request's
`DbContext`, and that context is never disposed with the request.

All three behaviors can be reproduced from this repository. The
`test/Innovation.CaptiveDependency.Demo` project registers a Scoped service and a Singleton that
constructor-injects it, exercises all three provider configurations, and verifies its own
expectations - it returns a non-zero exit code if any of them do not hold:

```powershell
cd test\Innovation.CaptiveDependency.Demo
dotnet run -c Release
```

It references nothing but `Microsoft.Extensions.DependencyInjection`. No mediator library is
involved, because the behavior belongs to the container rather than to any library measured here.

Mediator's documentation recommends Singleton, and explains why:

> Singleton lifetime is highly recommended as it yields the best performance. Every application
> is different, but it is likely that a lot of your message handlers doesn't keep state and have
> no need for transient or scoped lifetime. In a lot of cases those lifetimes only allocate lots
> of memory for no particular reason.
>
> - [Mediator README, section 3.4 Configuration](https://github.com/martinothamar/Mediator#34-configuration)

That reasoning is sound for handlers that hold no state. Whether it applies to a given application
depends on what its handlers inject. Mediator also publishes its own benchmarks across varying
lifetimes and project sizes, which are a better source for its performance characteristics than this
document: see the [Mediator benchmarks
folder](https://github.com/martinothamar/Mediator/blob/main/benchmarks/README.md).

The setting is global and applies to all handlers and behaviors at once; there is no per-handler
override in the standard configuration:

```csharp
options.ServiceLifetime = ServiceLifetime.Singleton;
```

Under Singleton, Mediator's per-request-type wrapper composes the handler and pipeline-behavior
delegate chain once and caches it in a field. Under Scoped, the source generator emits a different
wrapper for the same request type, with no cached field, that resolves the handler and behaviors
and rebuilds the chain inside `Handle` on every dispatch.

This can be checked against the two benchmark projects in this repository, which differ in exactly
that setting: `Innovation.Benchmarks.Comparison` calls `AddMediator()` and takes the default
Singleton lifetime, while `Innovation.Benchmarks.Comparison.MediatorScoped` carries
`[assembly: MediatorOptions(ServiceLifetime = ServiceLifetime.Scoped)]`. Add the following to each
`.csproj`, rebuild, and compare the emitted `RequestHandlerWrapper<TRequest,TResponse>` in
`obj/GeneratedFiles`:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)GeneratedFiles</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

The two snippets below are abbreviated for readability - the `global::` prefixes and the body of the
delegate-chain composition are elided - but the presence of the cached field in one and its absence
in the other is verbatim:

```csharp
// Singleton: composed once in Init(...), cached, reused.
private MessageHandlerDelegate<TRequest, TResponse> _rootHandler = null!;

public ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    => _rootHandler(request, cancellationToken);
```

```csharp
// Scoped: no cached field and no Init(...); resolved and composed on every call.
public ValueTask<TResponse> Handle(Mediator mediator, TRequest request, CancellationToken cancellationToken)
{
    var concreteHandler = mediator.Services.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
    var pipelineBehaviours = mediator.Services.GetServices<IPipelineBehavior<TRequest, TResponse>>();
    // ... composes the delegate chain, every call ...
}
```

The measured cost of that difference: 15.31 ns to 45.31 ns with no behaviors registered (2.96x),
and 39.61 ns to 93.75 ns with the validation + logging pipeline (2.37x), with allocation going
from 0 B to 64 B and 0 B to 256 B respectively.

Innovation resolves handlers and pipeline components from the container on every dispatch and does
not cache a composed chain, so its handlers, validators, interceptors and reactors can be
registered Scoped, Transient or Singleton. MediatR resolves per dispatch as well. That is a
description of how each library works, not a claim about which approach is preferable - the
tables above show what each approach costs in these scenarios.

### Choosing a configuration

Which row of the tables applies depends on the application:

- If handlers are stateless and need no Scoped dependencies, Mediator's Singleton rows are the
  relevant ones, and they are the fastest and lowest-allocating measurements in this document by a
  clear margin.
- If handlers constructor-inject a Scoped dependency such as an EF Core `DbContext`, the Singleton
  rows do not apply, and the comparison is between Innovation, MediatR, and Mediator's Scoped rows.
- Absolute magnitudes matter as much as ratios. The widest gap between any two rows in this
  document is roughly 117 ns per dispatch (`Command_MediatR_Loaded` at 157.08 ns against
  `Command_Mediator_Loaded` at 39.61 ns). Whether that is significant depends on how many dispatches per second an
  application sustains and what its handlers do; for a handler that touches a database or a
  network, it will be lost in the noise.

These libraries also solve overlapping rather than identical problems, and can be used together in
one codebase. Innovation provides an audit store hook, interceptors, reactors and startup
validation of handler registration; MediatR and Mediator provide a smaller, more focused surface
with pipeline behaviors as the extension point.

### Reproducing these results

From the repository root:

```powershell
cd test\Innovation.Benchmarks.Comparison
dotnet run -c Release
```

This runs `CommandComparisonTests`, `QueryComparisonTests`, `MessageComparisonTests`,
`LoadedCommandComparisonTests` and `LoadedCommandPerRequestComparisonTests`. Use `--filter` to run
a single class, for example
`dotnet run -c Release -- --filter *LoadedCommandComparisonTests*`.

The Mediator Scoped-lifetime rows live in a separate console project, for the compile-time
source-generator reason described in [Environment](#environment), and run the same way:

```powershell
cd test\Innovation.Benchmarks.Comparison.MediatorScoped
dotnet run -c Release
```

---

## Innovation's own pipeline benchmarks

These benchmarks measure Innovation on its own, with no other library involved. They come from the
`Innovation.Benchmarks` project. Their purpose is to make the cost of each pipeline feature visible
individually, so that the price of enabling one is known before you enable it.

They were run on the same machine and runtime as the comparison benchmarks above - see
[Environment](#environment) - using BenchmarkDotNet's default job. All benchmarks are IO-free.

### Unit reference

- 1 second = 1,000 ms
- 1 second = 1,000,000 us
- 1 second = 1,000,000,000 ns

### Dispatcher, no pipeline behaviors

| Method               | Mean     | Error    | StdDev   | Gen0   | Allocated |
|--------------------- |---------:|---------:|---------:|-------:|----------:|
| DispatchBlankCommand | 63.15 ns | 0.138 ns | 0.123 ns | 0.0023 |      24 B |
| DispatchBlankQuery   | 54.75 ns | 0.090 ns | 0.075 ns | 0.0023 |      24 B |

Operations per second: `1 000 000 000 / 63.15 = 15 835 313` for the command,
`1 000 000 000 / 54.75 = 18 264 840` for the query.

These rows, and every "Blank"/baseline row below, register no `IAuditStore`.
`DependencyBuilderBase`'s default constructor deliberately leaves the pipeline at its
zero-registration floor, so these represent the framework's lower bound rather than a typical
configuration. The cost of registering an audit store is measured separately below.

The 24 B allocated is the `CommandResult`/query result object returned by the handler, not
dispatcher overhead.

### Messages

| Method             | Mean      | Error    | StdDev   | Ratio        | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|-------------:|--------:|-------:|----------:|------------:|
| DispatchMessage    |  52.82 ns | 0.119 ns | 0.106 ns |     baseline |         | 0.0053 |      56 B |             |
| DispatchMessageFor | 269.46 ns | 1.234 ns | 1.094 ns | 5.10x slower |   0.02x | 0.0672 |     704 B | 12.57x more |

`DispatchMessageFor` resolves handlers for a runtime-supplied type rather than a compile-time
generic argument, which is why it costs more.

### Audit store

| Method                  | Mean      | Error    | StdDev   | Ratio        | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |----------:|---------:|---------:|-------------:|--------:|-------:|----------:|------------:|
| AuditStoreNotRegistered |  63.80 ns | 0.155 ns | 0.137 ns |     baseline |         | 0.0023 |      24 B |             |
| AuditStoreRegistered    | 144.06 ns | 0.419 ns | 0.350 ns | 2.26x slower |   0.01x | 0.0052 |      56 B | 2.33x more  |

Registering an `IAuditStore` adds ~80 ns and an extra allocation per dispatch: an `AuditContext`,
plus `SampleAuditStore`'s per-correlation-id `Dictionary`/`List<IEvent>` bookkeeping. Most
applications that want auditing will pay something in this range, and the exact figure depends
entirely on the audit store implementation - `SampleAuditStore` is an in-memory reference
implementation, not a tuned one.

### Pipeline features

| Method                            | Mean        | Error    | StdDev   | Ratio         | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |------------:|---------:|---------:|--------------:|--------:|-------:|----------:|------------:|
| Blank                             |    62.22 ns | 0.100 ns | 0.094 ns |      baseline |         | 0.0023 |      24 B |             |
| Interceptor                       |    93.28 ns | 0.165 ns | 0.155 ns |  1.50x slower |   0.00x | 0.0076 |      80 B |  3.33x more |
| ReactorAndResultReactor           |   338.56 ns | 1.627 ns | 1.359 ns |  5.44x slower |   0.02x | 0.0372 |     389 B | 16.21x more |
| DataAnnotationsAndCustomValidator | 1,363.15 ns | 6.073 ns | 5.681 ns | 21.91x slower |   0.09x | 0.1926 |    2032 B | 84.67x more |

Each row adds one pipeline feature to the `Blank` baseline. The reflection-based
`DataAnnotationsValidator` pass is by far the most expensive feature in the pipeline; it is opt-in
via `InnovationOptions.IsValidationEnabled`.

### Validation aggregation

| Method    | Mean     | Error     | StdDev    | Ratio        | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------- |---------:|----------:|----------:|-------------:|--------:|-------:|----------:|------------:|
| FailFast  | 1.826 us | 0.0128 us | 0.0120 us |     baseline |         | 0.3643 |   3.73 KB |             |
| Aggregate | 2.002 us | 0.0109 us | 0.0102 us | 1.10x slower |   0.01x | 0.3815 |   3.91 KB | 1.05x more  |

`InsertVendorCommand` fails both DataAnnotations and the custom `InsertVendorAddressValidator` in
this benchmark, so `Aggregate` genuinely has two error sets to merge. The ~176 ns difference is the
cost of that merge (`AggregateValidationErrors = true`) versus failing fast on the first validator
that reports an error, which is the default.

### Validators in isolation

These two benchmarks construct `DataAnnotationsValidator`/MiniValidation directly and never enter
the dispatcher, so they measure validation cost on its own rather than dispatch cost.

| Method                | Mean     | Error    | StdDev   | Gen0   | Allocated |
|---------------------- |---------:|---------:|---------:|-------:|----------:|
| BlankCommandNew       |  46.53 ns | 0.082 ns | 0.072 ns | 0.0023 |      24 B |
| InsertCustomerCommand | 775.5 ns  | 3.25 ns  | 3.04 ns  | 0.1268 |    1.3 KB |

Operations per second: `1 000 000 000 / 46.53 = 21 491 512` and
`1 000 000 000 / 775.5 = 1 289 490`.

### ValueStopwatch

| Method         | Mean     | Error    | StdDev   | Allocated |
|--------------- |---------:|---------:|---------:|----------:|
| StopWatchUsage | 30.50 ns | 0.090 ns | 0.084 ns |         - |

`ValueStopwatch` is the allocation-free timing primitive the dispatcher uses when an audit store is
registered.

### Reproducing the pipeline benchmarks

From the repository root:

```powershell
cd test\Innovation.Benchmarks
dotnet run -c Release
```

Use `--filter` to run a single class, for example
`dotnet run -c Release -- --filter *AuditStoreComparisonTests*`.
