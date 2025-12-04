using Newtonsoft.Json;

namespace HytaleClient.Utils;

public struct ScreenResolution
{
	public int Width;

	public int Height;

	public bool Fullscreen;

	public ScreenResolution(int width, int height, bool fullscreen)
	{
		Width = width;
		Height = height;
		Fullscreen = fullscreen;
	}

	public override string ToString()
	{
		return JsonConvert.SerializeObject((object)this);
	}

	public static ScreenResolution FromString(string value)
	{
		return JsonConvert.DeserializeObject<ScreenResolution>(value);
	}

	public bool Equals(ScreenResolution other)
	{
		return Width == other.Width && Height == other.Height;
	}

	public bool FitsIn(int screenWidth, int screenHeight)
	{
		return Width <= screenWidth && Height <= screenHeight;
	}

	public string GetOptionName(bool native)
	{
		string arg = (native ? "*" : "");
		return $"{Width} x {Height}{arg}";
	}
}
