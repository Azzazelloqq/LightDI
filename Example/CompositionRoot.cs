using LightDI.Runtime;

namespace LightDI.Example
{
public class CompositionRoot
{
	public void Enter()
	{
		var container = DiContainerFactory.CreateContainer("LightDI.Example");
		
		container.RegisterAsSingletonLazy<IServiceA>(() => new ServiceA());
		container.RegisterAsSingletonLazy<IWeapon>(() => new Sword());

		var gameManager = GameManagerFactory.CreateGameManager(10);
		gameManager.RunGame();
	}
}
}