using System;
using LightDI.Runtime;

namespace LightDI.Tests
{
    /// <summary>
    /// Mock interface for testing dependency injection.
    /// </summary>
    public interface ITestService
    {
        void DoWork();
        string GetData();
    }

    /// <summary>
    /// Mock implementation of ITestService.
    /// </summary>
    public class TestService : ITestService
    {
        public bool WorkCalled { get; private set; }
        public string Data { get; }

        public TestService(string data = "default")
        {
            Data = data;
        }

        public void DoWork()
        {
            WorkCalled = true;
        }

        public string GetData()
        {
            return Data;
        }
    }

    /// <summary>
    /// Mock disposable service for testing disposal functionality.
    /// </summary>
    public class DisposableTestService : ITestService, IDisposable
    {
        public bool IsDisposed { get; private set; }
        public bool WorkCalled { get; private set; }
        public string Data { get; }

        public DisposableTestService(string data = "disposable")
        {
            Data = data;
        }

        public void DoWork()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(DisposableTestService));
            
            WorkCalled = true;
        }

        public string GetData()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(DisposableTestService));
            
            return Data;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    /// <summary>
    /// Mock service that depends on another service.
    /// </summary>
    public interface IDependentService
    {
        ITestService TestService { get; }
        string ProcessData();
    }

    /// <summary>
    /// Implementation of IDependentService for testing dependency resolution.
    /// </summary>
    public class DependentService : IDependentService
    {
        public ITestService TestService { get; }

        public DependentService([Inject] ITestService testService)
        {
            TestService = testService;
        }

        public string ProcessData()
        {
            return $"Processed: {TestService.GetData()}";
        }
    }

    /// <summary>
    /// Mock service with multiple dependencies.
    /// </summary>
    public interface IComplexService
    {
        ITestService TestService { get; }
        IDependentService DependentService { get; }
        string CombineData();
    }

    /// <summary>
    /// Implementation of IComplexService for testing complex dependency graphs.
    /// </summary>
    public class ComplexService : IComplexService
    {
        public ITestService TestService { get; }
        public IDependentService DependentService { get; }

        public ComplexService([Inject] ITestService testService, [Inject] IDependentService dependentService)
        {
            TestService = testService;
            DependentService = dependentService;
        }

        public string CombineData()
        {
            return $"{TestService.GetData()} + {DependentService.ProcessData()}";
        }
    }

    /// <summary>
    /// Mock service for testing singleton behavior.
    /// </summary>
    public class SingletonTestService
    {
        public Guid InstanceId { get; }
        public DateTime CreatedAt { get; }

        public SingletonTestService()
        {
            InstanceId = Guid.NewGuid();
            CreatedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// Mock service for testing transient behavior.
    /// </summary>
    public class TransientTestService
    {
        public Guid InstanceId { get; }
        public DateTime CreatedAt { get; }

        public TransientTestService()
        {
            InstanceId = Guid.NewGuid();
            CreatedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// Mock service that throws exception in constructor for testing error handling.
    /// </summary>
    public class FailingService
    {
        public FailingService()
        {
            throw new InvalidOperationException("This service always fails to initialize");
        }
    }

    /// <summary>
    /// Mock service for testing field injection.
    /// </summary>
    public class FieldInjectionService
    {
        [Inject]
        private ITestService _injectedService;

        public ITestService InjectedService => _injectedService;

        public string GetInjectedData()
        {
            return _injectedService?.GetData() ?? "No service injected";
        }
    }

    /// <summary>
    /// Mock service with both constructor and field injection.
    /// </summary>
    public class MixedInjectionService
    {
        private readonly ITestService _constructorService;
        
        [Inject]
        private IDependentService _fieldService;

        public ITestService ConstructorService => _constructorService;
        public IDependentService FieldService => _fieldService;

        public MixedInjectionService([Inject] ITestService constructorService)
        {
            _constructorService = constructorService;
        }

        public string GetCombinedData()
        {
            var constructorData = _constructorService?.GetData() ?? "No constructor service";
            var fieldData = _fieldService?.ProcessData() ?? "No field service";
            return $"{constructorData} | {fieldData}";
        }
    }
}