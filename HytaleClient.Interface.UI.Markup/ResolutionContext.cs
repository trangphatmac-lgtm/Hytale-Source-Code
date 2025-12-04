using System.Collections.Generic;

namespace HytaleClient.Interface.UI.Markup;

public class ResolutionContext
{
	public readonly IUIProvider Provider;

	public readonly HashSet<Expression> ExpressionPath = new HashSet<Expression>();

	public ResolutionContext(IUIProvider provider)
	{
		Provider = provider;
	}
}
