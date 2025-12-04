namespace HytaleClient.Interface.UI.Styles;

public abstract class BaseCheckBoxStyle<T> where T : class, new()
{
	public T Checked;

	public T Unchecked;
}
