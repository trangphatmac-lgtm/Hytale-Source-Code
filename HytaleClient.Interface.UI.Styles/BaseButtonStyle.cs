namespace HytaleClient.Interface.UI.Styles;

public abstract class BaseButtonStyle<T> where T : class, new()
{
	public T Default = new T();

	public T Hovered;

	public T Pressed;

	public T Disabled;

	public ButtonSounds Sounds;
}
