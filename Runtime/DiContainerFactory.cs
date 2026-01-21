using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LightDI.Runtime
{
/// <summary>
/// Factory for creating dependency injection containers.
/// The created container is automatically registered with the global DiContainerProvider.
/// </summary>
public static class DiContainerFactory
{
	/// <summary>
	/// Creates a new container, registers it globally, and subscribes to its dispose callback.
	/// </summary>
	/// <param name="disposeRegistered">
	/// If true, the container will dispose of all registered disposable services when disposed.
	/// </param>
	/// <returns>A new instance of IDiContainer.</returns>
	public static IDiContainer CreateGlobalContainer(bool disposeRegistered = true)
	{
		return CreateGlobalContainerInternal(disposeRegistered);
	}

	/// <summary>
	/// Creates a new container that is local to the calling assembly and subscribes to its dispose callback.
	/// </summary>
	/// <param name="disposeRegistered">
	/// If true, the container will dispose of all registered disposable services when disposed.
	/// </param>
	/// <returns>A new instance of IDiContainer.</returns>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static IDiContainer CreateLocalContainer(bool disposeRegistered = true)
	{
		var assembly = Assembly.GetCallingAssembly();
		var container = new LightDiContainer(disposeRegistered);
		DiContainerProvider.RegisterLocalContainer(container, assembly);

		container.SubscribeOnDisposeCallback(() => DiContainerProvider.UnregisterLocalContainer(container, assembly));

		return container;
	}

	/// <summary>
	/// Creates a new container, registers it globally, and subscribes to its dispose callback.
	/// </summary>
	/// <param name="disposeRegistered">
	/// If true, the container will dispose of all registered disposable services when disposed.
	/// </param>
	/// <returns>A new instance of IDiContainer.</returns>
	public static IDiContainer CreateContainer(bool disposeRegistered = true)
	{
		return CreateGlobalContainerInternal(disposeRegistered);
	}

	private static IDiContainer CreateGlobalContainerInternal(bool disposeRegistered)
	{
		var container = new LightDiContainer(disposeRegistered);
		DiContainerProvider.RegisterGlobalContainer(container);

		container.SubscribeOnDisposeCallback(() => DiContainerProvider.UnregisterGlobalContainer(container));

		return container;
	}
}
}