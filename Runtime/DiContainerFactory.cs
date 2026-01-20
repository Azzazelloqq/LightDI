using System;

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
	public static IDiContainer CreateContainer(bool disposeRegistered = true)
	{
		var container = new LightDiContainer(disposeRegistered);
		DiContainerProvider.RegisterContainer(container);

		container.SubscribeOnDisposeCallback(() => DiContainerProvider.UnregisterContainer(container));

		return container;
	}

	/// <summary>
	/// Creates a new container and registers it with a namespace scope.
	/// </summary>
	/// <param name="namespaceScope">The namespace scope used for resolution.</param>
	/// <param name="disposeRegistered">
	/// If true, the container will dispose of all registered disposable services when disposed.
	/// </param>
	/// <returns>A new instance of IDiContainer.</returns>
	public static IDiContainer CreateContainer(string namespaceScope, bool disposeRegistered = true)
	{
		var container = new LightDiContainer(disposeRegistered);
		DiContainerProvider.RegisterContainer(container, namespaceScope, null);

		container.SubscribeOnDisposeCallback(() => DiContainerProvider.UnregisterContainer(container));

		return container;
	}

	/// <summary>
	/// Creates a new container and registers it with a scope owner object.
	/// </summary>
	/// <param name="scopeOwner">The object that represents the scope owner.</param>
	/// <param name="disposeRegistered">
	/// If true, the container will dispose of all registered disposable services when disposed.
	/// </param>
	/// <returns>A new instance of IDiContainer.</returns>
	public static IDiContainer CreateContainer(object scopeOwner, bool disposeRegistered = true)
	{
		if (scopeOwner == null)
		{
			throw new ArgumentNullException(nameof(scopeOwner));
		}

		var container = new LightDiContainer(disposeRegistered);
		DiContainerProvider.RegisterContainer(container, null, scopeOwner);

		container.SubscribeOnDisposeCallback(() => DiContainerProvider.UnregisterContainer(container));

		return container;
	}
}
}