using System;

namespace LightDI.Runtime
{
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field)]
public class InjectAttribute : Attribute
{
}
}