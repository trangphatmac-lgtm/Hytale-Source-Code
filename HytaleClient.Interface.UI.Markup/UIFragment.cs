using System.Collections.Generic;
using HytaleClient.Interface.UI.Elements;

namespace HytaleClient.Interface.UI.Markup;

public class UIFragment
{
	public List<Element> RootElements;

	public readonly Dictionary<string, Element> ElementsByName = new Dictionary<string, Element>();

	public T Get<T>(string name) where T : Element
	{
		return ElementsByName[name] as T;
	}
}
