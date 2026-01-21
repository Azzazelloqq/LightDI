using System;
using System.Text.RegularExpressions;
using LightDI.Runtime;
using LightDI.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

        #region Local/Global Resolution Tests

        /// <summary>
        /// Resolves from the local container before falling back to global containers.
        /// </summary>
        [Test]
        public void Resolve_WithLocalContainer_ShouldPreferLocal()
        {
            // Arrange
            var localContainer = TestHelper.CreateLocalTestContainer();
            var globalContainer = TestHelper.CreateTestContainer();
            
            localContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("local"));
            globalContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("global"));
            
            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            var resolved = DiContainerProvider.Resolve<ITestService>();
            #pragma warning restore CS0618
            
            // Assert
            Assert.AreEqual("local", resolved.GetData());
            
            // Cleanup
            localContainer.Dispose();
            globalContainer.Dispose();
        }

        /// <summary>
        /// Falls back to global containers when the local container cannot resolve.
        /// </summary>
        [Test]
        public void Resolve_WithLocalContainerMissingService_ShouldFallbackToGlobal()
        {
            // Arrange
            var localContainer = TestHelper.CreateLocalTestContainer();
            var globalContainer = TestHelper.CreateTestContainer();
            globalContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("global"));
            
            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            var resolved = DiContainerProvider.Resolve<ITestService>();
            #pragma warning restore CS0618
            
            // Assert
            Assert.AreEqual("global", resolved.GetData());
            
            // Cleanup
            localContainer.Dispose();
            globalContainer.Dispose();
        }

        /// <summary>
        /// Uses global containers when the supplied assembly has no local container.
        /// </summary>
        [Test]
        public void Resolve_WithAssemblyParameter_ShouldFallbackToGlobal()
        {
            // Arrange
            var localContainer = TestHelper.CreateLocalTestContainer();
            var globalContainer = TestHelper.CreateTestContainer();
            localContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("local"));
            globalContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("global"));
            
            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            var resolved = DiContainerProvider.Resolve<ITestService>(typeof(string).Assembly);
            #pragma warning restore CS0618
            
            // Assert
            Assert.AreEqual("global", resolved.GetData());
            
            // Cleanup
            localContainer.Dispose();
            globalContainer.Dispose();
        }

        #endregion

        #region Multiple Container Tests

        [Test]
        public void Resolve_WithMultipleGlobalContainers_ShouldResolveFromRegisteredOrder()
        {
            // Arrange
            var previousSetting = DiContainerProvider.AllowMultipleGlobalContainers;
            DiContainerProvider.AllowMultipleGlobalContainers = true;
            var container1 = TestHelper.CreateTestContainer();
            var container2 = TestHelper.CreateTestContainer();
            
            container1.RegisterAsSingletonLazy<ITestService>(() => new TestService("container1"));
            container2.RegisterAsSingletonLazy<ITestService>(() => new TestService("container2"));
            container2.RegisterAsSingletonLazy<TransientTestService>(() => new TransientTestService());
            
            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            var testService = DiContainerProvider.Resolve<ITestService>();
            var transientService = DiContainerProvider.Resolve<TransientTestService>();
            #pragma warning restore CS0618
            
            // Assert
            Assert.AreEqual("container1", testService.GetData());
            Assert.IsNotNull(transientService);
            
            // Cleanup
            container1.Dispose();
            container2.Dispose();
            DiContainerProvider.AllowMultipleGlobalContainers = previousSetting;
        }

        [Test]
        public void Resolve_WithPartialContainerDisposal_ShouldResolveFromRemainingContainers()
        {
            // Arrange
            var previousSetting = DiContainerProvider.AllowMultipleGlobalContainers;
            DiContainerProvider.AllowMultipleGlobalContainers = true;
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
            DiContainerProvider.AllowMultipleGlobalContainers = previousSetting;
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
            var previousSetting = DiContainerProvider.AllowMultipleGlobalContainers;
            DiContainerProvider.AllowMultipleGlobalContainers = true;
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
            DiContainerProvider.AllowMultipleGlobalContainers = previousSetting;
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
            var baseContainer = TestHelper.CreateTestContainer();
            var dependencyContainer = TestHelper.CreateLocalTestContainer();
            
            baseContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("base"));
            dependencyContainer.RegisterAsSingletonLazy<IDependentService>(() => 
                new DependentService(DiContainerProvider.Resolve<ITestService>()));
            
            // Act
            #pragma warning disable CS0618 // Type or member is obsolete
            var dependentService = DiContainerProvider.Resolve<IDependentService>();
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
        public void Register_MultipleGlobalContainers_ShouldLogWarningWhenNotAllowed()
        {
            // Arrange
            var previousSetting = DiContainerProvider.AllowMultipleGlobalContainers;
            DiContainerProvider.AllowMultipleGlobalContainers = false;
            LogAssert.Expect(LogType.Warning, new Regex("Multiple global containers"));

            // Act
            var container1 = TestHelper.CreateTestContainer();
            var container2 = TestHelper.CreateTestContainer();

            // Cleanup
            container1.Dispose();
            container2.Dispose();
            DiContainerProvider.AllowMultipleGlobalContainers = previousSetting;
        }

        #endregion

    }
}