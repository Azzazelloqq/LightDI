namespace LightDI.Runtime
{
/// <summary>
/// Represents the lifetime of a registered service.
/// </summary>
public enum Lifetime
{
	/// <summary>
	/// A new instance is created every time the service is resolved.
	/// </summary>
	Transient,
	/// <summary>
	/// A single instance is created and reused for all resolutions.
	/// </summary>
	Singleton
}
}