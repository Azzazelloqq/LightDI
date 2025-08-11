using System;
using LightDI.Runtime;
using NUnit.Framework;

namespace LightDI.Tests
{
    /// <summary>
    /// Comprehensive tests for DiContainerFactory functionality.
    /// </summary>
    [TestFixture]
    public class DiContainerFactoryTests
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

        #region Container Creation Tests

        [Test]
        public void CreateContainer_ShouldReturnValidContainer()
        {
            // Act
            var container = DiContainerFactory.CreateContainer();
            
            // Assert
            Assert.IsNotNull(container);
            Assert.IsInstanceOf<IDiContainer>(container);
            
            // Cleanup
            container.Dispose();
        }

        [Test]
        public void CreateContainer_WithDisposeRegisteredTrue_ShouldCreateContainerThatDisposesServices()
        {
            // Arrange
            var disposableService = new DisposableTestService("test");
            
            // Act
            var container = DiContainerFactory.CreateContainer(true);
            container.RegisterAsSingleton<DisposableTestService>(disposableService);
            
            // Force resolution to add to disposables list
            TestHelper.ResolveFromContainer<DisposableTestService>(container);
            
            container.Dispose();
            
            // Assert
            Assert.IsTrue(disposableService.IsDisposed);
        }

        [Test]
        public void CreateContainer_WithDisposeRegisteredFalse_ShouldCreateContainerThatDoesNotDisposeServices()
        {
            // Arrange
            var disposableService = new DisposableTestService("test");
            
            // Act
            var container = DiContainerFactory.CreateContainer(false);
            container.RegisterAsSingleton<DisposableTestService>(disposableService);
            
            // Force resolution to potentially add to disposables list
            TestHelper.ResolveFromContainer<DisposableTestService>(container);
            
            container.Dispose();
            
            // Assert
            Assert.IsFalse(disposableService.IsDisposed);
            
            // Manual cleanup
            disposableService.Dispose();
        }

        #endregion

        #region Global Registration Tests

        [Test]
        public void CreateContainer_ShouldAutomaticallyRegisterWithGlobalProvider()
        {
            // Act
            var container = DiContainerFactory.CreateContainer();
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("factory-test"));
            
            // Assert - Should be able to resolve via global provider
            #pragma warning disable CS0618 // Type or member is obsolete
            var resolved = DiContainerProvider.Resolve<ITestService>();
            #pragma warning restore CS0618
            
            Assert.IsNotNull(resolved);
            Assert.AreEqual("factory-test", resolved.GetData());
            
            // Cleanup
            container.Dispose();
        }

        [Test]
        public void CreateContainer_MultipleContainers_ShouldRegisterAllWithGlobalProvider()
        {
            // Act
            var container1 = DiContainerFactory.CreateContainer();
            var container2 = DiContainerFactory.CreateContainer();
            
            container1.RegisterAsSingletonLazy<ITestService>(() => new TestService("container1"));
            container2.RegisterAsSingletonLazy<TransientTestService>(() => new TransientTestService());
            
            // Assert
            #pragma warning disable CS0618 // Type or member is obsolete
            var testService = DiContainerProvider.Resolve<ITestService>();
            var transientService = DiContainerProvider.Resolve<TransientTestService>();
            #pragma warning restore CS0618
            
            Assert.AreEqual("container1", testService.GetData());
            Assert.IsNotNull(transientService);
            
            // Cleanup
            container1.Dispose();
            container2.Dispose();
        }

        #endregion

        #region Dispose Callback Tests

        [Test]
        public void CreateContainer_WhenDisposed_ShouldUnregisterFromGlobalProvider()
        {
            // Arrange
            var container = DiContainerFactory.CreateContainer();
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("test"));
            
            // Verify registration
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

        [Test]
        public void CreateContainer_PartialDisposal_ShouldOnlyUnregisterDisposedContainer()
        {
            // Arrange
            var container1 = DiContainerFactory.CreateContainer();
            var container2 = DiContainerFactory.CreateContainer();
            
            container1.RegisterAsSingletonLazy<ITestService>(() => new TestService("container1"));
            container2.RegisterAsSingletonLazy<ITestService>(() => new TestService("container2"));
            
            // Act
            container1.Dispose(); // Only dispose first container
            
            // Assert - Should still resolve from container2
            #pragma warning disable CS0618 // Type or member is obsolete
            var resolved = DiContainerProvider.Resolve<ITestService>();
            #pragma warning restore CS0618
            
            Assert.AreEqual("container2", resolved.GetData());
            
            // Cleanup
            container2.Dispose();
        }

        #endregion

        #region Default Parameters Tests

        [Test]
        public void CreateContainer_WithoutParameters_ShouldUseDefaultDisposeRegistered()
        {
            // Arrange
            var disposableService = new DisposableTestService("test");
            
            // Act
            var container = DiContainerFactory.CreateContainer(); // No parameter - should default to true
            container.RegisterAsSingleton<DisposableTestService>(disposableService);
            
            // Force resolution
            TestHelper.ResolveFromContainer<DisposableTestService>(container);
            
            container.Dispose();
            
            // Assert - Should dispose by default
            Assert.IsTrue(disposableService.IsDisposed);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void CreateContainer_FullWorkflow_ShouldWorkEndToEnd()
        {
            // Arrange & Act
            var container = DiContainerFactory.CreateContainer();
            
            // Register services
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("workflow-test"));
            container.RegisterAsTransient<TransientTestService>(() => new TransientTestService());
            
            // Resolve services
            var testService = TestHelper.ResolveFromContainer<ITestService>(container);
            var transientService1 = TestHelper.ResolveFromContainer<TransientTestService>(container);
            var transientService2 = TestHelper.ResolveFromContainer<TransientTestService>(container);
            
            // Assert functionality
            Assert.AreEqual("workflow-test", testService.GetData());
            Assert.AreNotSame(transientService1, transientService2);
            
            // Test global resolution
            #pragma warning disable CS0618 // Type or member is obsolete
            var globalResolved = DiContainerProvider.Resolve<ITestService>();
            #pragma warning restore CS0618
            Assert.AreSame(testService, globalResolved);
            
            // Test disposal
            container.Dispose();
            
            #pragma warning disable CS0618 // Type or member is obsolete
            var exception = TestHelper.AssertThrows<Exception>(() => 
                DiContainerProvider.Resolve<ITestService>());
            #pragma warning restore CS0618
            
            Assert.That(exception.Message, Contains.Substring("not registered"));
        }

        [Test]
        public void CreateContainer_WithComplexDependencies_ShouldHandleCorrectly()
        {
            // Arrange
            var container = DiContainerFactory.CreateContainer();
            
            // Register dependencies in dependency order
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("base-service"));
            container.RegisterAsTransient<IDependentService>(() => 
                new DependentService(DiContainerProvider.Resolve<ITestService>()));
            container.RegisterAsTransient<IComplexService>(() => 
                new ComplexService(
                    DiContainerProvider.Resolve<ITestService>(),
                    DiContainerProvider.Resolve<IDependentService>()));
            
            // Act
            var complexService = TestHelper.ResolveFromContainer<IComplexService>(container);
            
            // Assert
            Assert.IsNotNull(complexService);
            Assert.IsNotNull(complexService.TestService);
            Assert.IsNotNull(complexService.DependentService);
            Assert.AreEqual("base-service", complexService.TestService.GetData());
            Assert.AreEqual("Processed: base-service", complexService.DependentService.ProcessData());
            Assert.AreEqual("base-service + Processed: base-service", complexService.CombineData());
            
            // Cleanup
            container.Dispose();
        }

        #endregion
    }
}