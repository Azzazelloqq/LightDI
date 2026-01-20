using System;
using System.Collections.Generic;
using LightDI.Runtime;
using NUnit.Framework;

namespace LightDI.Tests
{
    /// <summary>
    /// Comprehensive integration tests for LightDI functionality.
    /// Tests complex scenarios and real-world usage patterns.
    /// </summary>
    [TestFixture]
    public class IntegrationTests
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

        #region Complex Dependency Resolution Tests

        [Test]
        public void ComplexDependencyChain_ShouldResolveCorrectly()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            
            // Register services in dependency order
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("root-service"));
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
            
            // Verify singleton behavior
            var directTestService = TestHelper.ResolveFromContainer<ITestService>(container);
            Assert.AreSame(directTestService, complexService.TestService);
            Assert.AreSame(directTestService, complexService.DependentService.TestService);
            
            // Verify data flow
            Assert.AreEqual("root-service", complexService.TestService.GetData());
            Assert.AreEqual("Processed: root-service", complexService.DependentService.ProcessData());
            Assert.AreEqual("root-service + Processed: root-service", complexService.CombineData());
            
            // Cleanup
            container.Dispose();
        }

        [Test]
        public void MultipleContainers_WithDifferentServices_ShouldWorkTogether()
        {
            // Arrange
            const string baseScope = "LightDI.Tests.Base";
            const string extensionScope = "LightDI.Tests.Extension";
            var baseContainer = TestHelper.CreateTestContainer(baseScope);
            var extensionContainer = TestHelper.CreateTestContainer(extensionScope);
            
            // Base container provides core services
            baseContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("base-data"));
            
            // Extension container provides additional services that depend on base services
            extensionContainer.RegisterAsTransient<IDependentService>(() => 
                new DependentService(DiContainerProvider.Resolve<ITestService>(baseScope)));
            extensionContainer.RegisterAsTransient<IComplexService>(() => 
                new ComplexService(
                    DiContainerProvider.Resolve<ITestService>(baseScope),
                    DiContainerProvider.Resolve<IDependentService>(extensionScope)));
            
            // Act
            var testService = TestHelper.ResolveFromContainer<ITestService>(baseContainer);
            var dependentService = TestHelper.ResolveFromContainer<IDependentService>(extensionContainer);
            var complexService = TestHelper.ResolveFromContainer<IComplexService>(extensionContainer);
            
            // Assert
            Assert.AreEqual("base-data", testService.GetData());
            Assert.AreEqual("Processed: base-data", dependentService.ProcessData());
            Assert.AreEqual("base-data + Processed: base-data", complexService.CombineData());
            
            // Verify they're using the same base service instance
            Assert.AreSame(testService, dependentService.TestService);
            Assert.AreSame(testService, complexService.TestService);
            
            // Cleanup
            baseContainer.Dispose();
            extensionContainer.Dispose();
        }

        #endregion

        #region Lifecycle Management Tests

        [Test]
        public void LifecycleManagement_WithMixedLifetimes_ShouldBehaveCorrectly()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            
            container.RegisterAsSingletonLazy<SingletonTestService>(() => new SingletonTestService());
            container.RegisterAsTransient<TransientTestService>(() => new TransientTestService());
            
            // Act
            var singleton1 = TestHelper.ResolveFromContainer<SingletonTestService>(container);
            var singleton2 = TestHelper.ResolveFromContainer<SingletonTestService>(container);
            
            var transient1 = TestHelper.ResolveFromContainer<TransientTestService>(container);
            var transient2 = TestHelper.ResolveFromContainer<TransientTestService>(container);
            
            // Assert
            // Singletons should be the same instance
            Assert.AreSame(singleton1, singleton2);
            Assert.AreEqual(singleton1.InstanceId, singleton2.InstanceId);
            
            // Transients should be different instances
            Assert.AreNotSame(transient1, transient2);
            Assert.AreNotEqual(transient1.InstanceId, transient2.InstanceId);
            
            // Cleanup
            container.Dispose();
        }

        [Test]
        public void DisposalChain_WithComplexDependencies_ShouldDisposeAllServices()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            
            // Create singleton service that will be tracked for disposal
            var singletonService = new DisposableTestService("singleton");
            container.RegisterAsSingleton<DisposableTestService>(singletonService);
            
            // Register transient services that create new instances (these will be tracked)
            container.RegisterAsTransient<ITestService>(() => new DisposableTestService("transient1"));
            
            // Force resolution to add services to disposal list
            var singleton = TestHelper.ResolveFromContainer<DisposableTestService>(container);
            var transient1 = TestHelper.ResolveFromContainer<ITestService>(container) as DisposableTestService;
            var transient2 = TestHelper.ResolveFromContainer<ITestService>(container) as DisposableTestService;
            
            // Verify we got the expected instances
            Assert.AreSame(singletonService, singleton);
            Assert.IsNotNull(transient1);
            Assert.IsNotNull(transient2);
            Assert.AreNotSame(transient1, transient2); // Transients should be different instances
            
            // Act
            container.Dispose();
            
            // Assert - Singleton should be disposed, transients should be disposed
            Assert.IsTrue(singleton.IsDisposed, "Singleton service should be disposed");
            Assert.IsTrue(transient1.IsDisposed, "Transient service 1 should be disposed");
            Assert.IsTrue(transient2.IsDisposed, "Transient service 2 should be disposed");
        }

        #endregion

        #region Real-World Scenario Tests

        [Test]
        public void GameManagerScenario_ShouldWorkLikeRealUsage()
        {
            // Arrange - Simulate the example from README
            var container = TestHelper.CreateTestContainer();
            
            // Register services like in the example
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("game-data"));
            container.RegisterAsSingletonLazy<TransientTestService>(() => new TransientTestService());
            
            // Simulate what a generated factory might do
            var gameManagerLikeService = new GameManagerLikeService(
                DiContainerProvider.Resolve<ITestService>(), 
                42);
            
            // Act
            var result = gameManagerLikeService.ProcessGame();
            
            // Assert
            Assert.IsNotNull(result);
            Assert.That(result, Contains.Substring("game-data"));
            Assert.That(result, Contains.Substring("42"));
            
            // Cleanup
            container.Dispose();
        }

        [Test]
        public void ServiceLocatorPattern_ShouldWorkWithMultipleContainers()
        {
            // Arrange
            const string coreScope = "LightDI.Tests.Core";
            const string pluginScope1 = "LightDI.Tests.Plugin1";
            const string pluginScope2 = "LightDI.Tests.Plugin2";
            var coreContainer = TestHelper.CreateTestContainer(coreScope);
            var pluginContainer1 = TestHelper.CreateTestContainer(pluginScope1);
            var pluginContainer2 = TestHelper.CreateTestContainer(pluginScope2);
            
            // Core services
            coreContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("core"));
            
            // Plugin services
            pluginContainer1.RegisterAsTransient<TransientTestService>(() => new TransientTestService());
            pluginContainer2.RegisterAsTransient<SingletonTestService>(() => new SingletonTestService());
            
            // Act - Resolve services from different containers via global provider
            #pragma warning disable CS0618 // Type or member is obsolete
            var coreService = DiContainerProvider.Resolve<ITestService>(coreScope);
            var plugin1Service = DiContainerProvider.Resolve<TransientTestService>(pluginScope1);
            var plugin2Service = DiContainerProvider.Resolve<SingletonTestService>(pluginScope2);
            #pragma warning restore CS0618
            
            // Assert
            Assert.IsNotNull(coreService);
            Assert.IsNotNull(plugin1Service);
            Assert.IsNotNull(plugin2Service);
            Assert.AreEqual("core", coreService.GetData());
            
            // Cleanup
            coreContainer.Dispose();
            pluginContainer1.Dispose();
            pluginContainer2.Dispose();
        }

        #endregion

        #region Error Handling and Edge Cases

        [Test]
        public void CircularDependency_ShouldThrowException()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            
            // Create a circular dependency scenario
            container.RegisterAsSingletonLazy<CircularServiceA>(() => 
                new CircularServiceA(DiContainerProvider.Resolve<CircularServiceB>()));
            container.RegisterAsSingletonLazy<CircularServiceB>(() => 
                new CircularServiceB(DiContainerProvider.Resolve<CircularServiceA>()));
            
            // Act & Assert - Circular dependencies should be detected
            Assert.Throws<InvalidOperationException>(() =>
            {
                #pragma warning disable CS0618 // Type or member is obsolete
                var serviceA = DiContainerProvider.Resolve<CircularServiceA>();
                #pragma warning restore CS0618
            });
            
            // Cleanup
            container.Dispose();
        }

        [Test]
        public void ContainerDisposal_DuringActiveResolution_ShouldHandleGracefully()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            var serviceCreated = false;
            
            container.RegisterAsSingletonLazy<ITestService>(() =>
            {
                serviceCreated = true;
                return new TestService("test");
            });
            
            // Act - Dispose container while it might be resolving
            container.Dispose();
            
            // Assert - Should not be able to resolve after disposal
            var exception = TestHelper.AssertThrows<ObjectDisposedException>(() =>
                TestHelper.ResolveFromContainer<ITestService>(container));
            
            Assert.IsFalse(serviceCreated, "Service should not be created from disposed container");
        }

        [Test]
        public void LargeNumberOfServices_ShouldPerformReasonably()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            const int serviceCount = 1000;
            
            // Register many services
            for (int i = 0; i < serviceCount; i++)
            {
                var serviceData = $"service-{i}";
                container.RegisterAsTransient<ITestService>(() => new TestService(serviceData));
            }
            
            // Act & Assert - Should complete in reasonable time
            var startTime = DateTime.Now;
            
            for (int i = 0; i < 100; i++)
            {
                var service = TestHelper.ResolveFromContainer<ITestService>(container);
                Assert.IsNotNull(service);
            }
            
            var elapsed = DateTime.Now - startTime;
            Assert.Less(elapsed.TotalSeconds, 1.0, "Resolution should complete quickly even with many registered services");
            
            // Cleanup
            container.Dispose();
        }

        #endregion

        #region Test Helper Classes

        private class GameManagerLikeService
        {
            private readonly ITestService _testService;
            private readonly int _value;

            public GameManagerLikeService(ITestService testService, int value)
            {
                _testService = testService;
                _value = value;
            }

            public string ProcessGame()
            {
                return $"Processing game with {_testService.GetData()} and value {_value}";
            }
        }

        private class CircularServiceA
        {
            public CircularServiceB ServiceB { get; }

            public CircularServiceA(CircularServiceB serviceB)
            {
                ServiceB = serviceB;
            }
        }

        private class CircularServiceB
        {
            public CircularServiceA ServiceA { get; }

            public CircularServiceB(CircularServiceA serviceA)
            {
                ServiceA = serviceA;
            }
        }

        #endregion
    }
}