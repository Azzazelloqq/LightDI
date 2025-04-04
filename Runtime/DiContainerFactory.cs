namespace LightDI.Runtime
{
public static class DiContainerFactory
{
	public static IDiContainer CreateContainer(bool disposeRegistered = true)
	{
		var container = new LightDiContainer(disposeRegistered);
		DiContainerProvider.RegisterContainer(container);

		container.SubscribeOnDisposeCallback(() => DiContainerProvider.UnregisterContainer(container));

		return container;
	}
}
}