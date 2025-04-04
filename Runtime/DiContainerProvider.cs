using System;
using System.Collections.Generic;

namespace LightDI.Runtime
{
public static class DiContainerProvider
{
	private static readonly List<IDiContainer> _containers = new();

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
	

	public static void Dispose()
	{
		_containers.Clear();
	}

	internal static void RegisterContainer(IDiContainer diContainer)
	{
		_containers.Add(diContainer);
	}
	
	internal static void UnregisterContainer(IDiContainer diContainer)
	{
		_containers.Remove(diContainer);
	}
}
}