using System;

namespace LightDI.Runtime
{
internal class RegistrationInfo
{
	public Func<IDiContainer, object> Factory { get; }
	public Lifetime Lifetime { get; }
	public object CachedInstance { get; set; }

	public RegistrationInfo(Func<IDiContainer, object> factory, Lifetime lifetime)
	{
		Factory = factory;
		Lifetime = lifetime;
	}

	public RegistrationInfo(object instance, Lifetime lifetime)
	{
		Factory = _ => instance;
		CachedInstance = instance;
		Lifetime = lifetime;
	}
}
}