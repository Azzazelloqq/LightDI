using System;
using LightDI.Runtime;
using NUnit.Framework;

namespace LightDI.Tests
{
    /// <summary>
    /// Comprehensive tests for RegistrationInfo functionality.
    /// </summary>
    [TestFixture]
    public class RegistrationInfoTests
    {
        #region Constructor Tests - Factory

        [Test]
        public void Constructor_WithFactory_ShouldInitializeCorrectly()
        {
            // Arrange
            var testService = new TestService("test");
            Func<IDiContainer, object> factory = _ => testService;
            var lifetime = Lifetime.Singleton;
            
            // Act
            var registrationInfo = new RegistrationInfo(factory, lifetime);
            
            // Assert
            Assert.AreSame(factory, registrationInfo.Factory);
            Assert.AreEqual(lifetime, registrationInfo.Lifetime);
            Assert.IsNull(registrationInfo.CachedInstance);
        }

        [Test]
        public void Constructor_WithFactoryAndTransientLifetime_ShouldInitializeCorrectly()
        {
            // Arrange
            Func<IDiContainer, object> factory = _ => new TransientTestService();
            var lifetime = Lifetime.Transient;
            
            // Act
            var registrationInfo = new RegistrationInfo(factory, lifetime);
            
            // Assert
            Assert.AreSame(factory, registrationInfo.Factory);
            Assert.AreEqual(lifetime, registrationInfo.Lifetime);
            Assert.IsNull(registrationInfo.CachedInstance);
        }

