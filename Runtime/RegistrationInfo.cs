using System;

namespace LightDI.Runtime
{
/// <summary>
/// Represents the registration information for a service.
/// It includes a factory function, the lifetime of the service,
/// and a cached instance for singleton registrations.
/// </summary>
internal class RegistrationInfo
{
	/// <summary>
	/// Gets the factory function that creates the service instance.
	/// The factory receives the container as a parameter.
	/// </summary>
	public Func<IDiContainer, object> Factory { get; }
	
	/// <summary>
	/// Gets the lifetime of the service (Singleton or Transient).
	/// </summary>
	public Lifetime Lifetime { get; }
	
	/// <summary>
	/// Gets or sets the cached instance for a singleton service.
	/// </summary>
	public object CachedInstance { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RegistrationInfo"/> class with a factory function.
	/// </summary>
	/// <param name="factory">A factory function that creates the service instance.</param>
	/// <param name="lifetime">The lifetime of the service.</param>
	public RegistrationInfo(Func<IDiContainer, object> factory, Lifetime lifetime)
	{
		Factory = factory;
		Lifetime = lifetime;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RegistrationInfo"/> class with an already created instance.
	/// </summary>
	/// <param name="instance">The already created service instance.</param>
	/// <param name="lifetime">The lifetime of the service.</param>
	public RegistrationInfo(object instance, Lifetime lifetime)
	{
		Factory = _ => instance;
		CachedInstance = instance;
		Lifetime = lifetime;
	}
}
}