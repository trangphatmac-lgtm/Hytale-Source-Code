using System;

namespace HytaleClient.Interface.UI.Markup;

[AttributeUsage(AttributeTargets.Class)]
internal class UIMarkupElementAttribute : Attribute
{
	public bool AcceptsChildren = false;

	public bool ExposeInheritedProperties = true;
}
