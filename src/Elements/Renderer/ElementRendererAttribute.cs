using System;

namespace Promete.Elements.Renderer;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class ElementRendererAttribute(Type elementType) : Attribute
{
	public Type ElementType { get; } = elementType;
}
