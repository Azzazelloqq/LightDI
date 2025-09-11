namespace LightDI.Example.AssemblyExample
{
internal class AssemblyRoot
{
	public void Program()
	{
		var injectExample = InjectExampleFactory.CreateInjectExample();
	}
}
}