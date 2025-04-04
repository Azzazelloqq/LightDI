using System;

namespace LightDI.Example
{
internal class ServiceA : IServiceA {
	void IServiceA.DoSomething() {
		Console.WriteLine("ServiceA is doing its job.");
	}
}
}