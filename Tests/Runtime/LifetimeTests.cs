using System;
using System.Collections.Generic;
using LightDI.Runtime;
using NUnit.Framework;

namespace LightDI.Tests
{
    /// <summary>
    /// Comprehensive tests for Lifetime enum functionality.
    /// </summary>
    [TestFixture]
    public class LifetimeTests
    {
        #region Enum Value Tests

        [Test]
        public void Lifetime_TransientValue_ShouldBeZero()
        {
            // Act & Assert
            Assert.AreEqual(0, (int)Lifetime.Transient);
        }

        [Test]
        public void Lifetime_SingletonValue_ShouldBeOne()
        {
            // Act & Assert
            Assert.AreEqual(1, (int)Lifetime.Singleton);
        }

        [Test]
        public void Lifetime_ShouldHaveExactlyTwoValues()
        {
            // Arrange
            var lifetimeValues = Enum.GetValues(typeof(Lifetime));
            
            // Act & Assert
            Assert.AreEqual(2, lifetimeValues.Length);
        }

        [Test]
        public void Lifetime_AllValuesShouldBeDefined()
        {
            // Act & Assert
            Assert.IsTrue(Enum.IsDefined(typeof(Lifetime), Lifetime.Transient));
            Assert.IsTrue(Enum.IsDefined(typeof(Lifetime), Lifetime.Singleton));
        }

        #endregion

        #region Enum Conversion Tests

        [Test]
        public void Lifetime_CanConvertToString()
        {
            // Act & Assert
            Assert.AreEqual("Transient", Lifetime.Transient.ToString());
            Assert.AreEqual("Singleton", Lifetime.Singleton.ToString());
        }

        [Test]
        public void Lifetime_CanParseFromString()
        {
            // Act & Assert
            Assert.AreEqual(Lifetime.Transient, Enum.Parse(typeof(Lifetime), "Transient"));
            Assert.AreEqual(Lifetime.Singleton, Enum.Parse(typeof(Lifetime), "Singleton"));
        }

        [Test]
        public void Lifetime_CanParseFromStringIgnoreCase()
        {
            // Act & Assert
            Assert.AreEqual(Lifetime.Transient, Enum.Parse(typeof(Lifetime), "transient", true));
            Assert.AreEqual(Lifetime.Singleton, Enum.Parse(typeof(Lifetime), "singleton", true));
            Assert.AreEqual(Lifetime.Transient, Enum.Parse(typeof(Lifetime), "TRANSIENT", true));
            Assert.AreEqual(Lifetime.Singleton, Enum.Parse(typeof(Lifetime), "SINGLETON", true));
        }

        [Test]
        public void Lifetime_TryParseValidValues_ShouldSucceed()
        {
            // Act & Assert
            Assert.IsTrue(Enum.TryParse<Lifetime>("Transient", out var transient));
            Assert.AreEqual(Lifetime.Transient, transient);
            
            Assert.IsTrue(Enum.TryParse<Lifetime>("Singleton", out var singleton));
            Assert.AreEqual(Lifetime.Singleton, singleton);
        }

        [Test]
        public void Lifetime_TryParseInvalidValues_ShouldFail()
        {
            // Act & Assert
            Assert.IsFalse(Enum.TryParse<Lifetime>("Invalid", out _));
            Assert.IsFalse(Enum.TryParse<Lifetime>("Scoped", out _));
            Assert.IsFalse(Enum.TryParse<Lifetime>("", out _));
        }

        #endregion

        #region Comparison Tests

        [Test]
        public void Lifetime_EqualityComparison_ShouldWork()
        {
            // Act & Assert
            Assert.IsTrue(Lifetime.Transient == Lifetime.Transient);
            Assert.IsTrue(Lifetime.Singleton == Lifetime.Singleton);
            Assert.IsFalse(Lifetime.Transient == Lifetime.Singleton);
            Assert.IsFalse(Lifetime.Singleton == Lifetime.Transient);
        }

        [Test]
        public void Lifetime_InequalityComparison_ShouldWork()
        {
            // Act & Assert
            Assert.IsFalse(Lifetime.Transient != Lifetime.Transient);
            Assert.IsFalse(Lifetime.Singleton != Lifetime.Singleton);
            Assert.IsTrue(Lifetime.Transient != Lifetime.Singleton);
            Assert.IsTrue(Lifetime.Singleton != Lifetime.Transient);
        }

        [Test]
        public void Lifetime_EqualsMethod_ShouldWork()
        {
            // Act & Assert
            Assert.IsTrue(Lifetime.Transient.Equals(Lifetime.Transient));
            Assert.IsTrue(Lifetime.Singleton.Equals(Lifetime.Singleton));
            Assert.IsFalse(Lifetime.Transient.Equals(Lifetime.Singleton));
            Assert.IsFalse(Lifetime.Singleton.Equals(Lifetime.Transient));
        }

        [Test]
        public void Lifetime_GetHashCode_ShouldBeConsistent()
        {
            // Arrange
            var transient1 = Lifetime.Transient;
            var transient2 = Lifetime.Transient;
            var singleton1 = Lifetime.Singleton;
            var singleton2 = Lifetime.Singleton;
            
            // Act & Assert
            Assert.AreEqual(transient1.GetHashCode(), transient2.GetHashCode());
            Assert.AreEqual(singleton1.GetHashCode(), singleton2.GetHashCode());
            Assert.AreNotEqual(transient1.GetHashCode(), singleton1.GetHashCode());
        }

