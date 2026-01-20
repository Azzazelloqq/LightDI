# LightDI 🚀

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)  
[![GitHub release (latest by SemVer)](https://img.shields.io/github/release/Azzazelloqq/lightdi.svg?style=flat-square&cacheSeconds=86400)](https://github.com/Azzazelloqq/LightDI/releases)

LightDI is a lightweight, high-performance dependency injection container for .NET and Unity projects. It leverages Roslyn Source Generators to produce compile-time factories for constructor injection—**eliminating runtime reflection overhead** and delivering superior performance. While you can access the container directly via the `DiContainerProvider`, it is **strongly recommended** to use the generated factories and constructor injection for optimal efficiency.

> **Performance Note:**  
> Using the `[Inject]` attribute on constructor parameters ensures dependencies are resolved **without runtime reflection**, delivering a significant performance boost compared to field injection.

---

## ✨ Key Features

- **Compile-Time Injection:**  
  Automatically generate type-safe factories for classes decorated with `[Inject]`. Constructor injection is handled at compile time—no runtime reflection required!

- **Flexible Lifetime Management:**  
  Register services as either **Singletons** (one instance per container, lazily instantiated) or **Transients** (a new instance is created on every resolve).

- **Automatic Disposal:**  
  The container implements `IDisposable` and disposes all registered services implementing `IDisposable` when the container is disposed.

- **Global Container Management:**  
  Containers created via `DiContainerFactory` automatically register with the global `DiContainerProvider`.  
  > **Warning:** Direct calls to `DiContainerProvider.Resolve<T>()` should be used **only in critical cases**.

---

## 📦 Project Structure

```plaintext
LightDI/
├── LightDI.Runtime            // Core DI container implementation
│   ├── LightDiContainer.cs    // Main container logic and lifetime management
│   ├── IDiContainer.cs        // Container interface definition
│   ├── RegistrationInfo.cs    // Internal registration storage for services
│   ├── Lifetime.cs            // Lifetime enum (Singleton, Transient)
│   └── InjectAttribute.cs     // [Inject] attribute definition for DI
├── LightDI.SourceGenerators   // Roslyn Source Generator for compile-time factory creation
│   └── InjectSourceGenerator.cs
└── LightDI.Example            // Example usage and test classes
    ├── CompositionRoot.cs     // Entry point for container initialization
    ├── GameManager.cs         // Sample class demonstrating constructor and field injection
    ├── IServiceA.cs           // Sample service interface
    ├── IWeapon.cs             // Sample weapon interface
    ├── ServiceA.cs            // Implementation of IServiceA
    └── Sword.cs               // Implementation of IWeapon
```
---

## 🚀 Quick Start

### 1. Container Initialization

Use the `DiContainerFactory` to create a container that automatically registers itself with the global provider:

```csharp
using LightDI.Runtime;

public class CompositionRoot
{
    public void Enter()
    {
        // Create a new container and register it with DiContainerProvider.
        // Use a scope that matches the namespace of classes created by generated factories.
        var container = DiContainerFactory.CreateContainer("LightDI.Example");

        // Register your dependencies.
        container.RegisterAsSingleton<IServiceA>(() => new ServiceA());
        container.RegisterAsSingleton<IWeapon>(() => new Sword());

        // Create your GameManager using the generated factory.
        // Constructor injection (with [Inject]) avoids reflection for optimal performance.
        var gameManager = GameManagerFactory.CreateGameManager(10);
        gameManager.RunGame();
    }
}


```

> **Scope Note:**  
> Generated factories resolve dependencies using the class namespace.  
> For multi-container setups, create containers with matching namespace scopes (or object scopes) to avoid cross-module resolution.  
> `Resolve<T>()` without a scope works only when a single container is registered.

Object scopes are also supported:
```csharp
var scopeOwner = new ModuleRoot();
var container = DiContainerFactory.CreateContainer(scopeOwner);
```

## 2. Registering Services

LightDI offers a straightforward approach to service registration, allowing you to specify whether a service should behave as a **Singleton** or as a **Transient**. Below you'll find examples that showcase how to register your services in a clean and efficient manner.

### 🔒 Singleton Registration

Register your services using simple methods:

```csharp
// Register as Singleton (a single instance is returned on every resolve)
container.RegisterAsSingleton<IMyService>(() => new MyService());

// Register an already created instance as Singleton
var existingService = new ExistingService();
container.RegisterAsSingleton<ExistingService>(existingService);

// Register as Transient (a new instance is created each time)
container.RegisterAsTransient<ILogic>(() => new SomeLogic());
```

## 3. Using `[Inject]` for Dependency Injection

LightDI leverages the `[Inject]` attribute to automatically inject dependencies into your classes. When you mark constructor parameters or fields with `[Inject]`, the Source Generator creates a factory that resolves these dependencies at compile time. This means:

- **Constructor Injection** avoids runtime reflection entirely, offering excellent performance.
- **Field Injection** is performed via reflection only when the dependency is not provided through the constructor.

### 🚀 Constructor Injection

When you decorate constructor parameters with `[Inject]`, LightDI generates a factory method that creates your object with its dependencies already provided—**without using reflection**. This leads to a significant performance boost.

```csharp
public class GameManager
{
    private readonly IServiceA _serviceA;
    private readonly int _runtimeValue;

    // Constructor injection: dependencies are provided directly.
    public GameManager([Inject] IServiceA serviceA, int runtimeValue)
    {
        _serviceA = serviceA;
        _runtimeValue = runtimeValue;
    }

    public void RunGame()
    {
        _serviceA.DoSomething();
        UnityEngine.Debug.Log($"GameManager running with runtime value: {_runtimeValue}");
    }
}
```

### 💥 Field Injection

If a dependency is not provided via the constructor, you can use field injection by applying [Inject] to a field. In this case, the generated factory injects the dependency via reflection. However, for performance reasons, constructor injection is always preferred.
```csharp
public class GameManager
{
    private readonly IServiceA _serviceA;
    private readonly int _runtimeValue;

    // Constructor injection for critical dependencies.
    public GameManager([Inject] IServiceA serviceA, int runtimeValue)
    {
        _serviceA = serviceA;
        _runtimeValue = runtimeValue;
    }

    // Field injection for additional dependencies.
    [Inject]
    private IWeapon _weapon;

    public void RunGame()
    {
        _serviceA.DoSomething();
        _weapon?.Attack();
        UnityEngine.Debug.Log($"GameManager running with runtime value: {_runtimeValue}");
    }
}
```

### 🔥 Example of a Generated Factory

A typical generated factory (e.g., GameManagerFactory.g.cs) might look like this:
```csharp
public static class GameManagerFactory
{
    private const string __namespaceScope = "LightDI.Example";

    public static GameManager CreateGameManager(int runtimeValue)
    {
        // Resolve constructor dependency without reflection.
        var serviceA = DiContainerProvider.Resolve<IServiceA>(__namespaceScope);
        var instance = new GameManager(serviceA, runtimeValue);
        
        // Field injection is performed via reflection if necessary.
        instance._weapon = DiContainerProvider.Resolve<IWeapon>(__namespaceScope);
        
        return instance;
    }
}
```

> **⚡ Performance Tip:**  
> Prefer **constructor injection** over field injection whenever possible—constructor injection avoids runtime reflection and results in **faster resolution**.
>
> **🚀 Efficiency Boost:**  
> Embrace the power of `[Inject]` in LightDI to achieve **clean, efficient dependency management** with minimal overhead!

## 4. Direct Resolution (Discouraged) 🚫

While you **can** resolve services directly using `DiContainerProvider.Resolve<T>(scope)`, this approach is intended only for **critical or internal cases**. For everyday use, please rely on the generated factories and constructor injection to maintain performance and clarity.

```csharp
var service = DiContainerProvider.Resolve<IServiceA>("LightDI.Example");
```

If you want to avoid passing the scope repeatedly, use a scoped block:
```csharp
using (DiContainerProvider.BeginScope("LightDI.Example"))
{
    var service = DiContainerProvider.Resolve<IServiceA>();
}
```

> **💡 Tip: Direct resolution bypasses the benefits of compile-time injection, so it should be avoided in favor of using the generated factories.**

### 5. Disposal 🗑️

When your container is no longer needed (for example, when a scene is unloaded in Unity), make sure to call Dispose() on the container. This will:
- Dispose all registered services that implement IDisposable.
- Unregister the container from the global provider.
```charp
container.Dispose();
```
> **Remember: Proper disposal is key for managing resources and ensuring your application remains efficient.**

### ⚠️ Known Limitations

- Internal Access:
Generated factories are produced in a separate assembly, so accessing internal services may lead to visibility issues.
Recommendation: Either use public types or add an appropriate [InternalsVisibleTo] attribute in your assembly.

- Direct Resolve Usage:
Direct calls via DiContainerProvider.Resolve<T>(scope) are available but are discouraged for regular use. Always prefer the compile-time generated factories and constructor injection.

- MonoBehaviour Support:
LightDI currently does not support dependency injection for MonoBehaviour classes. For Unity-specific DI with MonoBehaviours, consider using frameworks like Zenject or VContainer.

### ⚠️ Important Note for Unity Projects with Domain Reload Disabled

When domain reload is disabled in Unity, static data—such as the global container registry in `DiContainerProvider`—may persist between scene loads. This persistence can lead to the accumulation of outdated or unnecessary data and potential memory leaks.

> **Tip:**  
> To maintain a clean state between scenes or during application shutdown, **always call** `DiContainerProvider.Dispose()` to clear the static data held by the global container registry.

### Example

In your scene transition or shutdown code, add the following call:

```csharp
// For example, during scene unload or application exit:
DiContainerProvider.Dispose();
```

### 📚 Contributing

Contributions, issues, and feature requests are warmly welcome!
Please visit our [Issues](https://github.com/Azzazelloqq/LightDI/issues) page or submit a pull request with your suggestions and improvements.

### 📄 License

This project is licensed under the MIT License.
See the [LICENSE](LICENSE) file for full details.
