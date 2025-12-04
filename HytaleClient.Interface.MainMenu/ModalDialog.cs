using System;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.MainMenu;

internal class ModalDialog : InterfaceComponent
{
	public class DialogSetup
	{
		public string Title;

		public string Text;

		public string ConfirmationText;

		public string CancelText;

		public bool Cancellable = true;

		public bool Dismissable = true;

		public Action OnConfirm;

		public Action OnCancel;
	}

	private Label _title;

	private Label _text;

	private TextButton _confirmButton;

	private TextButton _cancelButton;

	private DialogSetup _setup;

	public ModalDialog(Interface @interface, DialogSetup setup = null)
		: base(@interface, null)
	{
		_setup = setup;
	}

	public void Build()
	{
		Clear();
		Interface.TryGetDocument("MainMenu/ModalDialog.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_title = uIFragment.Get<Label>("TitleLabel");
		_text = uIFragment.Get<Label>("TextLabel");
		_confirmButton = uIFragment.Get<TextButton>("ConfirmButton");
		_confirmButton.Activating = Confirm;
		_cancelButton = uIFragment.Get<TextButton>("CancelButton");
		_cancelButton.Activating = Cancel;
		if (_setup != null)
		{
			Setup(_setup);
		}
	}

	public void Setup(DialogSetup setup)
	{
		_setup = setup;
		_title.Text = ((setup.Title != null) ? Interface.GetText(setup.Title) : string.Empty);
		_text.Text = ((setup.Text != null) ? Interface.GetText(setup.Text) : string.Empty);
		_confirmButton.Text = Interface.GetText(setup.ConfirmationText ?? "ui.general.confirm");
		_cancelButton.Text = Interface.GetText(setup.CancelText ?? "ui.general.cancel");
		_cancelButton.Visible = setup.Cancellable;
	}

	protected override void OnUnmounted()
	{
		_setup = null;
	}

	private void Confirm()
	{
		Action onConfirm = _setup.OnConfirm;
		Desktop.ClearLayer(4);
		onConfirm?.Invoke();
	}

	private void Cancel()
	{
		Action onCancel = _setup.OnCancel;
		Desktop.ClearLayer(4);
		onCancel?.Invoke();
	}

	protected internal override void Validate()
	{
		Confirm();
	}

	protected internal override void Dismiss()
	{
		if (_setup.Dismissable)
		{
			Cancel();
		}
	}
}
