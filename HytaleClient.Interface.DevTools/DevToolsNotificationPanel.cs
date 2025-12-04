using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;

namespace HytaleClient.Interface.DevTools;

internal class DevToolsNotificationPanel : Element
{
	private Group _errorPanel;

	private Group _warningPanel;

	private Label _errorLabel;

	private Label _warningLabel;

	private int _unreadErrors;

	private int _unreadWarnings;

	private readonly Interface _interface;

	public DevToolsNotificationPanel(Interface @interface)
		: base(@interface.Desktop, null)
	{
		_interface = @interface;
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("DevTools/NotificationPanel.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_errorPanel = uIFragment.Get<Group>("ErrorPanel");
		_warningPanel = uIFragment.Get<Group>("WarningPanel");
		_errorLabel = uIFragment.Get<Label>("ErrorLabel");
		_errorLabel.Text = Desktop.Provider.FormatNumber(_unreadErrors);
		_errorPanel.Visible = _unreadErrors > 0;
		_warningLabel = uIFragment.Get<Label>("WarningLabel");
		_warningLabel.Text = Desktop.Provider.FormatNumber(_unreadWarnings);
		_warningPanel.Visible = _unreadWarnings > 0;
	}

	public void AddUnreadError(int count)
	{
		_unreadErrors += count;
		if (!_interface.HasMarkupError && _interface.HasLoaded)
		{
			_errorLabel.Text = Desktop.Provider.FormatNumber(_unreadErrors);
			_errorPanel.Visible = true;
		}
	}

	public void ClearUnread()
	{
		if (_unreadErrors == 0 && _unreadWarnings == 0)
		{
			return;
		}
		_unreadErrors = 0;
		_unreadWarnings = 0;
		if (!_interface.HasMarkupError && _interface.HasLoaded)
		{
			_errorPanel.Visible = false;
			_warningPanel.Visible = false;
			if (base.IsMounted)
			{
				_warningPanel.Parent.Layout();
			}
		}
	}

	public void AddUnreadWarning(int count)
	{
		_unreadWarnings += count;
		if (!_interface.HasMarkupError && _interface.HasLoaded)
		{
			_warningLabel.Text = Desktop.Provider.FormatNumber(_unreadWarnings);
			_warningPanel.Visible = true;
		}
	}
}
