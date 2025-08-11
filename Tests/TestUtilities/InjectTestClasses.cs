using LightDI.Runtime;

namespace LightDI.Tests
{
    /// <summary>
    /// Test classes for InjectAttribute functionality testing.
    /// These are separate top-level classes so Source Generator can process them.
    /// </summary>
    
    public class ConstructorParameterTest
    {
        public ConstructorParameterTest([Inject] ITestService testService)
        {
        }
    }

    public class MultipleParameterTest
    {
        public MultipleParameterTest([Inject] ITestService testService, [Inject] IDependentService dependentService, string regularParameter)
        {
        }
    }

    public class FieldTest
    {
        [Inject]
        private ITestService _injectedService;
    }

    public class MultipleFieldTest
    {
        [Inject]
        private ITestService _injectedService;
        
        [Inject]
        private IDependentService _anotherInjectedService;
        
        private IComplexService _nonInjectedService;
    }

    public class ReflectionTest
    {
        public ReflectionTest([Inject] ITestService testService, string regularParam)
        {
        }
    }

    public class FilterTest
    {
        public FilterTest([Inject] ITestService testService, [Inject] IDependentService dependentService, string regularParam)
        {
        }
    }
}