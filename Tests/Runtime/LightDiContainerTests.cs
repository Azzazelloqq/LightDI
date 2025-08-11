using System;
using NUnit.Framework;
using LightDI.Runtime;

namespace LightDI.Tests.Runtime
{
    /// <summary>
    /// Comprehensive tests for LightDiContainer functionality.
    /// </summary>
    [TestFixture]
    public class LightDiContainerTests
    {
        private IDiContainer _container;

        [SetUp]
        public void SetUp()
        {
            TestHelper.CleanupGlobalProvider();
            _container = TestHelper.CreateTestContainer();
        }

        [TearDown]
        public void TearDown()
        {
            _container?.Dispose();
            TestHelper.CleanupGlobalProvider();
        }

        #region Singleton Registration Tests

        [Test]
        public void RegisterAsSingletonLazy_WithFactory_ShouldCreateLazyInstance()
        {
            // Act
            _container.RegisterAsSingletonLazy<ITestService>(() => new TestService("lazy-singleton"));
            
            // Assert
            var instance1 = TestHelper.ResolveFromContainer<ITestService>(_container);
            var instance2 = TestHelper.ResolveFromContainer<ITestService>(_container);
            
            Assert.IsNotNull(instance1);
            Assert.IsNotNull(instance2);
            Assert.AreSame(instance1, instance2);
            Assert.AreEqual("lazy-singleton", instance1.GetData());
        }

        [Test]
        public void RegisterAsSingleton_WithInstance_ShouldReturnSameInstance()
        {
            // Arrange
            var singleton = new TestService("pre-created");
            
            // Act
            _container.RegisterAsSingleton<ITestService>(singleton);
            
            // Assert
            var instance1 = TestHelper.ResolveFromContainer<ITestService>(_container);
            var instance2 = TestHelper.ResolveFromContainer<ITestService>(_container);
            
            Assert.AreSame(singleton, instance1);
            Assert.AreSame(instance1, instance2);
            Assert.AreEqual("pre-created", instance1.GetData());
        }

        [Test]
        public void RegisterAsSingleton_Multiple_ShouldOverridePreviousRegistration()
        {
            // Arrange
            var firstSingleton = new TestService("first");
            var secondSingleton = new TestService("second");
            
            // Act
            _container.RegisterAsSingleton<ITestService>(firstSingleton);
            _container.RegisterAsSingleton<ITestService>(secondSingleton);
            
            // Assert
            var resolved = TestHelper.ResolveFromContainer<ITestService>(_container);
            Assert.AreSame(secondSingleton, resolved);
            Assert.AreEqual("second", resolved.GetData());
        }

        #endregion

        #region Transient Registration Tests

        [Test]
        public void RegisterAsTransient_WithFactory_ShouldCreateNewInstanceEachTime()
        {
            // Act
            _container.RegisterAsTransient<TransientTestService>(() => new TransientTestService());
            
            // Assert
                    var instance1 = TestHelper.ResolveFromContainer<TransientTestService>(_container);
        var instance2 = TestHelper.ResolveFromContainer<TransientTestService>(_container);
            
            Assert.IsNotNull(instance1);
            Assert.IsNotNull(instance2);
            Assert.AreNotSame(instance1, instance2);
            Assert.AreNotEqual(instance1.InstanceId, instance2.InstanceId);
        }

        [Test]
        public void RegisterAsTransient_Multiple_ShouldOverridePreviousRegistration()
        {
            // Arrange
            var factoryCallCount = 0;
            
            // Act
            _container.RegisterAsTransient<ITestService>(() => new TestService("first"));
            _container.RegisterAsTransient<ITestService>(() =>
            {
                factoryCallCount++;
                return new TestService("second");
            });
            
            // Assert
            var resolved = TestHelper.ResolveFromContainer<ITestService>(_container);
            Assert.AreEqual(1, factoryCallCount);
            Assert.AreEqual("second", resolved.GetData());
        }

        #endregion

        #region Resolution Tests

