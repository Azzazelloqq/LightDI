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
}
}