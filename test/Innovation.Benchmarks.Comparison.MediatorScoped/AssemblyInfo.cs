using Microsoft.Extensions.DependencyInjection;

// ServiceLifetime is a compile-time, assembly-wide switch for the Mediator source generator (see
// MediatorOptionsAttribute) - it cannot be mixed per-handler within one assembly, which is why this
// Scoped-lifetime comparison lives in its own project rather than alongside MediatorLib (which uses
// the default Singleton lifetime). See BENCHMARKS.md, "Confirming what Scoped mode actually
// generates", for what this actually changes in the generated dispatch code.
[assembly: Mediator.MediatorOptions(ServiceLifetime = ServiceLifetime.Scoped)]