        [Test]
        public void Constructor_WithNullFactory_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new RegistrationInfo(null, Lifetime.Singleton));
        }

        #endregion

        #region Constructor Tests - Instance4

        [Test]
        public void Constructor_WithInstance_ShouldInitializeCorrectly()
        {
            // Arrange
            var testService = new TestService("instance");
            var lifetime = Lifetime.Singleton;
            
            // Act
            var registrationInfo = new RegistrationInfo(testService, lifetime);
            
            // Assert
            Assert.IsNotNull(registrationInfo.Factory);
            Assert.AreEqual(lifetime, registrationInfo.Lifetime);
            Assert.AreSame(testService, registrationInfo.CachedInstance);
        }

        [Test]
        public void Constructor_WithInstanceAndTransientLifetime_ShouldInitializeCorrectly()
        {
            // Arrange
            var testService = new TestService("transient-instance");
            var lifetime = Lifetime.Transient;
            
            // Act
            var registrationInfo = new RegistrationInfo(testService, lifetime);
            
            // Assert
            Assert.IsNotNull(registrationInfo.Factory);
            Assert.AreEqual(lifetime, registrationInfo.Lifetime);
            Assert.AreSame(testService, registrationInfo.CachedInstance);
        }

        [Test]
        public void Constructor_WithNullInstance_ShouldInitializeWithNullCache()
        {
            // Act
            var registrationInfo = new RegistrationInfo((object)null, Lifetime.Singleton);
            
            // Assert
            Assert.IsNotNull(registrationInfo.Factory);
            Assert.AreEqual(Lifetime.Singleton, registrationInfo.Lifetime);
            Assert.IsNull(registrationInfo.CachedInstance);
        }

        #endregion

        #region Factory Behavior Tests

        [Test]
        public void Factory_CreatedFromInstance_ShouldReturnSameInstance()
        {
            // Arrange
            var testService = new TestService("factory-test");
            var registrationInfo = new RegistrationInfo(testService, Lifetime.Singleton);
            var mockContainer = TestHelper.CreateTestContainer();
            
            // Act
            var result1 = registrationInfo.Factory(mockContainer);
            var result2 = registrationInfo.Factory(mockContainer);
            
            // Assert
            Assert.AreSame(testService, result1);
            Assert.AreSame(testService, result2);
            Assert.AreSame(result1, result2);
            
            // Cleanup
            mockContainer.Dispose();
        }

        [Test]
        public void Factory_CreatedFromFactoryFunction_ShouldInvokeFunction()
        {
            // Arrange
            var callCount = 0;
            var testService = new TestService("function-test");
            Func<IDiContainer, object> factory = _ =>
            {
                callCount++;
                return testService;
            };
            
            var registrationInfo = new RegistrationInfo(factory, Lifetime.Transient);
            var mockContainer = TestHelper.CreateTestContainer();
            
            // Act
            var result = registrationInfo.Factory(mockContainer);
            
            // Assert
            Assert.AreEqual(1, callCount);
            Assert.AreSame(testService, result);
            
            // Cleanup
            mockContainer.Dispose();
        }

        [Test]
        public void Factory_WithContainerDependency_ShouldPassContainer()
        {
            // Arrange
            IDiContainer passedContainer = null;
            Func<IDiContainer, object> factory = container =>
            {
                passedContainer = container;
                return new TestService("container-test");
            };
            
            var registrationInfo = new RegistrationInfo(factory, Lifetime.Singleton);
            var mockContainer = TestHelper.CreateTestContainer();
            
            // Act
            registrationInfo.Factory(mockContainer);
            
            // Assert
            Assert.AreSame(mockContainer, passedContainer);
            
            // Cleanup
            mockContainer.Dispose();
        }

        #endregion

        #region CachedInstance Tests

        [Test]
        public void CachedInstance_InitiallyNull_ForFactoryConstructor()
        {
            // Arrange
            var registrationInfo = new RegistrationInfo(_ => new TestService(), Lifetime.Singleton);
            
            // Act & Assert
            Assert.IsNull(registrationInfo.CachedInstance);
        }

        [Test]
        public void CachedInstance_InitiallySet_ForInstanceConstructor()
        {
            // Arrange
            var testService = new TestService("cached");
            var registrationInfo = new RegistrationInfo(testService, Lifetime.Singleton);
            
            // Act & Assert
            Assert.AreSame(testService, registrationInfo.CachedInstance);
        }

        [Test]
        public void CachedInstance_CanBeSet_AfterCreation()
        {
            // Arrange
            var registrationInfo = new RegistrationInfo(_ => new TestService(), Lifetime.Singleton);
            var testService = new TestService("new-cache");
            
            // Act
            registrationInfo.CachedInstance = testService;
            
            // Assert
            Assert.AreSame(testService, registrationInfo.CachedInstance);
        }

        [Test]
        public void CachedInstance_CanBeChanged_AfterInitialSet()
        {
            // Arrange
            var initialService = new TestService("initial");
            var registrationInfo = new RegistrationInfo(initialService, Lifetime.Singleton);
            var newService = new TestService("new");
            
            // Act
            registrationInfo.CachedInstance = newService;
            
            // Assert
            Assert.AreSame(newService, registrationInfo.CachedInstance);
            Assert.AreNotSame(initialService, registrationInfo.CachedInstance);
        }

        [Test]
        public void CachedInstance_CanBeSetToNull()
        {
            // Arrange
            var testService = new TestService("test");
            var registrationInfo = new RegistrationInfo(testService, Lifetime.Singleton);
            
            // Act
            registrationInfo.CachedInstance = null;
            
            // Assert
            Assert.IsNull(registrationInfo.CachedInstance);
        }

        #endregion

        #region Lifetime Tests

        [Test]
        public void Lifetime_SingletonValue_ShouldBeCorrect()
        {
            // Arrange & Act
            var registrationInfo = new RegistrationInfo(_ => new TestService(), Lifetime.Singleton);
            
            // Assert
            Assert.AreEqual(Lifetime.Singleton, registrationInfo.Lifetime);
        }

        [Test]
        public void Lifetime_TransientValue_ShouldBeCorrect()
        {
            // Arrange & Act
            var registrationInfo = new RegistrationInfo(_ => new TestService(), Lifetime.Transient);
            
            // Assert
            Assert.AreEqual(Lifetime.Transient, registrationInfo.Lifetime);
        }

        [Test]
        public void Lifetime_IsReadOnly_CannotBeChanged()
        {
            // Arrange
            var registrationInfo = new RegistrationInfo(_ => new TestService(), Lifetime.Singleton);
            
            // Act & Assert - Should not have a setter (compile-time check)
            var lifetime = registrationInfo.Lifetime;
            Assert.AreEqual(Lifetime.Singleton, lifetime);
            
            // This would cause a compile error if we tried to set it:
            // registrationInfo.Lifetime = Lifetime.Transient; // Should not compile
        }

        #endregion

        #region Integration Tests

        [Test]
        public void RegistrationInfo_WithComplexFactory_ShouldWorkCorrectly()
        {
            // Arrange
            var container = TestHelper.CreateTestContainer();
            container.RegisterAsSingletonLazy<ITestService>(() => new TestService("dependency"));
            
            Func<IDiContainer, object> complexFactory = diContainer =>
            {
                var dependency = TestHelper.ResolveFromContainer<ITestService>(diContainer);
                return new DependentService(dependency);
            };
            
            var registrationInfo = new RegistrationInfo(complexFactory, Lifetime.Transient);
            
            // Act
            var result = registrationInfo.Factory(container);
            
            // Assert
            Assert.IsInstanceOf<DependentService>(result);
            var dependentService = (DependentService)result;
            Assert.IsNotNull(dependentService.TestService);
            Assert.AreEqual("dependency", dependentService.TestService.GetData());
            
            // Cleanup
            container.Dispose();
        }

        [Test]
        public void RegistrationInfo_WithDisposableInstance_ShouldMaintainReference()
        {
            // Arrange
            var disposableService = new DisposableTestService("disposable");
            var registrationInfo = new RegistrationInfo(disposableService, Lifetime.Singleton);
            
            // Act
            var factoryResult = registrationInfo.Factory(null);
            
            // Assert
            Assert.AreSame(disposableService, registrationInfo.CachedInstance);
            Assert.AreSame(disposableService, factoryResult);
            Assert.IsFalse(disposableService.IsDisposed);
            
            // Cleanup
            disposableService.Dispose();
        }

        [Test]
        public void RegistrationInfo_FactoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            Func<IDiContainer, object> failingFactory = _ => throw new InvalidOperationException("Factory failed");
            var registrationInfo = new RegistrationInfo(failingFactory, Lifetime.Singleton);
            
            // Act & Assert
            var exception = TestHelper.AssertThrows<InvalidOperationException>(() => 
                registrationInfo.Factory(null));
            
            Assert.That(exception.Message, Contains.Substring("Factory failed"));
        }

        #endregion
    }
}