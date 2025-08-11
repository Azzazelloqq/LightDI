using System;
using System.Collections.Generic;
using System.Reflection;
using LightDI.Runtime;
using NUnit.Framework;

namespace LightDI.Tests
{
    /// <summary>
    /// Comprehensive tests for InjectAttribute functionality.
    /// </summary>
    [TestFixture]
    public class InjectAttributeTests
    {
        #region Attribute Creation Tests

        [Test]
        public void InjectAttribute_CanBeCreated()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new InjectAttribute());
        }

        [Test]
        public void InjectAttribute_InheritsFromAttribute()
        {
            // Arrange
            var injectAttribute = new InjectAttribute();
            
            // Act & Assert
            Assert.IsInstanceOf<Attribute>(injectAttribute);
        }

        #endregion

        #region AttributeUsage Tests

        [Test]
        public void InjectAttribute_HasCorrectAttributeUsage()
        {
            // Arrange
            var attributeType = typeof(InjectAttribute);
            
            // Act
            var attributeUsage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();
            
            // Assert
            Assert.IsNotNull(attributeUsage);
            Assert.That(attributeUsage.ValidOn, Is.EqualTo(AttributeTargets.Parameter | AttributeTargets.Field));
        }

        [Test]
        public void InjectAttribute_AllowsMultiple_ShouldBeFalse()
        {
            // Arrange
            var attributeType = typeof(InjectAttribute);
            
            // Act
            var attributeUsage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();
            
            // Assert
            Assert.IsNotNull(attributeUsage);
            Assert.IsFalse(attributeUsage.AllowMultiple);
        }

        [Test]
        public void InjectAttribute_Inherited_ShouldBeFalse()
        {
            // Arrange
            var attributeType = typeof(InjectAttribute);
            
            // Act
            var attributeUsage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();
            
            // Assert
            Assert.IsNotNull(attributeUsage);
            Assert.IsFalse(attributeUsage.Inherited);
        }

        #endregion

        #region Parameter Application Tests

        [Test]
        public void InjectAttribute_CanBeAppliedToConstructorParameter()
        {
            // Arrange
            var constructorParameterTestType = typeof(ConstructorParameterTest);
            var constructor = constructorParameterTestType.GetConstructors()[0];
            var parameter = constructor.GetParameters()[0];
            
            // Act
            var injectAttribute = parameter.GetCustomAttribute<InjectAttribute>();
            
            // Assert
            Assert.IsNotNull(injectAttribute);
        }

        [Test]
        public void InjectAttribute_CanBeAppliedToMultipleConstructorParameters()
        {
            // Arrange
            var multipleParameterTestType = typeof(MultipleParameterTest);
            var constructor = multipleParameterTestType.GetConstructors()[0];
            var parameters = constructor.GetParameters();
            
            // Act
            var firstParamAttribute = parameters[0].GetCustomAttribute<InjectAttribute>();
            var secondParamAttribute = parameters[1].GetCustomAttribute<InjectAttribute>();
            var thirdParamAttribute = parameters[2].GetCustomAttribute<InjectAttribute>();
            
            // Assert
            Assert.IsNotNull(firstParamAttribute);
            Assert.IsNotNull(secondParamAttribute);
            Assert.IsNull(thirdParamAttribute); // This parameter shouldn't have the attribute
        }

        #endregion

        #region Field Application Tests

        [Test]
        public void InjectAttribute_CanBeAppliedToField()
        {
            // Arrange
            var fieldTestType = typeof(FieldTest);
            var field = fieldTestType.GetField("_injectedService", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            var injectAttribute = field.GetCustomAttribute<InjectAttribute>();
            
            // Assert
            Assert.IsNotNull(injectAttribute);
        }

        [Test]
        public void InjectAttribute_CanBeAppliedToMultipleFields()
        {
            // Arrange
            var multipleFieldTestType = typeof(MultipleFieldTest);
            var injectedField = multipleFieldTestType.GetField("_injectedService", BindingFlags.NonPublic | BindingFlags.Instance);
            var anotherInjectedField = multipleFieldTestType.GetField("_anotherInjectedService", BindingFlags.NonPublic | BindingFlags.Instance);
            var nonInjectedField = multipleFieldTestType.GetField("_nonInjectedService", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            var injectedAttribute = injectedField.GetCustomAttribute<InjectAttribute>();
            var anotherInjectedAttribute = anotherInjectedField.GetCustomAttribute<InjectAttribute>();
            var nonInjectedAttribute = nonInjectedField.GetCustomAttribute<InjectAttribute>();
            
            // Assert
            Assert.IsNotNull(injectedAttribute);
            Assert.IsNotNull(anotherInjectedAttribute);
            Assert.IsNull(nonInjectedAttribute);
        }

        #endregion

        #region Mixed Application Tests

        [Test]
        public void InjectAttribute_CanBeAppliedToBothParametersAndFields()
        {
            // Arrange
            var mixedTestType = typeof(MixedInjectionService);
            var constructor = mixedTestType.GetConstructors()[0];
            var parameter = constructor.GetParameters()[0];
            var field = mixedTestType.GetField("_fieldService", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            var parameterAttribute = parameter.GetCustomAttribute<InjectAttribute>();
            var fieldAttribute = field.GetCustomAttribute<InjectAttribute>();
            
            // Assert
            Assert.IsNotNull(parameterAttribute);
            Assert.IsNotNull(fieldAttribute);
        }

        #endregion

        #region JetBrains Annotations Tests

        [Test]
        public void InjectAttribute_HasMeansImplicitUseAttribute()
        {
            // Arrange
            var attributeType = typeof(InjectAttribute);
            
            // Act
            var meansImplicitUseAttributes = attributeType.GetCustomAttributes<Attribute>();
            
            // Assert
            var hasMeansImplicitUse = false;
            foreach (var attr in meansImplicitUseAttributes)
            {
                if (attr.GetType().Name == "MeansImplicitUseAttribute")
                {
                    hasMeansImplicitUse = true;
                    break;
                }
            }
            
            Assert.IsTrue(hasMeansImplicitUse, "InjectAttribute should have MeansImplicitUseAttribute");
        }

        #endregion

        #region Reflection Integration Tests

        [Test]
        public void InjectAttribute_CanBeDetectedViaReflection()
        {
            // Arrange
            var testType = typeof(ReflectionTest);
            var constructor = testType.GetConstructors()[0];
            
            // Act
            var hasInjectParameters = false;
            foreach (var parameter in constructor.GetParameters())
            {
                if (parameter.GetCustomAttribute<InjectAttribute>() != null)
                {
                    hasInjectParameters = true;
                    break;
                }
            }
            
            // Assert
            Assert.IsTrue(hasInjectParameters);
        }

        [Test]
        public void InjectAttribute_CanFilterParametersViaReflection()
        {
            // Arrange
            var testType = typeof(FilterTest);
            var constructor = testType.GetConstructors()[0];
            var parameters = constructor.GetParameters();
            
            // Act
            var injectParameters = new List<ParameterInfo>();
            var nonInjectParameters = new List<ParameterInfo>();
            
            foreach (var parameter in parameters)
            {
                if (parameter.GetCustomAttribute<InjectAttribute>() != null)
                {
                    injectParameters.Add(parameter);
                }
                else
                {
                    nonInjectParameters.Add(parameter);
                }
            }
            
            // Assert
            Assert.AreEqual(2, injectParameters.Count);
            Assert.AreEqual(1, nonInjectParameters.Count);
            Assert.AreEqual(typeof(ITestService), injectParameters[0].ParameterType);
            Assert.AreEqual(typeof(IDependentService), injectParameters[1].ParameterType);
            Assert.AreEqual(typeof(string), nonInjectParameters[0].ParameterType);
        }

        #endregion

        #region Test Classes

        internal class ConstructorParameterTest
        {
            public ConstructorParameterTest([Inject] ITestService testService)
            {

            }
        }

        internal class MultipleParameterTest
        {
            public MultipleParameterTest([Inject] ITestService testService, [Inject] IDependentService dependentService, string regularParameter)
            {
            }
        }

        internal class FieldTest
        {
            [Inject]
            private ITestService _injectedService;
        }

        internal class MultipleFieldTest
        {
            [Inject]
            private ITestService _injectedService;
            
            [Inject]
            private IDependentService _anotherInjectedService;
            
            private IComplexService _nonInjectedService;
        }

        internal class ReflectionTest
        {
            public ReflectionTest([Inject] ITestService testService, string regularParam)
            {
                
            }
        }

        internal class FilterTest
        {
            public FilterTest([Inject] ITestService testService, [Inject] IDependentService dependentService, string regularParam)
            {
            }
        }

        #endregion
    }
}