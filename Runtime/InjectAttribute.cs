using System;
using JetBrains.Annotations;

namespace LightDI.Runtime
{
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field)]
public class InjectAttribute : Attribute
{
}
}