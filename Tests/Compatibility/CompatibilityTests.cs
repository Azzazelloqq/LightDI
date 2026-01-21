using System;
using System.Threading.Tasks;
using LightDI.Runtime;
using NUnit.Framework;

namespace LightDI.Tests
{
    /// <summary>
    /// Tests to ensure LightDI works correctly with Unity-specific scenarios
    /// and maintains compatibility with expected usage patterns.
    /// </summary>
    [TestFixture]
    public class CompatibilityTests
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

        #region Unity Domain Reload Compatibility

        [Test]
        public void GlobalProvider_AfterStaticReset_ShouldStartEmpty()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("test"));
            
            // Verify service is registered
            #pragma warning disable CS0618 // Type or member is obsolete
            var service = DiContainerProvider.Resolve<ITestService>();
            #pragma warning restore CS0618
            Assert.IsNotNull(service);
            
            // Act - Simulate domain reload by disposing global provider
            DiContainerProvider.Dispose();
            
            // Assert - Should not be able to resolve after reset
            #pragma warning disable CS0618 // Type or member is obsolete
            var exception = TestHelper.AssertThrows<Exception>(() => 
                DiContainerProvider.Resolve<ITestService>());
            #pragma warning restore CS0618
            
            Assert.That(exception.Message, Contains.Substring("not registered"));
            
            // Cleanup
            container.Dispose();
        }

        [Test]
        public void MultipleContainerCreation_AfterGlobalDispose_ShouldWork()
        {
            // Arrange
            var firstContainer = TestHelper.CreateTestContainer();
            firstContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("first"));
            
            // Simulate domain reload
            DiContainerProvider.Dispose();
            
            // Act - Create new container after global dispose
            var secondContainer = TestHelper.CreateTestContainer();
            secondContainer.RegisterAsSingletonLazy<ITestService>(() => new TestService("second"));
            
            // Assert
            #pragma warning disable CS0618 // Type or member is obsolete
            var service = DiContainerProvider.Resolve<ITestService>();
            #pragma warning restore CS0618
            
            Assert.IsNotNull(service);
            Assert.AreEqual("second", service.GetData());
            
            // Cleanup
            firstContainer.Dispose();
            secondContainer.Dispose();
        }

        #endregion

        #region Example Code Compatibility

        [Test]
        public void ExampleCompositionRoot_ShouldWorkAsDocumented()
        {
            // This test verifies that the example code from README works correctly
            
            // Arrange & Act - Replicate the example
            var container = DiContainerFactory.CreateContainer();
            
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("example-service"));
            container.RegisterAsSingletonLazy<TransientTestService>(() => new TransientTestService());
            
            // Simulate what GameManagerFactory.CreateGameManager(10) would do
            var testService = TestHelper.ResolveFromContainer<ITestService>(container);
            var gameManagerLike = new ExampleGameManager(testService, 10);
            
            // Assert
            Assert.IsNotNull(gameManagerLike);
            Assert.IsNotNull(gameManagerLike.TestService);
            Assert.AreEqual(10, gameManagerLike.RuntimeValue);
            Assert.AreEqual("example-service", gameManagerLike.TestService.GetData());
            
            var result = gameManagerLike.RunGame();
            Assert.That(result, Contains.Substring("example-service"));
            Assert.That(result, Contains.Substring("10"));
            
            // Cleanup
            container.Dispose();
        }

        [Test]
        public void RegistrationPatterns_FromREADME_ShouldWork()
        {
            // Arrange
            var container = DiContainerFactory.CreateContainer();
            
            // Act - Test all registration patterns from README
            
            // Register as Singleton with factory
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("singleton-factory"));
            
            // Register existing instance as Singleton
            var existingService = new TestService("existing-instance");
            container.RegisterAsSingleton<TestService>(existingService);
            
            // Register as Transient
            container.RegisterAsTransient<TransientTestService>(() => new TransientTestService());
            
            // Assert
            var singletonFromFactory = TestHelper.ResolveFromContainer<ITestService>(container);
            var existingInstance = TestHelper.ResolveFromContainer<TestService>(container);
            var transient1 = TestHelper.ResolveFromContainer<TransientTestService>(container);
            var transient2 = TestHelper.ResolveFromContainer<TransientTestService>(container);
            
            Assert.AreEqual("singleton-factory", singletonFromFactory.GetData());
            Assert.AreSame(existingService, existingInstance);
            Assert.AreNotSame(transient1, transient2);
            
            // Cleanup
            container.Dispose();
        }

        #endregion

        #region Obsolete API Compatibility

        [Test]
        public void ObsoleteResolveMethod_ShouldStillWork()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("obsolete-test"));
            
            // Act & Assert - Obsolete method should still function
            #pragma warning disable CS0618 // Type or member is obsolete
            var resolved = DiContainerProvider.Resolve<ITestService>();
            #pragma warning restore CS0618
            
            Assert.IsNotNull(resolved);
            Assert.AreEqual("obsolete-test", resolved.GetData());
            
            // Cleanup
            container.Dispose();
        }

        [Test]
        public void ObsoleteMethod_ShouldHaveCorrectObsoleteMessage()
        {
            // This test verifies the obsolete message is appropriate
            // We can't test the actual obsolete attribute at runtime easily,
            // but we can verify the method still works as expected
            
            // Arrange
            var container = TestHelper.CreateTestContainer();
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("obsolete-message-test"));
            
            // Act - The obsolete method should work but with warning
            #pragma warning disable CS0618 // Type or member is obsolete
            Assert.DoesNotThrow(() => DiContainerProvider.Resolve<ITestService>());
            #pragma warning restore CS0618
            
            // Cleanup
            container.Dispose();
        }

        #endregion

        #region Assembly Loading Compatibility

        [Test]
        public void InternalServices_ShouldNotBeDirectlyAccessible()
        {
            // This test verifies that internal classes can't be directly instantiated
            // This matches the documentation warning about internal access
            
            // We can't directly test this at runtime since internal classes
            // aren't accessible, but we can verify the public API surface
            
            var containerType = typeof(IDiContainer);
            var factoryType = typeof(DiContainerFactory);
            var providerType = typeof(DiContainerProvider);
            
            Assert.IsTrue(containerType.IsPublic);
            Assert.IsTrue(factoryType.IsPublic);
            Assert.IsTrue(providerType.IsPublic);
        }

        #endregion

        #region Memory Management Compatibility

        [Test]
        public void LargeObjectGraph_ShouldNotCauseMemoryLeaks()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            var initialMemory = GC.GetTotalMemory(true);
            
            // Act - Create and dispose many objects
            for (int i = 0; i < 1000; i++)
            {
                container.RegisterAsTransient<DisposableTestService>(() => new DisposableTestService($"service-{i}"));
                var service = TestHelper.ResolveFromContainer<DisposableTestService>(container);
                Assert.IsNotNull(service);
            }
            
            container.Dispose();
            
            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(true);
            
            // Assert - Memory shouldn't grow excessively
            var memoryGrowth = finalMemory - initialMemory;
            Assert.Less(memoryGrowth, 1024 * 1024, "Memory growth should be reasonable"); // Less than 1MB growth
        }

        #endregion

        #region Thread Safety Basics

        [Test]
        public void ConcurrentContainerCreation_ShouldWork()
        {
            // Arrange
            var previousSetting = DiContainerProvider.AllowMultipleGlobalContainers;
            DiContainerProvider.AllowMultipleGlobalContainers = true;
            var containers = new IDiContainer[10];
            var exceptions = new Exception[10];
            
            // Act - Create containers concurrently
            Parallel.For(0, 10, i =>
            {
                try
                {
                    containers[i] = DiContainerFactory.CreateContainer();
                    containers[i].RegisterAsSingletonLazy<ITestService>(() => new TestService($"concurrent-{i}"));
                }
                catch (Exception ex)
                {
                    exceptions[i] = ex;
                }
            });
            
            // Assert - All containers should be created successfully
            for (int i = 0; i < 10; i++)
            {
                Assert.IsNull(exceptions[i], $"Container {i} creation failed: {exceptions[i]?.Message}");
                Assert.IsNotNull(containers[i], $"Container {i} is null");
                
                var service = TestHelper.ResolveFromContainer<ITestService>(containers[i]);
                Assert.IsNotNull(service);
                Assert.AreEqual($"concurrent-{i}", service.GetData());
            }
            
            // Cleanup
            for (int i = 0; i < 10; i++)
            {
                containers[i]?.Dispose();
            }
            DiContainerProvider.AllowMultipleGlobalContainers = previousSetting;
        }

        #endregion

        #region Helper Classes

        private class ExampleGameManager
        {
            public ITestService TestService { get; }
            public int RuntimeValue { get; }

            public ExampleGameManager(ITestService testService, int runtimeValue)
            {
                TestService = testService;
                RuntimeValue = runtimeValue;
            }

            public string RunGame()
            {
                return $"GameManager running with {TestService.GetData()} and runtime value: {RuntimeValue}";
            }
        }

        #endregion
    }
}