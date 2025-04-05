using System;

namespace LightDI.Runtime
{
/// <summary>
/// Defines the interface for a dependency injection container.
/// 
/// <para>
/// Note: The internal Resolve and TryResolve methods are not intended for direct use.
/// </para>
/// </summary>
public interface IDiContainer : IDisposable
{
	/// <summary>
	/// Registers a singleton service with lazy initialization.
	/// </summary>
	/// <typeparam name="T">The service type.</typeparam>
	/// <param name="factory">A factory function to create the service.</param>
	public void RegisterAsSingletonLazy<T>(Func<T> factory) where T : class;
	
	/// <summary>
	/// Registers an already created singleton instance.
	/// </summary>
	/// <typeparam name="T">The service type.</typeparam>
	/// <param name="singleton">The singleton instance.</param>
	public void RegisterAsSingleton<T>(T singleton) where T : class;
	
	/// <summary>
	/// Registers a service as transient, creating a new instance on each resolve.
	/// </summary>
	/// <typeparam name="T">The service type.</typeparam>
	/// <param name="factory">A factory function to create the service.</param>
	public void RegisterAsTransient<T>(Func<T> factory) where T : class;
	
	/// <summary>
	/// Resolves a service of type <typeparamref name="T"/>.
	/// This method is intended for internal use only.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <returns>An instance of the service.</returns>
	internal T Resolve<T>() where T : class;
	
	/// <summary>
	/// Attempts to resolve a service of type <typeparamref name="T"/>.
	/// This method is intended for internal use only.
	/// </summary>
	/// <typeparam name="T">The service type to resolve.</typeparam>
	/// <param name="instance">When the method returns, contains the resolved service instance if found; otherwise, null.</param>
	/// <returns>True if the service was resolved; otherwise, false.</returns>
	internal bool TryResolve<T>(out T instance) where T : class;
}
}