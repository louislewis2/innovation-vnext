# Contributing to Innovation.vNext

Thanks for taking an interest. Issues, discussions and pull requests are all welcome.

Innovation.vNext is alpha software and the API surface is still moving, so if you are planning
anything substantial, it is worth opening an issue first so we can agree on the shape before you
spend time on it.

## Prerequisites

- Visual Studio 2026 (>= 18.8.1), or any editor with the .NET SDK
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

All projects target `net10.0`. There is currently no down-level or .NET Standard target.

## Building

```bash
dotnet build Innovation.vNext.sln -c Release
```

## Running the tests

The primary suite is `test/Innovation.ServiceBus.InProcess.Tests` (MSTest).

```bash
dotnet test
```

Two supporting projects exist purely to give the tests and benchmarks something realistic to
dispatch, and are worth knowing about before you add a test:

- `test/Innovation.ApiSample` - command, query and message definitions
- `test/Innovation.SampleApi.Consumer` - handlers, validators, interceptors, reactors and a sample
  audit store

## Running the benchmarks

Benchmarks use [BenchmarkDotNet](https://benchmarkdotnet.org/) and must be run in `Release`.

Innovation's own pipeline benchmarks:

```bash
cd test/Innovation.Benchmarks
dotnet run -c Release
```

Comparison benchmarks against MediatR and Mediator:

```bash
cd test/Innovation.Benchmarks.Comparison
dotnet run -c Release
```

Mediator's `Scoped`-lifetime rows live in a separate project, because Mediator's `ServiceLifetime`
is a compile-time, assembly-wide source-generator setting and cannot share an assembly with its
`Singleton` rows:

```bash
cd test/Innovation.Benchmarks.Comparison.MediatorScoped
dotnet run -c Release
```

Use `--filter` to run a single class, for example:

```bash
dotnet run -c Release -- --filter *AuditStoreComparisonTests*
```

## Running the DI lifetime demo

`test/Innovation.CaptiveDependency.Demo` backs the claims made in `BENCHMARKS.md` about what happens
when a Singleton constructor-injects a Scoped service. It references nothing but
`Microsoft.Extensions.DependencyInjection`, verifies its own expectations, and returns a non-zero
exit code if any of them do not hold.

```bash
cd test/Innovation.CaptiveDependency.Demo
dotnet run -c Release
```

## Changes that affect the dispatch path

`src/Innovation.ServiceBus.InProcess.vNext/Dispatching/Dispatcher.cs` is the hot path. If you change
it, please run the benchmarks before and after and include both sets of numbers in the pull request.
Benchmark results are hardware-dependent, so a before/after pair from the same machine is far more
useful than absolute figures.

If your change moves any number that appears in
[BENCHMARKS.md](BENCHMARKS.md) or [README.md](README.md), please update those documents in the same
pull request.

## Corrections to the benchmarks

If you believe a benchmark is unfair, misconfigured, or that this repository has stated something
inaccurate about another library, please open an issue. `BENCHMARKS.md` is intended to be
reproducible and factually correct rather than flattering, and corrections are genuinely
appreciated - particularly from maintainers of the libraries measured there.

## Pull requests

- Keep changes focused; unrelated refactoring is harder to review
- Match the existing code style rather than introducing a new one
- Add or update tests for behavior changes
- Make sure `dotnet build` and `dotnet test` both pass before opening the PR

## License

By contributing, you agree that your contributions will be licensed under the
[MIT License](LICENSE).
