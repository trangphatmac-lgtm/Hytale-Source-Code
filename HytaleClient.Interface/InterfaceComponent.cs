using HytaleClient.Interface.UI.Elements;

namespace HytaleClient.Interface;

internal abstract class InterfaceComponent : Element
{
	public readonly Interface Interface;

	public InterfaceComponent(Interface @interface, Element parent)
		: base(@interface.Desktop, parent)
	{
		Interface = @interface;
	}
}
