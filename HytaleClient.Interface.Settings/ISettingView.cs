using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.Settings;

internal interface ISettingView
{
	void SetHoveredSetting<T>(string setting, SettingComponent<T> component);

	bool TryGetDocument(string path, out Document document);
}
