using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HytaleClient.AssetEditor.Interface.Editor;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using HytaleClient.Utils;

namespace HytaleClient.AssetEditor.Interface.Modals;

internal class ChangelogModal : Element
{
	private struct ChangelogElement
	{
		public ChangelogElementType Type;

		public string Text;

		public string Date;
	}

	private enum ChangelogElementType
	{
		VersionTitle,
		SectionTitle,
		Change
	}

	public Version PreviouslyUsedVersion;

	private readonly AssetEditorOverlay _assetEditorOverlay;

	private Group _container;

	private Group _log;

	private bool _isInitialized;

	public ChangelogModal(AssetEditorOverlay assetEditorOverlay)
		: base(assetEditorOverlay.Desktop, null)
	{
		_assetEditorOverlay = assetEditorOverlay;
	}

	public void Build()
	{
		Clear();
		_isInitialized = false;
		Desktop.Provider.TryGetDocument("AssetEditor/ChangelogModal.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		uIFragment.Get<TextButton>("CloseButton").Activating = Dismiss;
		_log = uIFragment.Get<Group>("Log");
		_container = uIFragment.Get<Group>("Container");
		if (base.IsMounted)
		{
			InitChangelog();
		}
	}

	private void InitChangelog()
	{
		_isInitialized = true;
		Task.Run(delegate
		{
			List<ChangelogElement> elements = LoadChangelog();
			_assetEditorOverlay.Interface.Engine.RunOnMainThread(_assetEditorOverlay.Interface, delegate
			{
				BuildLog(elements);
			});
		});
	}

	private void BuildLog(List<ChangelogElement> elements)
	{
		_log.Clear();
		Desktop.Provider.TryGetDocument("AssetEditor/ChangelogVersion.ui", out var document);
		Desktop.Provider.TryGetDocument("AssetEditor/ChangelogChange.ui", out var document2);
		Desktop.Provider.TryGetDocument("AssetEditor/ChangelogSectionTitle.ui", out var document3);
		foreach (ChangelogElement element in elements)
		{
			switch (element.Type)
			{
			case ChangelogElementType.Change:
			{
				UIFragment uIFragment3 = document2.Instantiate(Desktop, _log);
				Label label = uIFragment3.Get<Label>("Text");
				label.TextSpans = FormattedMessageConverter.GetLabelSpansFromMarkup(element.Text, new SpanStyle
				{
					Color = label.Style.TextColor
				});
				break;
			}
			case ChangelogElementType.SectionTitle:
			{
				UIFragment uIFragment2 = document3.Instantiate(Desktop, _log);
				uIFragment2.Get<Label>("Text").Text = element.Text;
				break;
			}
			case ChangelogElementType.VersionTitle:
			{
				Version version = new Version(element.Text);
				UIFragment uIFragment = document.Instantiate(Desktop, _log);
				uIFragment.Get<Label>("Version").Text = element.Text;
				uIFragment.Get<Label>("NewLabel").Visible = PreviouslyUsedVersion != null && version > PreviouslyUsedVersion;
				uIFragment.Get<Label>("Date").Text = element.Date;
				break;
			}
			}
		}
		if (base.IsMounted)
		{
			_log.Layout();
		}
	}

	private List<ChangelogElement> LoadChangelog()
	{
		List<ChangelogElement> list = new List<ChangelogElement>();
		using (StreamReader streamReader = new StreamReader(Path.Combine(Paths.EditorData, "Changelog.md")))
		{
			Version version = null;
			string text;
			while ((text = streamReader.ReadLine()) != null)
			{
				text = text.Trim();
				if (text == "")
				{
					continue;
				}
				if (text.StartsWith("-"))
				{
					list.Add(new ChangelogElement
					{
						Type = ChangelogElementType.Change,
						Text = text.Substring(1).TrimStart(Array.Empty<char>())
					});
				}
				else if (text.StartsWith("###"))
				{
					list.Add(new ChangelogElement
					{
						Type = ChangelogElementType.SectionTitle,
						Text = text.Substring(3).Trim()
					});
				}
				else
				{
					if (!text.StartsWith("##"))
					{
						continue;
					}
					string[] array = text.Substring(2).Trim().Split(new char[1] { '-' }, 2);
					string text2 = array[0].Trim().TrimStart(new char[1] { '[' }).TrimEnd(new char[1] { ']' });
					if (!(text2.ToLowerInvariant() == "unreleased"))
					{
						string date = array[1].Trim();
						if (version == null)
						{
							version = new Version(text2);
						}
						list.Add(new ChangelogElement
						{
							Type = ChangelogElementType.VersionTitle,
							Text = text2,
							Date = date
						});
					}
				}
			}
		}
		return list;
	}

	protected override void OnMounted()
	{
		if (!_isInitialized)
		{
			InitChangelog();
			Layout();
		}
	}

	public override Element HitTest(Point position)
	{
		return base.HitTest(position) ?? this;
	}

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		base.OnMouseButtonUp(evt, activate);
		if (activate && !_container.AnchoredRectangle.Contains(Desktop.MousePosition))
		{
			Dismiss();
		}
	}

	protected internal override void Dismiss()
	{
		Desktop.ClearLayer(4);
	}

	protected internal override void Validate()
	{
		Dismiss();
	}
}
