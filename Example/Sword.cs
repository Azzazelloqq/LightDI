using System;

namespace LightDI.Example
{
public class Sword : IWeapon
{
	public void Attack()
	{
		Console.WriteLine("Sword slashes!");
	}
}
}