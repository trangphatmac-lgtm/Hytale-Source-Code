#define DEBUG
using System.Diagnostics;
using HytaleClient.AssetEditor.Data;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Math;
using Newtonsoft.Json.Linq;

namespace HytaleClient.AssetEditor.Interface.Config;

internal class KeyModal : Element
{
	private Group _container;

	private Group _inputContainer;

	private ValueEditor _input;

	private Label _errorLabel;

	private TextButton _saveButton;

	private Label _titleLabel;

	private string _key;

	private PropertyPath _parentPropertyPath;

	private readonly ConfigEditor _configEditor;

	public KeyModal(ConfigEditor configEditor)
		: base(configEditor.Desktop, null)
	{
		_configEditor = configEditor;
	}

	public void Build()
	{
		Clear();
		Desktop.Provider.TryGetDocument("AssetEditor/KeyModal.ui", out var document);
		UIFragment uIFragment = document.Instantiate(Desktop, this);
		_saveButton = uIFragment.Get<TextButton>("SaveButton");
		_saveButton.Activating = Validate;
		uIFragment.Get<TextButton>("CancelButton").Activating = Dismiss;
		_inputContainer = uIFragment.Get<Group>("InputContainer");
		_container = uIFragment.Get<Group>("Container");
		_errorLabel = uIFragment.Get<Label>("ErrorMessage");
		_titleLabel = uIFragment.Get<Label>("Title");
	}

	public void OpenEditKey(string key, PropertyPath parentPropertyPath, SchemaNode schema, ConfigEditor editor)
	{
		Debug.Assert(key != null);
		_key = key;
		Open(parentPropertyPath, schema, editor);
	}

	public void OpenInsertKey(PropertyPath parentPropertyPath, SchemaNode schema, ConfigEditor editor)
	{
		_key = null;
		Open(parentPropertyPath, schema, editor);
	}

	private void Open(PropertyPath parentPropertyPath, SchemaNode schema, ConfigEditor editor)
	{
		_parentPropertyPath = parentPropertyPath;
		_errorLabel.Visible = false;
		_input = ValueEditor.CreateFromSchema(_inputContainer, schema, PropertyPath.Root, null, null, editor, null);
		_input.IsDetachedEditor = true;
		_input.BuildEditor();
		if (_key != null)
		{
			_input.SetValue(JToken.op_Implicit(_key));
			_input.UpdateDisplayedValue();
			_titleLabel.Text = Desktop.Provider.GetText("ui.assetEditor.keyModal.renameTitle");
			_saveButton.Text = Desktop.Provider.GetText("ui.assetEditor.keyModal.renameButton");
		}
		else
		{
			_titleLabel.Text = Desktop.Provider.GetText("ui.assetEditor.keyModal.insertTitle");
			_saveButton.Text = Desktop.Provider.GetText("ui.assetEditor.keyModal.insertButton");
		}
		Desktop.SetLayer(4, this);
		_input.Focus();
		if (_input is TextEditor textEditor)
		{
			textEditor.SelectAll();
		}
	}

	protected override void OnUnmounted()
	{
		_inputContainer.Clear();
		_input = null;
	}

	protected internal override void Validate()
	{
		string text = ((_input.Value != null) ? ((string)_input.Value).Trim() : "");
		PropertyEditor propertyEditor;
		if (text == "")
		{
			SetError(Desktop.Provider.GetText("ui.assetEditor.keyModal.errors.fieldEmpty"));
		}
		else if (_configEditor.TryFindPropertyEditor(_parentPropertyPath, out propertyEditor))
		{
			ValueEditor valueEditor = propertyEditor.ValueEditor;
			ValueEditor valueEditor2 = valueEditor;
			if (!(valueEditor2 is MapEditor mapEditor))
			{
				if (valueEditor2 is WeightedTimelineEditor weightedTimelineEditor)
				{
					if (weightedTimelineEditor.HasEntryId(text))
					{
						SetError(Desktop.Provider.GetText("ui.assetEditor.keyModal.errors.existingKey"));
						return;
					}
					WeightedTimelineEditor weightedTimelineEditor2 = weightedTimelineEditor;
					weightedTimelineEditor2.HandleInsertEntry(text);
					Desktop.ClearLayer(4);
				}
				else
				{
					SetError(Desktop.Provider.GetText("ui.assetEditor.keyModal.errors.invalidProperty"));
				}
			}
			else if (mapEditor.HasItemWithKey(text))
			{
				SetError(Desktop.Provider.GetText("ui.assetEditor.keyModal.errors.existingKey"));
			}
			else
			{
				MapEditor mapEditor2 = mapEditor;
				if (_key != null)
				{
					mapEditor2.HandleRenameKey(_key, text);
				}
				else
				{
					mapEditor2.HandleInsertKey(text);
				}
				Desktop.ClearLayer(4);
			}
		}
		else
		{
			SetError(Desktop.Provider.GetText("ui.assetEditor.keyModal.errors.invalidProperty"));
		}
	}

	private void SetError(string message)
	{
		_errorLabel.Text = message;
		_errorLabel.Visible = true;
		Layout();
	}

	protected internal override void Dismiss()
	{
		Desktop.ClearLayer(4);
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
}
