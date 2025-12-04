#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.Interface.Settings;

internal abstract class SettingComponent<T> : Element
{
	protected string _name;

	protected ISettingView _settings;

	public Action<T> OnChange;

	protected SettingComponent(Desktop desktop, Group parent, string name, ISettingView settings)
		: base(desktop, parent)
	{
		_name = name;
		_settings = settings;
	}

	protected UIFragment Build(string template, out Document doc)
	{
		_settings.TryGetDocument(template, out doc);
		UIFragment uIFragment = doc.Instantiate(Desktop, this);
		uIFragment.Get<Label>("Name").Text = Desktop.Provider.GetText(_name);
		return uIFragment;
	}

	public override Element HitTest(Point position)
	{
		Debug.Assert(base.IsMounted);
		if (!_anchoredRectangle.Contains(position))
		{
			return null;
		}
		return base.HitTest(position) ?? this;
	}

	protected override void OnMouseEnter()
	{
		string key = _name + ".description";
		string text = Desktop.Provider.GetText(key, null, returnFallback: false);
		if (text != null)
		{
			_settings.SetHoveredSetting(text, this);
		}
	}

	protected override void OnMouseLeave()
	{
		_settings.SetHoveredSetting(null, this);
	}

	public abstract void SetValue(T value);
}
