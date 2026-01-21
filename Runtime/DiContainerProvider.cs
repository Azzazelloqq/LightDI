using System;
using System.Collections.Concurrent;
using System.Reflection;
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
	private static readonly ConcurrentDictionary<Assembly, IDiContainer> _localContainers = new();
	private static readonly object _globalSync = new object();
	private static IDiContainer[] _globalContainers = Array.Empty<IDiContainer>();
	private static bool _allowMultipleGlobalContainers;

	/// <summary>
	/// Gets or sets a value indicating whether multiple global containers are allowed without warnings.
	/// </summary>
	public static bool AllowMultipleGlobalContainers
	{
		get { return Volatile.Read(ref _allowMultipleGlobalContainers); }
		set { Volatile.Write(ref _allowMultipleGlobalContainers, value); }
	}

	/// <summary>
	/// Resolves a service of type <typeparamref name="T"/>.
	/// This method is marked obsolete; use compile-time injection ([Inject]) instead.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <returns>An instance of the service.</returns>
	/// <exception cref="Exception">Thrown if no container can resolve the service.</exception>
	[Obsolete("Use attribute [Inject]. DiContainer not for regular use. It's for internal use or critical cases only.")]
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static T Resolve<T>() where T : class
	{
		return Resolve<T>(Assembly.GetCallingAssembly());
	}

	/// <summary>
	/// Resolves a service of type <typeparamref name="T"/> using the specified assembly for local lookup.
	/// This method is marked obsolete; use compile-time injection ([Inject]) instead.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <param name="assembly">The assembly used to locate a local container.</param>
	/// <returns>An instance of the service.</returns>
	/// <exception cref="Exception">Thrown if no container can resolve the service.</exception>
	[Obsolete("Use attribute [Inject]. DiContainer not for regular use. It's for internal use or critical cases only.")]
	public static T Resolve<T>(Assembly assembly) where T : class
	{
		if (assembly != null)
		{
			if (_localContainers.TryGetValue(assembly, out var localContainer))
			{
				if (localContainer.TryResolve<T>(out var instance))
				{
					return instance;
				}
			}
		}

		var globals = _globalContainers;
		for (int i = 0; i < globals.Length; i++)
		{
			if (globals[i].TryResolve<T>(out var instance))
			{
				return instance;
			}
		}

		throw new Exception($"Service of type {typeof(T).FullName} is not registered.");
	}

	/// <summary>
	/// Resolves a service directly from the provided container.
	/// Use this only for performance-critical paths where you already have a container instance.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <param name="container">The container to resolve from.</param>
	/// <returns>An instance of the service.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the container is null.</exception>
	[Obsolete("Use generated factories or assembly-aware Resolve methods. This API is for performance-critical paths only.")]
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
	[Obsolete("Use generated factories or assembly-aware Resolve methods. This API is for performance-critical paths only.")]
	public static bool TryResolveFromContainer<T>(IDiContainer container, out T instance) where T : class
	{
		if (container == null)
		{
			throw new ArgumentNullException(nameof(container));
		}

		return container.TryResolve<T>(out instance);
	}

	/// <summary>
	/// Disposes the global container registry.
	/// Call this to clear static data (e.g., when Unity’s domain reload is disabled).
	/// </summary>
	public static void Dispose()
	{
		_localContainers.Clear();
		lock (_globalSync)
		{
			_globalContainers = Array.Empty<IDiContainer>();
		}
	}

	/// <summary>
	/// Registers a global container with the provider.
	/// </summary>
	/// <param name="diContainer">The container to register.</param>
	internal static void RegisterGlobalContainer(IDiContainer diContainer)
	{
		if (diContainer == null)
		{
			throw new ArgumentNullException(nameof(diContainer));
		}

		RegisterGlobal(diContainer);
	}

	/// <summary>
	/// Registers a local container with the provider for the specified assembly.
	/// </summary>
	/// <param name="diContainer">The container to register.</param>
	/// <param name="assembly">The assembly associated with the local container.</param>
	internal static void RegisterLocalContainer(IDiContainer diContainer, Assembly assembly)
	{
		if (diContainer == null)
		{
			throw new ArgumentNullException(nameof(diContainer));
		}

		RegisterLocal(diContainer, assembly);
	}

	/// <summary>
	/// Unregisters a global container from the provider.
	/// </summary>
	/// <param name="diContainer">The container to unregister.</param>
	internal static void UnregisterGlobalContainer(IDiContainer diContainer)
	{
		if (diContainer == null)
		{
			return;
		}

		UnregisterGlobal(diContainer);
	}

	/// <summary>
	/// Unregisters a local container for the specified assembly.
	/// </summary>
	/// <param name="diContainer">The container to unregister.</param>
	/// <param name="assembly">The assembly associated with the local container.</param>
	internal static void UnregisterLocalContainer(IDiContainer diContainer, Assembly assembly)
	{
		if (diContainer == null)
		{
			return;
		}

		UnregisterLocal(diContainer, assembly);
	}

	private static void RegisterGlobal(IDiContainer diContainer)
	{
		lock (_globalSync)
		{
			var current = _globalContainers;
			if (Array.IndexOf(current, diContainer) >= 0)
			{
				throw new Exception("Container is already registered as global.");
			}

			var newList = new IDiContainer[current.Length + 1];
			Array.Copy(current, newList, current.Length);
			newList[current.Length] = diContainer;
			_globalContainers = newList;

			if (newList.Length > 1 && !AllowMultipleGlobalContainers)
			{
				LogWarning($"Multiple global containers detected ({newList.Length}). " +
						   "Set DiContainerProvider.AllowMultipleGlobalContainers = true to suppress this warning.");
			}
		}
	}

	private static void RegisterLocal(IDiContainer diContainer, Assembly assembly)
	{
		if (assembly == null)
		{
			throw new ArgumentNullException(nameof(assembly));
		}

		if (!_localContainers.TryAdd(assembly, diContainer))
		{
			throw new Exception($"A local container is already registered for assembly '{assembly.FullName}'.");
		}
	}

	private static void UnregisterGlobal(IDiContainer diContainer)
	{
		lock (_globalSync)
		{
			var current = _globalContainers;
			var index = Array.IndexOf(current, diContainer);
			if (index < 0)
			{
				return;
			}

			if (current.Length == 1)
			{
				_globalContainers = Array.Empty<IDiContainer>();
				return;
			}

			var newList = new IDiContainer[current.Length - 1];
			if (index > 0)
			{
				Array.Copy(current, 0, newList, 0, index);
			}

			if (index < current.Length - 1)
			{
				Array.Copy(current, index + 1, newList, index, current.Length - index - 1);
			}

			_globalContainers = newList;
		}
	}

	private static void UnregisterLocal(IDiContainer diContainer, Assembly assembly)
	{
		if (assembly == null)
		{
			return;
		}

		_localContainers.TryRemove(assembly, out _);
	}

	private static void LogWarning(string message)
	{
#if UNITY_5_3_OR_NEWER
		UnityEngine.Debug.LogWarning(message);
#endif
	}
}
}
