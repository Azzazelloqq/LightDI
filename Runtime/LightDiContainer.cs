using System;
using System.Collections.Generic;

namespace LightDI.Runtime
{
/// <summary>
/// A lightweight dependency injection container implementation.
/// Supports registration of services as Singleton (lazy) and Transient.
/// </summary>
internal class LightDiContainer : IDiContainer
{
#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
	[ThreadStatic]
	private static List<Type> _resolutionStack;
#endif

	private readonly bool _disposeRegistered;
	private readonly Dictionary<Type, RegistrationInfo> _registrations = new();
	private readonly List<IDisposable> _disposables = new();
	private bool _disposed;
	private Action _onDispose;

	/// <summary>
	/// Initializes a new instance of the <see cref="LightDiContainer"/> class.
	/// </summary>
	/// <param name="disposeRegistered">If true, disposable services are disposed when the container is disposed.</param>
	internal LightDiContainer(bool disposeRegistered = true)
	{
		_disposeRegistered = disposeRegistered;
	}

	/// <summary>
	/// Registers a service with the specified factory and lifetime.
	/// This method is used internally by the container.
	/// </summary>
	/// <typeparam name="T">The service type.</typeparam>
	/// <param name="factory">A factory function to create the service.</param>
	/// <param name="lifetime">The lifetime of the service.</param>
	internal void Register<T>(Func<T> factory, Lifetime lifetime) where T : class
	{
		var type = typeof(T);
		var reg = new RegistrationInfo(
			_ => factory(),
			lifetime
		);
		
		_registrations[type] = reg;
	}
	
	/// <inheritdoc/>
	public void RegisterAsSingletonLazy<T>(Func<T> factory) where T : class
	{
		var type = typeof(T);
		var reg = new RegistrationInfo(
			_ => factory(),
			Lifetime.Singleton
		);
		
		_registrations[type] = reg;
	}

	/// <inheritdoc/>
	public void RegisterAsSingleton<T>(T singleton) where T : class
	{
		var type = typeof(T);
		var reg = new RegistrationInfo(
			singleton,
			Lifetime.Singleton
		);
		
		_registrations[type] = reg;
		HandleNewlyCreated(singleton);
	}

	/// <inheritdoc/>
	public void RegisterAsTransient<T>(Func<T> factory) where T : class
	{
		var type = typeof(T);
		var reg = new RegistrationInfo(
			_ => factory(),
			Lifetime.Transient
		);
		
		_registrations[type] = reg;
	}

	/// <inheritdoc/>
	bool IDiContainer.TryResolve<T>(out T instance)
	{
		if (_disposed)
		{
			instance = null;
			return false;
		}

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
		using (EnterResolutionScope(typeof(T)))
		{
#endif
		
		var type = typeof(T);
		if (!_registrations.TryGetValue(type, out var registrationInfo))
		{
			instance = null;
			return false;
		}
		
		switch (registrationInfo.Lifetime)
		{
			case Lifetime.Transient:
				instance =  TryResolveTransient<T>(registrationInfo);
				return true;
			case Lifetime.Singleton:
				 instance = ResolveSingleton<T>(registrationInfo);
				return true;
			default:
				throw new ArgumentOutOfRangeException();
		}
#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
		}
#endif
	}

	/// <inheritdoc/>
	T IDiContainer.Resolve<T>() where T : class
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(nameof(LightDiContainer),
				"Cannot resolve from a disposed container.");
		}

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
		using (EnterResolutionScope(typeof(T)))
		{
#endif
		var type = typeof(T);
		if (!_registrations.TryGetValue(type, out var registrationInfo))
		{
			throw new Exception($"Service of type {type.FullName} is not registered.");
		}

		switch (registrationInfo.Lifetime)
		{
			case Lifetime.Transient:
				return TryResolveTransient<T>(registrationInfo);
			case Lifetime.Singleton:
				return ResolveSingleton<T>(registrationInfo);
			default:
				throw new ArgumentOutOfRangeException();
		}
#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
		}
#endif
	}
	
	/// <summary>
	/// Subscribes to the container's dispose callback.
	/// The provided action will be invoked when the container is disposed.
	/// </summary>
	/// <param name="onDispose">The callback to invoke upon disposal.</param>
	internal void SubscribeOnDisposeCallback(Action onDispose)
	{
		if (_disposed)
		{
			throw new Exception("Cannot subscribe to dispose callback on a disposed container.");
		}
		
		_onDispose = onDispose;
	}
	
	/// <summary>
	/// Subscribes to the container's dispose callback.
	/// The provided action will be invoked when the container is disposed.
	/// </summary>
	/// <param name="onDispose">The callback to invoke upon disposal.</param>
	private T ResolveSingleton<T>(RegistrationInfo registrationInfo) where T : class
	{
		var cachedInstance = registrationInfo.CachedInstance;
		
		if (cachedInstance == null)
		{
			var instance = registrationInfo.Factory(this);
			registrationInfo.CachedInstance = instance;
			HandleNewlyCreated(instance);
		}

		if (registrationInfo.CachedInstance is T concreteInstance)
		{
			return concreteInstance;
		}
		
		throw new Exception($"Dependency type mismatch. You tried to resolve dependency as type {typeof(T).FullName} " +
							$"but created dependency is of type {registrationInfo.CachedInstance.GetType().FullName}. " +
							$"Maybe you forgot to register it or registered it with a different type.");
	}
	
	/// <summary>
	/// Resolves a transient service by creating a new instance and handling it if it implements IDisposable.
	/// </summary>
	private T TryResolveTransient<T>(RegistrationInfo registrationInfo) where T : class
	{
		var instance = registrationInfo.Factory(this);
		HandleNewlyCreated(instance);
			
		if (instance is T concreteInstance)
		{
			return concreteInstance;
		}

		throw new Exception($"Dependency type mismatch. You tried to resolve dependency as type {typeof(T).FullName} " +
							$"but created dependency is of type {instance.GetType().FullName}. " +
							$"Maybe you forgot to register it or registered it with a different type.");
	}

	/// <summary>
	/// If a newly created instance implements IDisposable, it is added to the disposables list.
	/// </summary>
	private void HandleNewlyCreated(object obj)
	{
		if (!_disposeRegistered)
		{
			return;
		}
		
		if (obj is IDisposable disposable)
		{
			_disposables.Add(disposable);
		}
	}

	/// <summary>
	/// Disposes the container by calling Dispose() on all registered disposable services,
	/// clearing all registrations, and invoking the dispose callback.
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		if (_disposeRegistered)
		{
			foreach (var disposable in _disposables)
			{
				disposable.Dispose();
			}
			
			_disposables.Clear();
		}

		_registrations.Clear();
		
		_disposed = true;
		
		_onDispose?.Invoke();
	}

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
	private static ResolutionScope EnterResolutionScope(Type type)
	{
		if (_resolutionStack == null)
		{
			_resolutionStack = new List<Type>(8);
		}

		for (int i = 0; i < _resolutionStack.Count; i++)
		{
			if (_resolutionStack[i] == type)
			{
				throw new InvalidOperationException($"Circular dependency detected while resolving {type.FullName}.");
			}
		}

		_resolutionStack.Add(type);
		return new ResolutionScope(_resolutionStack);
	}

	private readonly struct ResolutionScope : IDisposable
	{
		private readonly List<Type> _stack;

		public ResolutionScope(List<Type> stack)
		{
			_stack = stack;
		}

		public void Dispose()
		{
			if (_stack.Count > 0)
			{
				_stack.RemoveAt(_stack.Count - 1);
			}
		}
	}
#endif
}
}