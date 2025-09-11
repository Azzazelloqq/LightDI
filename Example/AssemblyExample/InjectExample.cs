using LightDI.Runtime;

namespace LightDI.Example.AssemblyExample
{
internal class InjectExample
{
	private readonly IServiceA _serviceA;
	private readonly AssemblyServiceA _assemblyServiceA;

	public InjectExample([Inject] IServiceA serviceA, [Inject] AssemblyServiceA assemblyServiceA)
	{
		_serviceA = serviceA;
		_assemblyServiceA = assemblyServiceA;
	}
}
}