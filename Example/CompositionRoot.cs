using LightDI.Runtime;

namespace LightDI.Example
{
public class CompositionRoot
{
	public void Enter()
	{
		var container = DiContainerFactory.CreateContainer();
		
		container.RegisterAsSingleton<IServiceA>(() => new ServiceA());
		container.RegisterAsSingleton<IWeapon>(() => new Sword());
	}
}
}