        [Test]
        public void Resolve_RegisteredService_ShouldReturnInstance()
        {
            // Arrange
            _container.RegisterAsSingletonLazy<ITestService>(() => new TestService("test"));
            
            // Act
            var resolved = TestHelper.ResolveFromContainer<ITestService>(_container);
            
            // Assert
            Assert.IsNotNull(resolved);
            Assert.AreEqual("test", resolved.GetData());
        }

        [Test]
        public void Resolve_UnregisteredService_ShouldThrowException()
        {
            // Act & Assert
            var exception = TestHelper.AssertThrows<Exception>(() => 
                TestHelper.ResolveFromContainer<ITestService>(_container));
            
            Assert.That(exception.Message, Contains.Substring("not registered"));
        }

        [Test]
        public void TryResolve_RegisteredService_ShouldReturnTrueAndInstance()
        {
            // Arrange
            _container.RegisterAsSingletonLazy<ITestService>(() => new TestService("test"));
            
            // Act
            var result = TestHelper.TryResolveFromContainer<ITestService>(_container, out var instance);
            
            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(instance);
            Assert.AreEqual("test", instance.GetData());
        }

        [Test]
        public void TryResolve_UnregisteredService_ShouldReturnFalseAndNull()
        {
            // Act
            var result = TestHelper.TryResolveFromContainer<ITestService>(_container, out var instance);
            
            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(instance);
        }

        #endregion

        #region Type Mismatch Tests

        [Test]
        public void Resolve_TypeMismatch_ShouldThrowException()
        {
            // Arrange - Use reflection to register a factory that returns wrong type
            var type = typeof(ITestService);
            var regInfoType = typeof(LightDI.Runtime.IDiContainer).Assembly.GetType("LightDI.Runtime.RegistrationInfo");
            var regInfoCtor = regInfoType.GetConstructor(new[] { typeof(Func<LightDI.Runtime.IDiContainer, object>), typeof(LightDI.Runtime.Lifetime) });
            
            var reg = regInfoCtor.Invoke(new object[] {
                new Func<LightDI.Runtime.IDiContainer, object>(_ => "string instead of ITestService"),
                LightDI.Runtime.Lifetime.Singleton
            });
            
            var registrationsField = _container.GetType()
                .GetField("_registrations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var registrations = registrationsField.GetValue(_container) as System.Collections.IDictionary;
            registrations[type] = reg;
            
            // Act & Assert
            var exception = TestHelper.AssertThrows<Exception>(() => 
                TestHelper.ResolveFromContainer<ITestService>(_container));
            
            Assert.That(exception.Message, Contains.Substring("type mismatch"));
        }

        [Test]
        public void TryResolve_TypeMismatch_ShouldThrowException()
        {
            // Arrange - Use reflection to register a factory that returns wrong type
            var type = typeof(ITestService);
            var regInfoType = typeof(LightDI.Runtime.IDiContainer).Assembly.GetType("LightDI.Runtime.RegistrationInfo");
            var regInfoCtor = regInfoType.GetConstructor(new[] { typeof(Func<LightDI.Runtime.IDiContainer, object>), typeof(LightDI.Runtime.Lifetime) });
            
            var reg = regInfoCtor.Invoke(new object[] {
                new Func<LightDI.Runtime.IDiContainer, object>(_ => "string instead of ITestService"),
                LightDI.Runtime.Lifetime.Singleton
            });
            
            var registrationsField = _container.GetType()
                .GetField("_registrations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var registrations = registrationsField.GetValue(_container) as System.Collections.IDictionary;
            registrations[type] = reg;
            
            // Act & Assert
            var exception = TestHelper.AssertThrows<Exception>(() => 
                TestHelper.TryResolveFromContainer<ITestService>(_container, out var instance));
            
            Assert.That(exception.Message, Contains.Substring("type mismatch"));
        }

        #endregion

        #region Disposal Tests

        [Test]
        public void Dispose_WithDisposableServices_ShouldDisposeAllServices()
        {
            // Arrange
            var disposableService1 = new DisposableTestService("service1");
            var disposableService2 = new DisposableTestService("service2");
            
            _container.RegisterAsSingleton<DisposableTestService>(disposableService1);
            _container.RegisterAsTransient<DisposableTestService>(() => disposableService2);
            
            // Force resolution to create instances
            TestHelper.ResolveFromContainer<DisposableTestService>(_container);
            var transientInstance = TestHelper.ResolveFromContainer<DisposableTestService>(_container);
            
            // Act
            _container.Dispose();
            
            // Assert
            Assert.IsTrue(disposableService1.IsDisposed);
            Assert.IsTrue(transientInstance.IsDisposed);
        }

        [Test]
        public void Dispose_WithoutDisposableServices_ShouldCompleteWithoutError()
        {
            // Arrange
            _container.RegisterAsSingletonLazy<ITestService>(() => new TestService("test"));
            
            // Act & Assert
            Assert.DoesNotThrow(() => _container.Dispose());
        }

        [Test]
        public void Dispose_CalledMultipleTimes_ShouldNotThrowException()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                _container.Dispose();
                _container.Dispose();
                _container.Dispose();
            });
        }

