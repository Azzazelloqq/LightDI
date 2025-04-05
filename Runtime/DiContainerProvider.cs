using System;
using System.Collections.Generic;

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
	private static readonly List<IDiContainer> _containers = new();

	/// <summary>
	/// Resolves a service of type <typeparamref name="T"/> from the first container that can resolve it.
	/// This method is marked obsolete; use compile-time injection ([Inject]) instead.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <returns>An instance of the service.</returns>
	/// <exception cref="Exception">Thrown if no container can resolve the service.</exception>
	[Obsolete("Use attribute [Inject]. DiContainer not for regular use. It's for internal use or critical cases only.")]
	public static T Resolve<T>() where T : class
	{
		foreach (var diContainer in _containers)
		{
			if (!diContainer.TryResolve<T>(out var instance))
			{
				continue;
			}

			return instance;
		}
		
		throw new Exception($"Service of type {typeof(T).FullName} is not registered.");
	}
	
	/// <summary>
	/// Disposes the global container registry.
	/// Call this to clear static data (e.g., when Unity’s domain reload is disabled).
	/// </summary>
	public static void Dispose()
	{
		_containers.Clear();
	}
	
	/// <summary>
	/// Registers a container with the global provider.
	/// </summary>
	/// <param name="diContainer">The container to register.</param>
	internal static void RegisterContainer(IDiContainer diContainer)
	{
		_containers.Add(diContainer);
	}

	/// <summary>
	/// Unregisters a container from the global provider.
	/// </summary>
	/// <param name="diContainer">The container to unregister.</param>
	internal static void UnregisterContainer(IDiContainer diContainer)
	{
		_containers.Remove(diContainer);
	}
}
}