using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;

namespace HytaleClient.AssetEditor.Interface.Elements;

internal abstract class BaseModal : Element
{
	protected Group _container;

	protected Group _content;

	protected Label _title;

	private readonly string _documentPath;

	protected readonly AssetEditorInterface _interface;

	protected BaseModal(AssetEditorInterface @interface, string documentPath)
		: base(@interface.Desktop, null)
	{
		_interface = @interface;
		_documentPath = documentPath;
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument(_documentPath, out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_container = uIFragment.Get<Group>("Container");
		_content = uIFragment.Get<Group>("Content");
		_title = uIFragment.Get<Label>("Title");
		uIFragment.Get<TextButton>("CloseButton").Activating = Dismiss;
		BuildModal(document, uIFragment);
	}

	protected abstract void BuildModal(Document doc, UIFragment fragment);

	protected override void OnMouseButtonUp(MouseButtonEvent evt, bool activate)
	{
		base.OnMouseButtonUp(evt, activate);
		if (activate && !_container.AnchoredRectangle.Contains(Desktop.MousePosition))
		{
			Dismiss();
		}
	}

	protected void OpenInLayer()
	{
		Desktop.SetLayer(4, this);
	}

	protected internal override void Dismiss()
	{
		Desktop.ClearLayer(4);
	}

	public override Element HitTest(Point position)
	{
		return base.HitTest(position) ?? this;
	}
}
