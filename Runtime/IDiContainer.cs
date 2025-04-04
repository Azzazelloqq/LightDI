using System;

namespace LightDI.Runtime
{
public interface IDiContainer : IDisposable
{
	public void RegisterAsSingleton<T>(Func<T> factory) where T : class;
	public void RegisterAsSingleton<T>(T singleton) where T : class;
	public void RegisterAsTransient<T>(Func<T> factory) where T : class;
	internal T Resolve<T>() where T : class;
	internal bool TryResolve<T>(out T instance) where T : class;
}
}