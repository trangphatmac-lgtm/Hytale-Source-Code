#define DEBUG
using System;
using System.Diagnostics;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.AssetEditor.Interface.Modals;

internal class ConfirmationModal : Element
{
	private Label _titleLabel;

	private Label _textlabel;

	private TextButton _confirmationButton;

	private TextButton _cancelButton;

	private Group _applyChangesLocallyContainer;

	private CheckBox _applyChangesLocally;

	private Action _onConfirm;

	private Action _onDismiss;

	public bool ApplyChangesLocally => _applyChangesLocallyContainer.Visible && _applyChangesLocally.Value;

	public ConfirmationModal(Desktop desktop, Element parent)
		: base(desktop, parent)
	{
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("AssetEditor/Modal.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_titleLabel = uIFragment.Get<Label>("TitleLabel");
		_textlabel = uIFragment.Get<Label>("TextLabel");
		_confirmationButton = uIFragment.Get<TextButton>("ConfirmationButton");
		_confirmationButton.Activating = Validate;
		_cancelButton = uIFragment.Get<TextButton>("CancelButton");
		_cancelButton.Activating = Dismiss;
		_applyChangesLocallyContainer = uIFragment.Get<Group>("ApplyChangesLocallyContainer");
		_applyChangesLocally = uIFragment.Get<CheckBox>("ApplyChangesLocally");
	}

	protected internal override void Dismiss()
	{
		Action onDismiss = _onDismiss;
		Close();
		onDismiss?.Invoke();
	}

	protected internal override void Validate()
	{
		Action onConfirm = _onConfirm;
		Close();
		onConfirm?.Invoke();
	}

	protected override void OnUnmounted()
	{
		_onDismiss = null;
		_onConfirm = null;
	}

	public void Open(string title, string text, Action onConfirm = null, Action onDismiss = null, string confirmText = null, string abortText = null, bool displayApplyLocalChangeCheckBox = false)
	{
		_confirmationButton.Text = confirmText ?? Desktop.Provider.GetText("ui.general.confirm");
		_cancelButton.Text = abortText ?? Desktop.Provider.GetText("ui.general.cancel");
		_titleLabel.Text = title;
		_textlabel.Text = text;
		_onConfirm = onConfirm;
		_onDismiss = onDismiss;
		_applyChangesLocallyContainer.Visible = displayApplyLocalChangeCheckBox;
		Desktop.SetTransientLayer(this);
	}

	private void Close()
	{
		Debug.Assert(Desktop.GetTransientLayer() == this);
		Desktop.SetTransientLayer(null);
	}
}
