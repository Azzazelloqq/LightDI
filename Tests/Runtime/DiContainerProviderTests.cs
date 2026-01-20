using System;
using LightDI.Runtime;
using LightDI.Tests;
using NUnit.Framework;

namespace LightDI.Tests
{
    /// <summary>
    /// Comprehensive tests for DiContainerProvider functionality.
    /// </summary>
    [TestFixture]
    public class DiContainerProviderTests
    {
        [SetUp]
        public void SetUp()
        {
            TestHelper.CleanupGlobalProvider();
        }

        [TearDown]
        public void TearDown()
        {
            TestHelper.CleanupGlobalProvider();
        }

        #region Container Registration Tests

        [Test]
        public void RegisterContainer_ShouldAddContainerToGlobalRegistry()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("test"));
            
            // Act & Assert - Should resolve from registered container
            #pragma warning disable CS0618 // Type or member is obsolete
            var resolved = DiContainerProvider.Resolve<ITestService>();
            #pragma warning restore CS0618
            
            Assert.IsNotNull(resolved);
            Assert.AreEqual("test", resolved.GetData());
            
            // Cleanup
            container.Dispose();
        }

        [Test]
        public void UnregisterContainer_ShouldRemoveContainerFromGlobalRegistry()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("test"));
            
            // Act
            container.Dispose(); // This should unregister the container
            
            // Assert
            #pragma warning disable CS0618 // Type or member is obsolete
            var exception = TestHelper.AssertThrows<Exception>(() => 
                DiContainerProvider.Resolve<ITestService>());
            #pragma warning restore CS0618
            
            Assert.That(exception.Message, Contains.Substring("not registered"));
        }

        #endregion

        #region Scope Resolution Tests

        /// <summary>
        /// Resolves a service using the most specific namespace scope.
        /// </summary>
        [Test]
        public void Resolve_WithNamespacePrefix_ShouldPreferMostSpecificScope()
        {
            // Arrange
            const string rootScope = "LightDI.Tests";
            const string leafScope = "LightDI.Tests.Sub";
            var rootContainer = TestHelper.CreateTestContainer(rootScope);
            var leafContainer = TestHelper.CreateTestContainer(leafScope);
            
            rootContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("root"));
            leafContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("leaf"));
            
            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            var resolved = DiContainerProvider.Resolve<ITestService>("LightDI.Tests.Sub.Component");
            #pragma warning restore CS0618
            
            // Assert
            Assert.AreEqual("leaf", resolved.GetData());
            
            // Cleanup
            rootContainer.Dispose();
            leafContainer.Dispose();
        }

        /// <summary>
        /// Resolves a service using an ambient namespace scope.
        /// </summary>
        [Test]
        public void Resolve_WithBeginScopeNamespace_ShouldRouteToScopedContainer()
        {
            // Arrange
            const string scope = "LightDI.Tests.Scoped";
            var container = TestHelper.CreateTestContainer(scope);
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("scoped"));
            
            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            using (DiContainerProvider.BeginScope(scope))
            {
                var resolved = DiContainerProvider.Resolve<ITestService>();
                Assert.AreEqual("scoped", resolved.GetData());
            }
            #pragma warning restore CS0618
            
            // Cleanup
            container.Dispose();
        }

        /// <summary>
        /// Resolves a service using an ambient object scope.
        /// </summary>
        [Test]
        public void Resolve_WithBeginScopeOwner_ShouldRouteToScopedContainer()
        {
            // Arrange
            var scopeOwner = new object();
            var container = TestHelper.CreateTestContainer(scopeOwner);
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("owner-scope"));
            
            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            using (DiContainerProvider.BeginScope(scopeOwner))
            {
                var resolved = DiContainerProvider.Resolve<ITestService>();
                Assert.AreEqual("owner-scope", resolved.GetData());
            }
            #pragma warning restore CS0618
            
            // Cleanup
            container.Dispose();
        }

        /// <summary>
        /// Ensures object scope resolution uses reference identity instead of Equals.
        /// </summary>
        [Test]
        public void Resolve_WithObjectScope_ShouldUseReferenceIdentity()
        {
            // Arrange
            var scopeOwner = new AlwaysEqualScopeOwner();
            var otherOwner = new AlwaysEqualScopeOwner();
            var container = TestHelper.CreateTestContainer(scopeOwner);
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("owner"));
            
            // Act & Assert
            #pragma warning disable CS0618 // Type or member is obsolete
            var exception = TestHelper.AssertThrows<Exception>(() => 
                DiContainerProvider.Resolve<ITestService>(otherOwner));
            #pragma warning restore CS0618
            
            Assert.That(exception.Message, Contains.Substring("No container registered"));
            
            // Cleanup
            container.Dispose();
        }

        /// <summary>
        /// Falls back to a single container even when the namespace scope is missing.
        /// </summary>
        [Test]
        public void Resolve_WithMissingNamespaceScopeAndSingleContainer_ShouldFallback()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("fallback"));
            
            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            var resolved = DiContainerProvider.Resolve<ITestService>("Missing.Scope");
            #pragma warning restore CS0618
            
            // Assert
            Assert.AreEqual("fallback", resolved.GetData());
            
            // Cleanup
            container.Dispose();
        }

        /// <summary>
        /// Throws when namespace scope is missing and multiple containers exist.
        /// </summary>
        [Test]
        public void Resolve_WithMissingNamespaceScopeAndMultipleContainers_ShouldThrow()
        {
            // Arrange
            var container1 = TestHelper.CreateTestContainer();
            var container2 = TestHelper.CreateTestContainer();
            container1.RegisterAsSingletonLazy<ITestService>(() => new TestService("first"));
            
            // Act & Assert
            #pragma warning disable CS0618 // Type or member is obsolete
            var exception = TestHelper.AssertThrows<Exception>(() => 
                DiContainerProvider.Resolve<ITestService>("Missing.Scope"));
            #pragma warning restore CS0618
            
            Assert.That(exception.Message, Contains.Substring("No container registered"));
            
            // Cleanup
            container1.Dispose();
            container2.Dispose();
        }

        /// <summary>
        /// Prevents disposing scopes out of order.
        /// </summary>
        [Test]
        public void BeginScope_DisposedOutOfOrder_ShouldThrow()
        {
            // Arrange
            var firstScope = DiContainerProvider.BeginScope("First.Scope");
            var secondScope = DiContainerProvider.BeginScope("Second.Scope");
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => firstScope.Dispose());
            
            // Cleanup
            secondScope.Dispose();
            firstScope.Dispose();
        }

        #endregion

        #region Multiple Container Tests

        [Test]
        public void Resolve_WithMultipleContainers_ShouldResolveFromScopedContainers()
        {
            // Arrange
            const string scope1 = "LightDI.Tests.Container1";
            const string scope2 = "LightDI.Tests.Container2";
            var container1 = TestHelper.CreateTestContainer(scope1);
            var container2 = TestHelper.CreateTestContainer(scope2);
            
            container1.RegisterAsSingletonLazy<ITestService>(() => new TestService("container1"));
            container2.RegisterAsSingletonLazy<ITestService>(() => new TestService("container2"));
            container2.RegisterAsSingletonLazy<TransientTestService>(() => new TransientTestService());
            
            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            var testService = DiContainerProvider.Resolve<ITestService>(scope1);
            var transientService = DiContainerProvider.Resolve<TransientTestService>(scope2);
            #pragma warning restore CS0618
            
            // Assert
            Assert.AreEqual("container1", testService.GetData());
            Assert.IsNotNull(transientService);
            
            // Cleanup
            container1.Dispose();
            container2.Dispose();
        }

        [Test]
        public void Resolve_WithPartialContainerDisposal_ShouldResolveFromRemainingContainers()
        {
            // Arrange
            var container1 = TestHelper.CreateTestContainer();
            var container2 = TestHelper.CreateTestContainer();
            
            container1.RegisterAsSingletonLazy<ITestService>(() => new TestService("container1"));
            container2.RegisterAsSingletonLazy<ITestService>(() => new TestService("container2"));
            
            // Act
            container1.Dispose(); // Remove first container
            
            #pragma warning disable CS0618 // Type or member is obsolete
            var testService = DiContainerProvider.Resolve<ITestService>();
            #pragma warning restore CS0618
            
            // Assert
            Assert.AreEqual("container2", testService.GetData());
            
            // Cleanup
            container2.Dispose();
        }

        #endregion

        #region Resolution Error Tests

        [Test]
        public void Resolve_NoRegisteredContainers_ShouldThrowException()
        {
            // Act & Assert
            #pragma warning disable CS0618 // Type or member is obsolete
            var exception = TestHelper.AssertThrows<Exception>(() => 
                DiContainerProvider.Resolve<ITestService>());
            #pragma warning restore CS0618
            
            Assert.That(exception.Message, Contains.Substring("not registered"));
        }

        [Test]
        public void Resolve_NoContainerCanResolveService_ShouldThrowException()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            container.RegisterAsSingletonLazy<TransientTestService>(() => new TransientTestService());
            
            // Act & Assert
            #pragma warning disable CS0618 // Type or member is obsolete
            var exception = TestHelper.AssertThrows<Exception>(() => 
                DiContainerProvider.Resolve<ITestService>());
            #pragma warning restore CS0618
            
            Assert.That(exception.Message, Contains.Substring("not registered"));
            
            // Cleanup
            container.Dispose();
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void Dispose_ShouldClearAllRegisteredContainers()
        {
            // Arrange
            var container1 = TestHelper.CreateTestContainer();
            var container2 = TestHelper.CreateTestContainer();
            
            container1.RegisterAsSingletonLazy<ITestService>(() => new TestService("container1"));
            container2.RegisterAsSingletonLazy<ITestService>(() => new TestService("container2"));
            
            // Act
            DiContainerProvider.Dispose();
            
            // Assert
            #pragma warning disable CS0618 // Type or member is obsolete
            var exception = TestHelper.AssertThrows<Exception>(() => 
                DiContainerProvider.Resolve<ITestService>());
            #pragma warning restore CS0618
            
            Assert.That(exception.Message, Contains.Substring("not registered"));
            
            // Note: Containers themselves are still valid, just not registered globally
            var directResolve = TestHelper.ResolveFromContainer<ITestService>(container1);
            Assert.IsNotNull(directResolve);
            
            // Cleanup
            container1.Dispose();
            container2.Dispose();
        }

        [Test]
        public void Dispose_CalledMultipleTimes_ShouldNotThrowException()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                DiContainerProvider.Dispose();
                DiContainerProvider.Dispose();
                DiContainerProvider.Dispose();
            });
            
            // Cleanup
            container.Dispose();
        }

        #endregion

        #region Container Lifecycle Tests

        [Test]
        public void ContainerFactory_ShouldAutomaticallyRegisterContainer()
        {
            // Act
            var container = DiContainerFactory.CreateContainer();
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("auto-registered"));
            
            // Assert
            #pragma warning disable CS0618 // Type or member is obsolete
            var resolved = DiContainerProvider.Resolve<ITestService>();
            #pragma warning restore CS0618
            
            Assert.AreEqual("auto-registered", resolved.GetData());
            
            // Cleanup
            container.Dispose();
        }

        [Test]
        public void ContainerDispose_ShouldAutomaticallyUnregisterContainer()
        {
            // Arrange
            var container = DiContainerFactory.CreateContainer();
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("test"));
            
            // Verify it's registered
            #pragma warning disable CS0618 // Type or member is obsolete
            var beforeDispose = DiContainerProvider.Resolve<ITestService>();
            #pragma warning restore CS0618
            Assert.IsNotNull(beforeDispose);
            
            // Act
            container.Dispose();
            
            // Assert
            #pragma warning disable CS0618 // Type or member is obsolete
            var exception = TestHelper.AssertThrows<Exception>(() => 
                DiContainerProvider.Resolve<ITestService>());
            #pragma warning restore CS0618
            
            Assert.That(exception.Message, Contains.Substring("not registered"));
        }

        #endregion

        #region Complex Resolution Scenarios

        [Test]
        public void Resolve_ComplexDependencyChain_ShouldResolveFromMultipleContainers()
        {
            // Arrange
            const string baseScope = "LightDI.Tests.Base";
            const string dependencyScope = "LightDI.Tests.Dependency";
            var baseContainer = TestHelper.CreateTestContainer(baseScope);
            var dependencyContainer = TestHelper.CreateTestContainer(dependencyScope);
            
            baseContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("base"));
            dependencyContainer.RegisterAsSingletonLazy<IDependentService>(() => 
                new DependentService(DiContainerProvider.Resolve<ITestService>(baseScope)));
            
            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            var dependentService = DiContainerProvider.Resolve<IDependentService>(dependencyScope);
            #pragma warning restore CS0618
            
            // Assert
            Assert.IsNotNull(dependentService);
            Assert.IsNotNull(dependentService.TestService);
            Assert.AreEqual("base", dependentService.TestService.GetData());
            Assert.AreEqual("Processed: base", dependentService.ProcessData());
            
            // Cleanup
            baseContainer.Dispose();
            dependencyContainer.Dispose();
        }

        [Test]
        public void Resolve_WithMultipleContainersWithoutScope_ShouldThrow()
        {
            // Arrange
            var highPriorityContainer = TestHelper.CreateTestContainer();
            var lowPriorityContainer = TestHelper.CreateTestContainer();
            
            // Register in specific order
            highPriorityContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("high-priority"));
            lowPriorityContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("low-priority"));
            
            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            var exception = TestHelper.AssertThrows<Exception>(() => 
                DiContainerProvider.Resolve<ITestService>());
            #pragma warning restore CS0618
            
            // Assert
            Assert.That(exception.Message, Contains.Substring("no scope is set"));
            
            // Cleanup
            highPriorityContainer.Dispose();
            lowPriorityContainer.Dispose();
        }

        #endregion

        private sealed class AlwaysEqualScopeOwner
        {
            public override bool Equals(object obj)
            {
                return true;
            }

            public override int GetHashCode()
            {
                return 1;
            }
        }
    }
}