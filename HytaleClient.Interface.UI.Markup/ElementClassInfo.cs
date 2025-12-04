using System;
using System.Collections.Generic;
using System.Reflection;

namespace HytaleClient.Interface.UI.Markup;

public class ElementClassInfo
{
	public string Name;

	public ConstructorInfo Constructor;

	public bool AcceptsChildren;

	public Dictionary<string, Type> PropertyTypes = new Dictionary<string, Type>();
}
