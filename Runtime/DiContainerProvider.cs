using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LightDI.Runtime
{
/// <summary>
/// Provides a global registry for DI containers.
/// 
/// <para>
/// <b>Note:</b> Direct resolution using <c>DiContainerProvider.Resolve&lt;T&gt;()</c> is marked obsolete 
/// and should only be used in internal or critical cases.
/// </para>
/// </summary>
public static class DiContainerProvider
{
	private static readonly ConcurrentDictionary<IDiContainer, ContainerRegistration> _containers = new();
	private static readonly ConcurrentDictionary<string, ContainerRegistration> _namespaceScopeIndex =
		new(StringComparer.Ordinal);
	private static readonly ConcurrentDictionary<string, List<ContainerRegistration>> _namespaceChainCache =
		new(StringComparer.Ordinal);
	private static readonly ConcurrentDictionary<object, ContainerRegistration> _objectScopeIndex =
		new(new ReferenceEqualityComparer());
	private static int _containerCount;
	private static ContainerRegistration _singleContainer;
	
	[ThreadStatic]
	private static ScopeFrame _currentScope;

	/// <summary>
	/// Resolves a service of type <typeparamref name="T"/> from the current scope.
	/// This method is marked obsolete; use compile-time injection ([Inject]) instead.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <returns>An instance of the service.</returns>
	/// <exception cref="Exception">Thrown if no container can resolve the service.</exception>
	[Obsolete("Use attribute [Inject]. DiContainer not for regular use. It's for internal use or critical cases only.")]
	public static T Resolve<T>() where T : class
	{
		var scope = _currentScope;
		if (scope != null)
		{
			switch (scope.Kind)
			{
				case ScopeKind.Namespace:
					return Resolve<T>(scope.NamespaceScope);
				case ScopeKind.Object:
					return Resolve<T>(scope.ScopeOwner);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		
		if (TryGetSingleContainer(out var singleRegistration))
		{
			return ResolveFromRegistration<T>(singleRegistration);
		}

		if (_containers.IsEmpty)
		{
			throw new Exception($"Service of type {typeof(T).FullName} is not registered.");
		}

		throw new Exception($"Multiple containers are registered but no scope is set for " +
							$"{typeof(T).FullName}. Use DiContainerProvider.BeginScope(...) or Resolve<T>(scope).");
	}

	/// <summary>
	/// Resolves a service of type <typeparamref name="T"/> from the best matching namespace scope.
	/// This method is marked obsolete; use compile-time injection ([Inject]) instead.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <param name="scope">The namespace scope used for resolution.</param>
	/// <returns>An instance of the service.</returns>
	/// <exception cref="Exception">Thrown if no container can resolve the service.</exception>
	[Obsolete("Use attribute [Inject]. DiContainer not for regular use. It's for internal use or critical cases only.")]
	public static T Resolve<T>(string scope) where T : class
	{
		ValidateNamespaceScope(scope);
		var chain = GetNamespaceChain(scope);

		if (chain.Count == 0)
		{
			if (TryGetSingleContainer(out var singleRegistration))
			{
				return ResolveFromRegistration<T>(singleRegistration);
			}

			throw new Exception($"No container registered for namespace scope '{scope}'.");
		}

		for (int i = 0; i < chain.Count; i++)
		{
			if (chain[i].Container.TryResolve<T>(out var instance))
			{
				return instance;
			}
		}

		throw new Exception($"Service of type {typeof(T).FullName} is not registered for scope '{scope}'.");
	}

	/// <summary>
	/// Resolves a service of type <typeparamref name="T"/> from the provided scope owner.
	/// This method is marked obsolete; use compile-time injection ([Inject]) instead.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <param name="scopeOwner">The object that represents the scope owner.</param>
	/// <returns>An instance of the service.</returns>
	/// <exception cref="Exception">Thrown if no container can resolve the service.</exception>
	[Obsolete("Use attribute [Inject]. DiContainer not for regular use. It's for internal use or critical cases only.")]
	public static T Resolve<T>(object scopeOwner) where T : class
	{
		if (scopeOwner == null)
		{
			throw new ArgumentNullException(nameof(scopeOwner));
		}
		
		if (!_objectScopeIndex.TryGetValue(scopeOwner, out var registration))
		{
			throw new Exception($"No container registered for scope owner '{scopeOwner.GetType().FullName}'.");
		}

		return ResolveFromRegistration<T>(registration);
	}

	/// <summary>
	/// Resolves a service directly from the provided container.
	/// Use this only for performance-critical paths where you already have a container instance.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <param name="container">The container to resolve from.</param>
	/// <returns>An instance of the service.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the container is null.</exception>
	[Obsolete("Use generated factories or scoped Resolve methods. This API is for performance-critical paths only.")]
	public static T ResolveFromContainer<T>(IDiContainer container) where T : class
	{
		if (container == null)
		{
			throw new ArgumentNullException(nameof(container));
		}

		return container.Resolve<T>();
	}

	/// <summary>
	/// Attempts to resolve a service directly from the provided container.
	/// Use this only for performance-critical paths where you already have a container instance.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <param name="container">The container to resolve from.</param>
	/// <param name="instance">The resolved instance if successful.</param>
	/// <returns>True if resolution succeeded; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the container is null.</exception>
	[Obsolete("Use generated factories or scoped Resolve methods. This API is for performance-critical paths only.")]
	public static bool TryResolveFromContainer<T>(IDiContainer container, out T instance) where T : class
	{
		if (container == null)
		{
			throw new ArgumentNullException(nameof(container));
		}

		return container.TryResolve<T>(out instance);
	}

	/// <summary>
	/// Begins a scope for the current thread using the specified namespace.
	/// </summary>
	/// <param name="scope">The namespace scope to use.</param>
	/// <returns>An <see cref="IDisposable"/> that ends the scope when disposed.</returns>
	public static IDisposable BeginScope(string scope)
	{
		ValidateNamespaceScope(scope);
		return PushScope(ScopeKind.Namespace, scope, null);
	}

	/// <summary>
	/// Begins a scope for the current thread using the specified scope owner object.
	/// </summary>
	/// <param name="scopeOwner">The object that represents the scope owner.</param>
	/// <returns>An <see cref="IDisposable"/> that ends the scope when disposed.</returns>
	public static IDisposable BeginScope(object scopeOwner)
	{
		if (scopeOwner == null)
		{
			throw new ArgumentNullException(nameof(scopeOwner));
		}
		
		return PushScope(ScopeKind.Object, null, scopeOwner);
	}
	
	/// <summary>
	/// Disposes the global container registry.
	/// Call this to clear static data (e.g., when Unity’s domain reload is disabled).
	/// </summary>
	public static void Dispose()
	{
		_namespaceScopeIndex.Clear();
		_namespaceChainCache.Clear();
		_objectScopeIndex.Clear();
		_containers.Clear();
		Interlocked.Exchange(ref _containerCount, 0);
		Volatile.Write(ref _singleContainer, null);
		_currentScope = null;
	}
	
	/// <summary>
	/// Registers a container with the global provider.
	/// </summary>
	/// <param name="diContainer">The container to register.</param>
	internal static void RegisterContainer(IDiContainer diContainer)
	{
		RegisterContainer(diContainer, null, null);
	}

	/// <summary>
	/// Registers a container with the global provider along with its scope metadata.
	/// </summary>
	/// <param name="diContainer">The container to register.</param>
	/// <param name="namespaceScope">The namespace scope (optional).</param>
	/// <param name="scopeOwner">The scope owner object (optional).</param>
	internal static void RegisterContainer(IDiContainer diContainer, string namespaceScope, object scopeOwner)
	{
		if (diContainer == null)
		{
			throw new ArgumentNullException(nameof(diContainer));
		}

		if (namespaceScope != null)
		{
			ValidateNamespaceScope(namespaceScope);
		}

		var registration = new ContainerRegistration(diContainer, namespaceScope, scopeOwner);
		if (!_containers.TryAdd(diContainer, registration))
		{
			throw new Exception("Container is already registered.");
		}

		if (namespaceScope != null)
		{
			if (!_namespaceScopeIndex.TryAdd(namespaceScope, registration))
			{
				_containers.TryRemove(diContainer, out _);
				throw new Exception($"A container with namespace scope '{namespaceScope}' is already registered.");
			}
		}

		if (scopeOwner != null)
		{
			if (!_objectScopeIndex.TryAdd(scopeOwner, registration))
			{
				if (namespaceScope != null)
				{
					_namespaceScopeIndex.TryRemove(namespaceScope, out _);
				}

				_containers.TryRemove(diContainer, out _);
				throw new Exception($"A container with scope owner '{scopeOwner.GetType().FullName}' is already registered.");
			}
		}

		_namespaceChainCache.Clear();
		UpdateSingleContainerAfterAdd(registration);
	}

	/// <summary>
	/// Unregisters a container from the global provider.
	/// </summary>
	/// <param name="diContainer">The container to unregister.</param>
	internal static void UnregisterContainer(IDiContainer diContainer)
	{
		if (diContainer == null)
		{
			return;
		}

		if (!_containers.TryRemove(diContainer, out var registration))
		{
			return;
		}

		if (registration.NamespaceScope != null)
		{
			_namespaceScopeIndex.TryRemove(registration.NamespaceScope, out _);
		}

		if (registration.ScopeOwner != null)
		{
			_objectScopeIndex.TryRemove(registration.ScopeOwner, out _);
		}

		_namespaceChainCache.Clear();
		UpdateSingleContainerAfterRemove();
	}

	private static IDisposable PushScope(ScopeKind kind, string namespaceScope, object scopeOwner)
	{
		var frame = new ScopeFrame(kind, namespaceScope, scopeOwner, _currentScope);
		_currentScope = frame;
		return frame;
	}

	private static List<ContainerRegistration> GetNamespaceChain(string scope)
	{
		return _namespaceChainCache.GetOrAdd(scope, BuildNamespaceChain);
	}

	private static List<ContainerRegistration> BuildNamespaceChain(string scope)
	{
		var chain = new List<ContainerRegistration>();
		var current = scope;

		while (true)
		{
			if (_namespaceScopeIndex.TryGetValue(current, out var registration))
			{
				chain.Add(registration);
			}

			var lastDotIndex = current.LastIndexOf('.');
			if (lastDotIndex < 0)
			{
				break;
			}

			current = current.Substring(0, lastDotIndex);
		}

		return chain;
	}

	private static bool TryGetSingleContainer(out ContainerRegistration registration)
	{
		if (Volatile.Read(ref _containerCount) != 1)
		{
			registration = null;
			return false;
		}

		registration = _singleContainer;
		if (registration != null)
		{
			return true;
		}

		foreach (var item in _containers.Values)
		{
			registration = item;
			_singleContainer = item;
			return true;
		}

		registration = null;
		return false;
	}

	private static void UpdateSingleContainerAfterAdd(ContainerRegistration registration)
	{
		var newCount = Interlocked.Increment(ref _containerCount);
		if (newCount == 1)
		{
			Volatile.Write(ref _singleContainer, registration);
		}
		else
		{
			Volatile.Write(ref _singleContainer, null);
		}
	}

	private static void UpdateSingleContainerAfterRemove()
	{
		var newCount = Interlocked.Decrement(ref _containerCount);
		if (newCount == 1)
		{
			foreach (var item in _containers.Values)
			{
				Volatile.Write(ref _singleContainer, item);
				return;
			}

			Volatile.Write(ref _singleContainer, null);
			return;
		}

		if (newCount <= 0)
		{
			Volatile.Write(ref _singleContainer, null);
			return;
		}

		Volatile.Write(ref _singleContainer, null);
	}

	private static void ValidateNamespaceScope(string scope)
	{
		if (scope == null)
		{
			throw new ArgumentNullException(nameof(scope));
		}

		if (scope.Length > 0 && string.IsNullOrWhiteSpace(scope))
		{
			throw new ArgumentException("Scope cannot be whitespace.", nameof(scope));
		}
	}

	private static T ResolveFromRegistration<T>(ContainerRegistration registration) where T : class
	{
		if (!registration.Container.TryResolve<T>(out var instance))
		{
			throw new Exception($"Service of type {typeof(T).FullName} is not registered.");
		}

		return instance;
	}

	private enum ScopeKind
	{
		Namespace,
		Object
	}

	private sealed class ScopeFrame : IDisposable
	{
		public ScopeKind Kind { get; }
		public string NamespaceScope { get; }
		public object ScopeOwner { get; }
		public ScopeFrame Previous { get; }
		private bool _disposed;

		public ScopeFrame(ScopeKind kind, string namespaceScope, object scopeOwner, ScopeFrame previous)
		{
			Kind = kind;
			NamespaceScope = namespaceScope;
			ScopeOwner = scopeOwner;
			Previous = previous;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			if (!ReferenceEquals(_currentScope, this))
			{
				throw new InvalidOperationException("Scope disposed out of order.");
			}

			_currentScope = Previous;
			_disposed = true;
		}
	}

	private sealed class ContainerRegistration
	{
		public IDiContainer Container { get; }
		public string NamespaceScope { get; }
		public object ScopeOwner { get; }

		public ContainerRegistration(IDiContainer container, string namespaceScope, object scopeOwner)
		{
			Container = container;
			NamespaceScope = namespaceScope;
			ScopeOwner = scopeOwner;
		}
	}

	private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
	{
		public bool Equals(object x, object y)
		{
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(object obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
	}
}
}