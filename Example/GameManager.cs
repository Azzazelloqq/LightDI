using LightDI.Runtime;

namespace LightDI.Example
{
public class GameManager
{
	private readonly IServiceA _serviceA;
	private readonly int _runtimeValue;

	[Inject]
	private IWeapon _weapon;

	public GameManager([Inject] IServiceA serviceA, int runtimeValue) {
		_serviceA = serviceA;
		_runtimeValue = runtimeValue;
	}

	public void RunGame() {
		_serviceA.DoSomething();
		_weapon?.Attack();
		UnityEngine.Debug.Log($"GameManager running with runtime value: {_runtimeValue}");
	}
}
}