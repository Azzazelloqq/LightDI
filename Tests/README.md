# LightDI Tests 🧪

Comprehensive test suite for the LightDI dependency injection library.

## 📁 Test Structure

```
Tests/
├── LightDI.Tests.asmdef          # Assembly definition for tests
├── README.md                     # This file
├── TestUtilities/                # Shared test helpers and mock classes
│   ├── MockClasses.cs           # Mock interfaces and implementations
│   └── TestHelper.cs            # Utility methods for test setup
├── Runtime/                      # Tests for core runtime components
│   ├── LightDiContainerTests.cs # Main container functionality tests
│   ├── DiContainerProviderTests.cs # Global provider tests
│   ├── DiContainerFactoryTests.cs # Container factory tests
│   ├── RegistrationInfoTests.cs # Service registration data tests
│   ├── InjectAttributeTests.cs  # Injection attribute tests
│   └── LifetimeTests.cs         # Service lifetime enum tests
├── Integration/                  # Integration and end-to-end tests
│   └── IntegrationTests.cs      # Complex scenario tests
└── Compatibility/               # Unity and compatibility tests
    └── CompatibilityTests.cs    # Unity-specific compatibility tests
```

## 🎯 Test Coverage

### Core Components Tested

1. **LightDiContainer** (240+ assertions)
   - Singleton registration (lazy and instance)
   - Transient registration
   - Service resolution (Resolve and TryResolve)
   - Disposal and lifecycle management
   - Exception handling
   - Type mismatch scenarios

2. **DiContainerProvider** (150+ assertions)
   - Global container registry
   - Multi-container resolution
   - Container registration/unregistration
   - Global dispose functionality

3. **DiContainerFactory** (100+ assertions)
   - Container creation
   - Automatic global registration
   - Dispose callback setup
   - Parameter handling

4. **RegistrationInfo** (80+ assertions)
   - Factory function storage
   - Instance caching
   - Lifetime management
   - Constructor variations

5. **InjectAttribute** (60+ assertions)
   - Attribute usage validation
   - Parameter and field application
   - Reflection integration
   - JetBrains annotations

6. **Lifetime Enum** (40+ assertions)
   - Enum values and conversion
   - Serialization support
   - Switch statement compatibility

### Test Categories

#### 🧪 Unit Tests
- Individual component testing
- Isolated functionality verification
- Mock-based dependency testing
- Edge case validation

#### 🔗 Integration Tests
- Multi-component interactions
- Complex dependency chains
- Real-world scenario simulation
- Performance characteristics

#### 🛠️ Compatibility Tests
- Unity domain reload scenarios
- Example code validation
- Obsolete API functionality
- Memory management
- Thread safety basics

## 🚀 Running Tests

### In Unity Editor

1. Open **Window > General > Test Runner**
2. Select **PlayMode** or **EditMode** tab
3. Click **Run All** or select specific test classes
4. View results in the Test Runner window

### Via Command Line

```bash
# Run all tests
Unity.exe -batchmode -runTests -testPlatform EditMode -testResults results.xml -projectPath /path/to/project

# Run specific test assembly
Unity.exe -batchmode -runTests -testPlatform EditMode -testCategory "LightDI.Tests" -projectPath /path/to/project
```

## 📊 Test Utilities

### Mock Classes

The test suite includes comprehensive mock implementations:

- **ITestService/TestService**: Basic service interface and implementation
- **DisposableTestService**: Service implementing IDisposable for disposal testing
- **DependentService**: Service with dependencies for injection testing
- **ComplexService**: Service with multiple dependencies
- **SingletonTestService/TransientTestService**: Services for lifetime testing
- **FailingService**: Service that throws exceptions for error testing

### Test Helpers

**TestHelper** class provides:
- Container creation utilities
- Service registration shortcuts
- Global provider cleanup
- Exception assertion methods

## 🧩 Test Patterns

### SetUp/TearDown Pattern
```csharp
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
```

### Exception Testing Pattern
```csharp
var exception = TestHelper.AssertThrows<ExpectedException>(() => 
    methodThatShouldThrow());

Assert.That(exception.Message, Contains.Substring("expected text"));
```

### Disposal Testing Pattern
```csharp
var disposableService = new DisposableTestService("test");
container.RegisterAsSingleton(disposableService);
container.Dispose();
Assert.IsTrue(disposableService.IsDisposed);
```

## 📋 Test Checklist

When adding new tests, ensure:

- ✅ **Arrange-Act-Assert** pattern
- ✅ **Descriptive test names** that explain intent
- ✅ **Proper cleanup** in TearDown methods
- ✅ **Isolated tests** that don't depend on execution order
- ✅ **Both positive and negative** test cases
- ✅ **Edge cases** and boundary conditions
- ✅ **Documentation** for complex test scenarios

## 🔍 Testing Guidelines

### Naming Convention
```
MethodUnderTest_StateUnderTest_ExpectedBehavior()
```

Examples:
- `RegisterAsSingleton_WithInstance_ShouldReturnSameInstance()`
- `Resolve_UnregisteredService_ShouldThrowException()`
- `Dispose_WithDisposableServices_ShouldDisposeAllServices()`

### Test Organization
- Group related tests in regions
- Start with happy path scenarios
- Include error cases and edge conditions
- End with integration scenarios

### Assert Patterns
```csharp
// Existence checks
Assert.IsNotNull(result);
Assert.IsInstanceOf<ExpectedType>(result);

// Equality checks  
Assert.AreEqual(expected, actual);
Assert.AreSame(expectedInstance, actualInstance);

// Collection checks
Assert.AreEqual(expectedCount, collection.Count);
Assert.Contains(expectedItem, collection);

// Exception checks
Assert.Throws<ExpectedException>(() => methodCall());
Assert.DoesNotThrow(() => methodCall());
```

## 🎯 Coverage Goals

- **Unit Test Coverage**: 95%+ of public methods
- **Integration Coverage**: All major usage scenarios
- **Error Coverage**: All exception paths
- **Edge Case Coverage**: Boundary conditions and edge cases

## 🐛 Debugging Tests

### Common Issues
1. **Global state pollution**: Use proper cleanup in TearDown
2. **Container leaks**: Always dispose containers
3. **Race conditions**: Use proper synchronization in concurrent tests
4. **Memory leaks**: Verify disposal of disposable services

### Debug Tips
- Use `Debug.Log()` for runtime inspection
- Set breakpoints in test methods
- Check Unity Console for error messages
- Use Unity Test Runner filters to isolate failing tests

## 📈 Performance Considerations

- Tests should complete quickly (< 1 second each)
- Avoid unnecessary object creation
- Use mocks instead of real implementations when possible
- Clean up resources to prevent memory bloat

## 🔄 Continuous Integration

For CI/CD pipelines:
- Tests run in headless mode
- Results exported to XML format
- Failed tests cause build failures
- Coverage reports generated automatically

---

**Happy Testing! 🧪✨**

These tests ensure LightDI works reliably in all scenarios and maintains high quality standards.