        [Test]
        public void Resolve_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            _container.RegisterAsSingletonLazy<ITestService>(() => new TestService("test"));
            _container.Dispose();
            
            // Act & Assert
            var exception = TestHelper.AssertThrows<ObjectDisposedException>(() => 
                TestHelper.ResolveFromContainer<ITestService>(_container));
            
            Assert.That(exception.ObjectName, Is.EqualTo("LightDiContainer"));
        }

        [Test]
        public void TryResolve_AfterDispose_ShouldReturnFalse()
        {
            // Arrange
            _container.RegisterAsSingletonLazy<ITestService>(() => new TestService("test"));
            _container.Dispose();
            
            // Act
            var result = TestHelper.TryResolveFromContainer<ITestService>(_container, out var instance);
            
            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(instance);
        }

        #endregion

        #region Factory Exception Tests

        [Test]
        public void Resolve_FactoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            _container.RegisterAsSingletonLazy<FailingService>(() => new FailingService());
            
            // Act & Assert
            var exception = TestHelper.AssertThrows<InvalidOperationException>(() => 
                TestHelper.ResolveFromContainer<FailingService>(_container));
            
            Assert.That(exception.Message, Contains.Substring("always fails"));
        }

        [Test]
        public void TryResolve_FactoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            _container.RegisterAsSingletonLazy<FailingService>(() => new FailingService());
            
            // Act & Assert
            var exception = TestHelper.AssertThrows<InvalidOperationException>(() => 
                TestHelper.TryResolveFromContainer<FailingService>(_container, out var instance));
            
            Assert.That(exception.Message, Contains.Substring("always fails"));
        }

        #endregion

        #region Dispose Callback Tests

        [Test]
        public void SubscribeOnDisposeCallback_WhenDisposed_ShouldInvokeCallback()
        {
            // Arrange
            var callbackInvoked = false;
            var container = new LightDiContainer();
            
            // Act
            container.SubscribeOnDisposeCallback(() => callbackInvoked = true);
            container.Dispose();
            
            // Assert
            Assert.IsTrue(callbackInvoked);
        }

        [Test]
        public void SubscribeOnDisposeCallback_AfterDispose_ShouldThrowException()
        {
            // Arrange
            var container = new LightDiContainer();
            container.Dispose();
            
            // Act & Assert
            var exception = TestHelper.AssertThrows<Exception>(() => 
                container.SubscribeOnDisposeCallback(() => { }));
            
            Assert.That(exception.Message, Contains.Substring("disposed container"));
        }

        #endregion

        #region Container Without Dispose Registration Tests

        [Test]
        public void Container_WithDisposeRegisteredFalse_ShouldNotDisposeServices()
        {
            // Arrange
            var containerWithoutDispose = DiContainerFactory.CreateContainer(false);
            var disposableService = new DisposableTestService("test");
            
            containerWithoutDispose.RegisterAsSingleton<DisposableTestService>(disposableService);
            TestHelper.ResolveFromContainer<DisposableTestService>(containerWithoutDispose);
            
            // Act
            containerWithoutDispose.Dispose();
            
            // Assert
            Assert.IsFalse(disposableService.IsDisposed);
            
            // Cleanup
            disposableService.Dispose();
        }

        #endregion
    }
}