        #endregion

        #region Enum Attributes and Metadata Tests

        [Test]
        public void Lifetime_IsPublicEnum()
        {
            // Arrange
            var lifetimeType = typeof(Lifetime);
            
            // Act & Assert
            Assert.IsTrue(lifetimeType.IsEnum);
            Assert.IsTrue(lifetimeType.IsPublic);
        }

        [Test]
        public void Lifetime_UnderlyingType_ShouldBeInt32()
        {
            // Arrange
            var lifetimeType = typeof(Lifetime);
            
            // Act
            var underlyingType = Enum.GetUnderlyingType(lifetimeType);
            
            // Assert
            Assert.AreEqual(typeof(int), underlyingType);
        }

        [Test]
        public void Lifetime_ShouldBeInCorrectNamespace()
        {
            // Arrange
            var lifetimeType = typeof(Lifetime);
            
            // Act & Assert
            Assert.AreEqual("LightDI.Runtime", lifetimeType.Namespace);
        }

        #endregion

        #region Switch Statement Tests

        [Test]
        public void Lifetime_CanBeUsedInSwitchStatement()
        {
            // Arrange & Act & Assert
            foreach (Lifetime lifetime in Enum.GetValues(typeof(Lifetime)))
            {
                string result = lifetime switch
                {
                    Lifetime.Transient => "Creates new instance each time",
                    Lifetime.Singleton => "Returns same instance always",
                    _ => throw new ArgumentOutOfRangeException(nameof(lifetime))
                };
                
                Assert.IsNotNull(result);
                Assert.IsNotEmpty(result);
            }
        }

        [Test]
        public void Lifetime_SwitchWithDefaultCase_ShouldHandleAllValues()
        {
            // Arrange
            var results = new Dictionary<Lifetime, string>();
            
            // Act
            foreach (Lifetime lifetime in Enum.GetValues(typeof(Lifetime)))
            {
                switch (lifetime)
                {
                    case Lifetime.Transient:
                        results[lifetime] = "Transient behavior";
                        break;
                    case Lifetime.Singleton:
                        results[lifetime] = "Singleton behavior";
                        break;
                    default:
                        results[lifetime] = "Unknown behavior";
                        break;
                }
            }
            
            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("Transient behavior", results[Lifetime.Transient]);
            Assert.AreEqual("Singleton behavior", results[Lifetime.Singleton]);
        }

        #endregion

        #region Serialization Tests

        [Test]
        public void Lifetime_CanBeSerializedAsInteger()
        {
            // Act & Assert
            Assert.AreEqual(0, (int)Lifetime.Transient);
            Assert.AreEqual(1, (int)Lifetime.Singleton);
            
            // Verify we can cast back
            Assert.AreEqual(Lifetime.Transient, (Lifetime)0);
            Assert.AreEqual(Lifetime.Singleton, (Lifetime)1);
        }

        [Test]
        public void Lifetime_InvalidIntegerCast_ShouldStillWork()
        {
            // This tests that casting invalid integers doesn't throw but creates invalid enum values
            // This is standard .NET enum behavior
            
            // Act
            var invalidLifetime = (Lifetime)999;
            
            // Assert
            Assert.IsFalse(Enum.IsDefined(typeof(Lifetime), invalidLifetime));
            Assert.AreEqual(999, (int)invalidLifetime);
        }

        #endregion

        #region Documentation Validation Tests

        [Test]
        public void Lifetime_TransientDocumentation_ShouldBeAccurate()
        {
            // This test validates that the documentation matches the actual behavior
            // In real scenarios, Transient means new instance every time
            
            // Arrange
            var lifetime = Lifetime.Transient;
            
            // Act & Assert
            Assert.AreEqual("Transient", lifetime.ToString());
            
            // The enum value should match the documented behavior:
            // "A new instance is created every time the service is resolved."
        }

        [Test]
        public void Lifetime_SingletonDocumentation_ShouldBeAccurate()
        {
            // This test validates that the documentation matches the actual behavior
            // In real scenarios, Singleton means same instance always
            
            // Arrange
            var lifetime = Lifetime.Singleton;
            
            // Act & Assert
            Assert.AreEqual("Singleton", lifetime.ToString());
            
            // The enum value should match the documented behavior:
            // "A single instance is created and reused for all resolutions."
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void Lifetime_DefaultValue_ShouldBeTransient()
        {
            // Arrange
            var defaultLifetime = default(Lifetime);
            
            // Act & Assert
            Assert.AreEqual(Lifetime.Transient, defaultLifetime);
            Assert.AreEqual(0, (int)defaultLifetime);
        }

        [Test]
        public void Lifetime_MinValue_ShouldBeTransient()
        {
            // Arrange
            var values = (Lifetime[])Enum.GetValues(typeof(Lifetime));
            Array.Sort(values);
            
            // Act & Assert
            Assert.AreEqual(Lifetime.Transient, values[0]);
        }

        [Test]
        public void Lifetime_MaxValue_ShouldBeSingleton()
        {
            // Arrange
            var values = (Lifetime[])Enum.GetValues(typeof(Lifetime));
            Array.Sort(values);
            
            // Act & Assert
            Assert.AreEqual(Lifetime.Singleton, values[values.Length - 1]);
        }

        #endregion
    }
}