using System;
using Microsoft.Extensions.DependencyInjection;

namespace Innovation.CaptiveDependency.Demo
{
    /// <summary>
    /// A self-contained, dependency-free demonstration of the two Microsoft.Extensions.DependencyInjection
    /// behaviours referenced in BENCHMARKS.md, under "Handler lifetime and what it costs".
    ///
    /// Nothing here references Innovation, MediatR or Mediator. The behaviour being demonstrated is a
    /// property of the DI container itself, not of any mediator library. It is included in this
    /// repository so that the claims made in BENCHMARKS.md can be verified by running this project
    /// rather than taken on trust.
    ///
    /// The program verifies its own expectations and returns a non-zero exit code if any of them
    /// do not hold.
    /// </summary>
    public static class Program
    {
        public static int Main()
        {
            var failures = 0;

            failures += ValidateOnBuildThrowsAtStartup() ? 0 : 1;
            failures += ValidateScopesThrowsOnFirstResolve() ? 0 : 1;
            failures += WithoutValidationTheScopedServiceIsHeldCaptive() ? 0 : 1;

            Console.WriteLine();

            if (failures == 0)
            {
                Console.WriteLine("All expectations held.");
                return 0;
            }

            Console.WriteLine($"{failures} expectation(s) did not hold.");
            return 1;
        }

        /// <summary>
        /// ValidateOnBuild (enabled by the ASP.NET Core host in the Development environment) surfaces
        /// the problem when the provider is built, before a single request is served.
        /// </summary>
        private static bool ValidateOnBuildThrowsAtStartup()
        {
            Header("1. ValidateOnBuild = true - fails when the provider is built");

            try
            {
                using var provider = BuildProvider(validateScopes: true, validateOnBuild: true);

                Console.WriteLine("  UNEXPECTED: the provider was built without error.");
                return false;
            }
            catch (AggregateException ex)
            {
                Console.WriteLine("  BuildServiceProvider threw AggregateException. Inner message:");
                Console.WriteLine();
                Console.WriteLine("    " + ex.InnerException?.Message);
                return true;
            }
        }

        /// <summary>
        /// ValidateScopes on its own does not check anything up front; it throws the first time the
        /// offending service is actually resolved.
        /// </summary>
        private static bool ValidateScopesThrowsOnFirstResolve()
        {
            Header("2. ValidateScopes = true, ValidateOnBuild = false - fails on first resolve");

            using var provider = BuildProvider(validateScopes: true, validateOnBuild: false);

            Console.WriteLine("  Provider built without error.");

            try
            {
                using var scope = provider.CreateScope();
                scope.ServiceProvider.GetRequiredService<SingletonConsumer>();

                Console.WriteLine("  UNEXPECTED: the singleton resolved without error.");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("  Resolving the singleton threw InvalidOperationException:");
                Console.WriteLine();
                Console.WriteLine("    " + ex.Message);
                return true;
            }
        }

        /// <summary>
        /// With scope validation disabled, nothing throws. The singleton is resolved from the root
        /// provider, so its Scoped dependency is created in the root scope and kept for the lifetime
        /// of the application - the "captive dependency" case. Every scope that uses the singleton
        /// shares that one instance, and it is not the instance belonging to that scope.
        /// </summary>
        private static bool WithoutValidationTheScopedServiceIsHeldCaptive()
        {
            Header("3. Scope validation disabled - the scoped service is held captive");

            using var provider = BuildProvider(validateScopes: false, validateOnBuild: false);

            ScopedService directFromFirstScope;
            ScopedService heldBySingletonInFirstScope;

            using (var firstScope = provider.CreateScope())
            {
                directFromFirstScope = firstScope.ServiceProvider.GetRequiredService<ScopedService>();
                heldBySingletonInFirstScope = firstScope.ServiceProvider
                    .GetRequiredService<SingletonConsumer>().Dependency;
            }

            ScopedService directFromSecondScope;
            ScopedService heldBySingletonInSecondScope;

            using (var secondScope = provider.CreateScope())
            {
                directFromSecondScope = secondScope.ServiceProvider.GetRequiredService<ScopedService>();
                heldBySingletonInSecondScope = secondScope.ServiceProvider
                    .GetRequiredService<SingletonConsumer>().Dependency;
            }

            Console.WriteLine("  Resolved directly from each scope:");
            Console.WriteLine($"    scope 1 -> ScopedService #{directFromFirstScope.InstanceId}");
            Console.WriteLine($"    scope 2 -> ScopedService #{directFromSecondScope.InstanceId}");
            Console.WriteLine();
            Console.WriteLine("  Held by the singleton, resolved from each scope:");
            Console.WriteLine($"    scope 1 -> ScopedService #{heldBySingletonInFirstScope.InstanceId}");
            Console.WriteLine($"    scope 2 -> ScopedService #{heldBySingletonInSecondScope.InstanceId}");
            Console.WriteLine();

            var scopesProduceDifferentInstances =
                !ReferenceEquals(directFromFirstScope, directFromSecondScope);

            var singletonHeldOneInstanceThroughout =
                ReferenceEquals(heldBySingletonInFirstScope, heldBySingletonInSecondScope);

            // A Singleton is always resolved from the root provider, so its Scoped dependency is
            // created in the root scope - it is not the instance belonging to whichever scope asked
            // for it. That instance lives until the provider is disposed, and is never disposed
            // alongside any request scope.
            var singletonHeldNeitherScopesInstance =
                !ReferenceEquals(heldBySingletonInFirstScope, directFromFirstScope)
                && !ReferenceEquals(heldBySingletonInFirstScope, directFromSecondScope);

            Report("the two scopes produce different ScopedService instances", scopesProduceDifferentInstances);
            Report("the singleton holds one ScopedService instance across both scopes", singletonHeldOneInstanceThroughout);
            Report("the instance it holds belongs to neither scope - it was created in the root scope", singletonHeldNeitherScopesInstance);

            return scopesProduceDifferentInstances
                && singletonHeldOneInstanceThroughout
                && singletonHeldNeitherScopesInstance;
        }

        private static ServiceProvider BuildProvider(bool validateScopes, bool validateOnBuild)
        {
            var services = new ServiceCollection();

            services.AddScoped<ScopedService>();
            services.AddSingleton<SingletonConsumer>();

            return services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = validateScopes,
                ValidateOnBuild = validateOnBuild
            });
        }

        private static void Header(string title)
        {
            Console.WriteLine();
            Console.WriteLine(title);
            Console.WriteLine(new string('-', title.Length));
        }

        private static void Report(string expectation, bool held)
        {
            Console.WriteLine($"  [{(held ? "ok" : "FAILED")}] {expectation}");
        }
    }

    /// <summary>
    /// Stands in for anything an application would normally register as Scoped - most commonly an
    /// EF Core DbContext, whose correctness depends on one instance per unit of work.
    /// </summary>
    public sealed class ScopedService
    {
        private static int counter;

        public ScopedService()
        {
            this.InstanceId = ++counter;
        }

        public int InstanceId { get; }
    }

    /// <summary>
    /// Stands in for a handler (or pipeline behavior) registered as a Singleton that
    /// constructor-injects a Scoped dependency.
    /// </summary>
    public sealed class SingletonConsumer
    {
        public SingletonConsumer(ScopedService dependency)
        {
            this.Dependency = dependency;
        }

        public ScopedService Dependency { get; }
    }
}
