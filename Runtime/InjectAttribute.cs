using System;
using JetBrains.Annotations;

namespace LightDI.Runtime
{
/// <summary>
/// Indicates that a field or constructor parameter should be injected by the DI container.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field, Inherited = false)]
public class InjectAttribute : Attribute
{
}
}