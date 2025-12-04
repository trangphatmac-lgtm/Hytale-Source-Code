using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.UI.Elements;

[UIMarkupData]
public class NumberFieldFormat
{
	public int MaxDecimalPlaces = 0;

	public decimal Step = 1m;

	public decimal MinValue = decimal.MinValue;

	public decimal MaxValue = decimal.MaxValue;

	public decimal DefaultValue;

	public string Suffix;
}
