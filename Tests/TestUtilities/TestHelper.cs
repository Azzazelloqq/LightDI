using System;
using LightDI.Runtime;

namespace LightDI.Tests
{
    /// <summary>
    /// Helper methods for testing LightDI functionality.
    /// </summary>
    public static class TestHelper
    {
        /// <summary>
        /// Creates a clean container for testing purposes.
        /// </summary>
        /// <param name="disposeRegistered">Whether to dispose registered services when container is disposed.</param>
        /// <returns>A new container instance.</returns>
        public static IDiContainer CreateTestContainer(bool disposeRegistered = true)
        {
            return DiContainerFactory.CreateGlobalContainer(disposeRegistered);
        }

        /// <summary>
        /// Creates a clean local container for testing purposes.
        /// </summary>
        /// <param name="disposeRegistered">Whether to dispose registered services when container is disposed.</param>
        /// <returns>A new container instance.</returns>
        public static IDiContainer CreateLocalTestContainer(bool disposeRegistered = true)
        {
            return DiContainerFactory.CreateLocalContainer(disposeRegistered);
        }

        /// <summary>
        /// Cleans up the global container provider for testing isolation.
        /// </summary>
        public static void CleanupGlobalProvider()
        {
            DiContainerProvider.Dispose();
        }

        /// <summary>
        /// Registers standard test services in the container.
        /// </summary>
        /// <param name="container">The container to register services in.</param>
        public static void RegisterStandardTestServices(IDiContainer container)
        {
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("singleton-service"));
            container.RegisterAsTransient<TransientTestService>(() => new TransientTestService());
            container.RegisterAsSingletonLazy<SingletonTestService>(() => new SingletonTestService());
        }

        /// <summary>
        /// Registers services for dependency injection testing.
        /// </summary>
        /// <param name="container">The container to register services in.</param>
        public static void RegisterDependencyTestServices(IDiContainer container)
        {
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("dependency-test"));
            container.RegisterAsTransient<IDependentService>(() => new DependentService(DiContainerProvider.Resolve<ITestService>()));
            container.RegisterAsTransient<IComplexService>(() => new ComplexService(
                DiContainerProvider.Resolve<ITestService>(),
                DiContainerProvider.Resolve<IDependentService>()));
        }

        /// <summary>
        /// Registers disposable services for disposal testing.
        /// </summary>
        /// <param name="container">The container to register services in.</param>
        public static void RegisterDisposableTestServices(IDiContainer container)
        {
            container.RegisterAsSingletonLazy<ITestService>(() => new DisposableTestService("disposable-singleton"));
            container.RegisterAsTransient<DisposableTestService>(() => new DisposableTestService("disposable-transient"));
        }

        /// <summary>
        /// Asserts that an action throws an exception of the specified type.
        /// </summary>
        /// <typeparam name="TException">The expected exception type.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <param name="expectedMessage">The expected exception message (optional).</param>
        /// <returns>The thrown exception.</returns>
        public static TException AssertThrows<TException>(Action action, string expectedMessage = null) 
            where TException : Exception
        {
            try
            {
                action();
                throw new AssertionException($"Expected {typeof(TException).Name} to be thrown, but no exception was thrown.");
            }
            catch (TException ex)
            {
                if (expectedMessage != null && !ex.Message.Contains(expectedMessage))
                {
                    throw new AssertionException($"Expected exception message to contain '{expectedMessage}', but was '{ex.Message}'.");
                }
                return ex;
            }
            catch (System.Reflection.TargetInvocationException tex) when (tex.InnerException is TException innerEx)
            {
                // Handle reflection-wrapped exceptions
                if (expectedMessage != null && !innerEx.Message.Contains(expectedMessage))
                {
                    throw new AssertionException($"Expected exception message to contain '{expectedMessage}', but was '{innerEx.Message}'.");
                }
                return innerEx;
            }
            catch (Exception ex)
            {
                // Check if it's a wrapped exception in TargetInvocationException
                if (ex is System.Reflection.TargetInvocationException tex && tex.InnerException != null)
                {
                    throw new AssertionException($"Expected {typeof(TException).Name} to be thrown, but {tex.InnerException.GetType().Name} was thrown instead: {tex.InnerException.Message}");
                }
                throw new AssertionException($"Expected {typeof(TException).Name} to be thrown, but {ex.GetType().Name} was thrown instead: {ex.Message}");
            }
        }

        /// <summary>
        /// Resolves a service using internal container methods for testing.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <param name="container">The container to resolve from.</param>
        /// <returns>The resolved service instance.</returns>
        public static T ResolveFromContainer<T>(IDiContainer container) where T : class
        {
            // Use reflection to call internal Resolve method for testing
            var resolveMethod = typeof(IDiContainer).GetMethod("Resolve", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, 
                null, new Type[] { }, null);
            
            if (resolveMethod == null)
                throw new InvalidOperationException("Could not find internal Resolve method on IDiContainer");
                
            var genericMethod = resolveMethod.MakeGenericMethod(typeof(T));
            
            try
            {
                return (T)genericMethod.Invoke(container, null);
            }
            catch (System.Reflection.TargetInvocationException tex)
            {
                // Unwrap the inner exception from reflection call
                throw tex.InnerException;
            }
        }

        /// <summary>
        /// Tries to resolve a service using internal container methods for testing.
        /// </summary>
        /// <typeparam name="T">The service type to resolve.</typeparam>
        /// <param name="container">The container to resolve from.</param>
        /// <param name="instance">The resolved instance if successful.</param>
        /// <returns>True if resolution was successful.</returns>
        public static bool TryResolveFromContainer<T>(IDiContainer container, out T instance) where T : class
        {
            // Use reflection to call internal TryResolve method for testing
            var tryResolveMethod = typeof(IDiContainer).GetMethod("TryResolve", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (tryResolveMethod == null)
            {
                instance = null;
                return false;
            }
            
            var genericMethod = tryResolveMethod.MakeGenericMethod(typeof(T));
            
            try
            {
                var parameters = new object[1] { null };
                var result = (bool)genericMethod.Invoke(container, parameters);
                instance = (T)parameters[0];
                return result;
            }
            catch (System.Reflection.TargetInvocationException tex)
            {
                // If internal TryResolve throws an exception, unwrap and rethrow it
                // This happens when factory throws or when there's a type mismatch
                if (tex.InnerException != null)
                    throw tex.InnerException;
                throw;
            }
        }

        /// <summary>
        /// Custom assertion exception for test helpers.
        /// </summary>
        public class AssertionException : Exception
        {
            public AssertionException(string message) : base(message) { }
        }
    }
}