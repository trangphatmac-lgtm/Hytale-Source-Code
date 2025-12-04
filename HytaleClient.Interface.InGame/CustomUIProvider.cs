using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using HytaleClient.Audio;
using HytaleClient.Core;
using HytaleClient.Data;
using HytaleClient.Graphics;
using HytaleClient.Graphics.Fonts;
using HytaleClient.Interface.Messages;
using HytaleClient.Interface.UI;
using HytaleClient.Interface.UI.Elements;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;
using HytaleClient.Math;
using HytaleClient.Protocol;
using HytaleClient.Utils;
using NLog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HytaleClient.Interface.InGame;

internal class CustomUIProvider : Disposable, IUIProvider
{
	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public readonly Interface Interface;

	private readonly Dictionary<string, Document> _documentsLibrary = new Dictionary<string, Document>();

	private Texture _atlas;

	private Dictionary<string, TextureArea> _atlasTextureAreas = new Dictionary<string, TextureArea>();

	public Point TextureAtlasSize => new Point(_atlas?.Width ?? 0, _atlas?.Height ?? 0);

	public TextureArea WhitePixel { get; private set; }

	public TextureArea MissingTexture { get; private set; }

	public TexturePatch MissingTexturePatch { get; private set; }

	private static void GetFieldOrProperty(Type parentType, string name, out FieldInfo fieldInfo, out PropertyInfo propertyInfo)
	{
		MemberInfo[] member = parentType.GetMember(name, BindingFlags.Instance | BindingFlags.Public);
		if (member.Length != 1)
		{
			fieldInfo = null;
			propertyInfo = null;
		}
		else
		{
			fieldInfo = member[0] as FieldInfo;
			propertyInfo = member[0] as PropertyInfo;
		}
	}

	public void ApplyCommands(CustomUICommand[] commands, Element layer)
	{
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected I4, but got Unknown
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Invalid comparison between Unknown and I4
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Invalid comparison between Unknown and I4
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Invalid comparison between Unknown and I4
		//IL_0536: Unknown result type (might be due to invalid IL or missing references)
		//IL_053c: Invalid comparison between Unknown and I4
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0560: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		foreach (CustomUICommand val in commands)
		{
			Element selectedElement = null;
			List<string> selectedPropertyPath = null;
			if (val.Selector != null)
			{
				ResolveSelector(val.Selector, layer, out selectedElement, out selectedPropertyPath);
				if (selectedElement == null)
				{
					throw new Exception("Selected element in CustomUI command was not found. Selector: " + val.Selector);
				}
			}
			CustomUICommandType type = val.Type;
			CustomUICommandType val2 = type;
			switch ((int)val2)
			{
			case 0:
			case 1:
			case 2:
			case 3:
			{
				if (selectedPropertyPath != null)
				{
					throw new Exception($"Custom UI {val.Type} command cannot be applied on a property. Selector:  Selector: {val.Selector}");
				}
				bool flag = (int)val.Type == 0 || (int)val.Type == 1;
				Document document;
				if ((int)val.Type == 1 || (int)val.Type == 3)
				{
					try
					{
						document = DocumentParser.Parse(val.Text, "");
						document.ResolveProperties(this);
					}
					catch
					{
						throw new Exception($"Failed to parse or resolve document for Custom UI {val.Type} command. Selector: {val.Selector}");
					}
				}
				else if (!Interface.InGameCustomUIProvider.TryGetDocument(val.Text, out document))
				{
					throw new Exception($"Could not find document {val.Text} for Custom UI {val.Type} command. Selector: {val.Selector}");
				}
				UIFragment uIFragment = document.Instantiate(layer.Desktop, null);
				if (flag)
				{
					if (selectedElement != null && !selectedElement.GetType().GetCustomAttribute<UIMarkupElementAttribute>().AcceptsChildren)
					{
						throw new Exception($"CustomUI {val.Type} command's selected element doesn't accept children. Selector: {val.Selector}");
					}
					Element element3 = selectedElement ?? layer;
					foreach (Element rootElement in uIFragment.RootElements)
					{
						element3.Add(rootElement);
					}
					break;
				}
				if (selectedElement == null)
				{
					throw new Exception($"CustomUI {val.Type} command needs a selected element");
				}
				Element parent = selectedElement.Parent;
				foreach (Element rootElement2 in uIFragment.RootElements)
				{
					parent.Add(rootElement2, selectedElement);
					selectedElement = rootElement2;
				}
				break;
			}
			case 6:
				if (selectedElement == null)
				{
					throw new Exception("CustomUI Clear command needs a selected element");
				}
				selectedElement.Clear();
				break;
			case 4:
				if (selectedElement == null)
				{
					throw new Exception("CustomUI Remove command needs a selected element");
				}
				if (selectedPropertyPath != null)
				{
					throw new Exception("CustomUI Remove command can't be applied on a property. Selector: " + val.Selector);
				}
				selectedElement.Parent.Remove(selectedElement);
				break;
			case 5:
			{
				if (selectedElement == null)
				{
					throw new Exception("CustomUI Set command needs a selected element");
				}
				JToken val3;
				try
				{
					val3 = ((JObject)BsonHelper.FromBson(val.Data))["0"];
				}
				catch (JsonReaderException)
				{
					throw new Exception("CustomUI command data is not valid JSON. Selector: " + val.Selector);
				}
				if (selectedPropertyPath != null && selectedPropertyPath.Count > 0)
				{
					GetFieldOrProperty(selectedElement.GetType(), selectedPropertyPath[0], out var fieldInfo, out var propertyInfo);
					if (fieldInfo == null && propertyInfo == null)
					{
						throw new Exception("CustomUI Set command selector doesn't match a markup property. Selector: " + val.Selector);
					}
					MemberInfo element = ((fieldInfo != null) ? ((MemberInfo)fieldInfo) : ((MemberInfo)propertyInfo));
					if (element.GetCustomAttribute<UIMarkupPropertyAttribute>() == null)
					{
						throw new Exception("CustomUI Set command selector doesn't match a markup property. Selector: " + val.Selector);
					}
					Type type2 = fieldInfo?.FieldType ?? propertyInfo.PropertyType;
					object obj = selectedElement;
					for (int j = 1; j < selectedPropertyPath.Count; j++)
					{
						if (obj == null)
						{
							throw new Exception("CustomUI Set command selector doesn't match a markup property. Selector: " + val.Selector);
						}
						obj = fieldInfo?.GetValue(obj) ?? propertyInfo.GetValue(obj);
						string name = selectedPropertyPath[j];
						GetFieldOrProperty(type2, name, out fieldInfo, out propertyInfo);
						if (fieldInfo == null && propertyInfo == null)
						{
							throw new Exception("CustomUI Set command selector doesn't match a markup property. Selector: " + val.Selector);
						}
						type2 = fieldInfo?.FieldType ?? propertyInfo.PropertyType;
					}
					try
					{
						if (fieldInfo != null)
						{
							fieldInfo.SetValue(obj, JsonToMarkupValue(type2, val3));
						}
						else
						{
							propertyInfo.SetValue(obj, JsonToMarkupValue(type2, val3));
						}
					}
					catch (Exception innerException)
					{
						throw new Exception("CustomUI Set command couldn't set value. Selector: " + val.Selector, innerException);
					}
					break;
				}
				if ((int)val3.Type != 1)
				{
					throw new Exception("CustomUI command data must be an object if no property was selected. Selector: " + val.Selector);
				}
				foreach (KeyValuePair<string, JToken> item in (JObject)val3)
				{
					GetFieldOrProperty(selectedElement.GetType(), item.Key, out var fieldInfo2, out var propertyInfo2);
					MemberInfo element2 = ((fieldInfo2 != null) ? ((MemberInfo)fieldInfo2) : ((MemberInfo)propertyInfo2));
					if (element2.GetCustomAttribute<UIMarkupPropertyAttribute>() == null)
					{
						throw new Exception("CustomUI Set command selector doesn't match a markup property. Selector: " + val.Selector);
					}
					Type type3 = fieldInfo2?.FieldType ?? propertyInfo2.PropertyType;
					object obj2 = fieldInfo2?.GetValue(selectedElement) ?? propertyInfo2.GetValue(selectedElement);
					try
					{
						if (fieldInfo2 != null)
						{
							fieldInfo2.SetValue(obj2, JsonToMarkupValue(fieldInfo2.FieldType, item.Value));
						}
						else
						{
							propertyInfo2.SetValue(obj2, JsonToMarkupValue(propertyInfo2.PropertyType, item.Value));
						}
					}
					catch (Exception innerException2)
					{
						throw new Exception("CustomUI Set command couldn't set value. Selector: " + val.Selector, innerException2);
					}
				}
				break;
			}
			}
		}
		layer.Layout();
	}

	public static void ResolveSelector(string selector, Element layer, out Element selectedElement, out List<string> selectedPropertyPath)
	{
		selectedElement = null;
		selectedPropertyPath = null;
		string[] array = selector.Split(new char[1] { '.' }, 2);
		Element element = null;
		string[] array2 = array[0].Split(new char[1] { ' ' });
		foreach (string text in array2)
		{
			if (text[0] != '#')
			{
				return;
			}
			string[] array3 = text.Split(new char[1] { '[' });
			element = (element ?? layer).Find<Element>(array3[0].Substring(1));
			if (element == null || element.GetType().GetCustomAttribute<UIMarkupElementAttribute>() == null)
			{
				return;
			}
			if (array3.Length <= 1)
			{
				continue;
			}
			for (int j = 1; j < array3.Length; j++)
			{
				string text2 = array3[j];
				if (text2.Length < 2 || text2[text2.Length - 1] != ']' || !int.TryParse(text2.Substring(0, text2.Length - 1), NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out var result) || result < 0 || result >= element.Children.Count)
				{
					return;
				}
				element = element.Children[result];
				if (element == null || element.GetType().GetCustomAttribute<UIMarkupElementAttribute>() == null)
				{
					return;
				}
			}
		}
		if (element == null)
		{
			return;
		}
		List<string> list = new List<string>();
		if (array.Length == 2)
		{
			string[] array4 = array[1].Split(new char[1] { '.' });
			foreach (string text3 in array4)
			{
				if (text3.Length == 0 || text3.IndexOf(' ') != -1)
				{
					return;
				}
				list.Add(text3);
			}
		}
		selectedElement = element;
		if (list.Count > 0)
		{
			selectedPropertyPath = list;
		}
	}

	public static bool TryGetPropertyValueAsJsonFromSelector(string selector, Element layer, out JToken value)
	{
		value = null;
		ResolveSelector(selector, layer, out var selectedElement, out var selectedPropertyPath);
		if (selectedPropertyPath == null)
		{
			return false;
		}
		GetFieldOrProperty(selectedElement.GetType(), selectedPropertyPath[0], out var fieldInfo, out var propertyInfo);
		if (fieldInfo == null && propertyInfo == null)
		{
			return false;
		}
		MemberInfo element = ((fieldInfo != null) ? ((MemberInfo)fieldInfo) : ((MemberInfo)propertyInfo));
		if (element.GetCustomAttribute<UIMarkupPropertyAttribute>() == null)
		{
			return false;
		}
		Type parentType = fieldInfo?.FieldType ?? propertyInfo.PropertyType;
		object obj = selectedElement;
		for (int i = 1; i < selectedPropertyPath.Count; i++)
		{
			if (obj == null)
			{
				return false;
			}
			obj = fieldInfo?.GetValue(obj) ?? propertyInfo.GetValue(obj);
			string name = selectedPropertyPath[i];
			GetFieldOrProperty(parentType, name, out fieldInfo, out propertyInfo);
			if (fieldInfo == null && propertyInfo == null)
			{
				return false;
			}
			parentType = fieldInfo?.FieldType ?? propertyInfo.PropertyType;
		}
		object obj2;
		try
		{
			obj2 = ((!(fieldInfo != null)) ? propertyInfo.GetValue(obj) : fieldInfo.GetValue(obj));
		}
		catch
		{
			return false;
		}
		return TrySerializeObjectAsJson(obj2, out value);
	}

	private static bool TrySerializeObjectAsJson(object obj, out JToken result)
	{
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Expected O, but got Unknown
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bb: Expected O, but got Unknown
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Expected O, but got Unknown
		result = null;
		if (obj == null)
		{
			result = (JToken)(object)JValue.CreateNull();
			return true;
		}
		Type type = obj.GetType();
		type = Nullable.GetUnderlyingType(type) ?? type;
		if (type == typeof(string))
		{
			result = JToken.FromObject((object)(string)obj);
			return true;
		}
		if (type == typeof(char))
		{
			result = JToken.FromObject((object)(char)obj);
			return true;
		}
		if (type == typeof(int))
		{
			result = JToken.FromObject((object)(int)obj);
			return true;
		}
		if (type == typeof(float))
		{
			result = JToken.FromObject((object)(float)obj);
			return true;
		}
		if (type == typeof(decimal))
		{
			result = JToken.FromObject((object)(decimal)obj);
			return true;
		}
		if (type == typeof(bool))
		{
			result = JToken.FromObject((object)(bool)obj);
			return true;
		}
		if (type.IsEnum)
		{
			result = JToken.FromObject((object)obj.ToString());
			return true;
		}
		if (type == typeof(UInt32Color))
		{
			result = JToken.FromObject((object)((UInt32Color)obj).ToHexString());
			return true;
		}
		if (type.IsArray)
		{
			JArray val = new JArray();
			foreach (object item in (Array)obj)
			{
				if (!TrySerializeObjectAsJson(item, out var result2))
				{
					return false;
				}
				val.Add(result2);
			}
			result = (JToken)(object)val;
			return true;
		}
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
		{
			JArray val2 = new JArray();
			foreach (object item2 in (IList)obj)
			{
				if (!TrySerializeObjectAsJson(item2, out var result3))
				{
					return false;
				}
				val2.Add(result3);
			}
			result = (JToken)(object)val2;
			return true;
		}
		result = (JToken)new JObject();
		MemberInfo[] members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public);
		foreach (MemberInfo memberInfo in members)
		{
			FieldInfo fieldInfo = memberInfo as FieldInfo;
			PropertyInfo propertyInfo = memberInfo as PropertyInfo;
			if (!(fieldInfo == null) || !(propertyInfo == null))
			{
				object obj2 = ((fieldInfo != null) ? fieldInfo.GetValue(obj) : propertyInfo.GetValue(obj));
				if (!TrySerializeObjectAsJson(obj2, out var result4))
				{
					return false;
				}
				result[(object)memberInfo.Name] = result4;
			}
		}
		return true;
	}

	private object JsonToMarkupValue(Type type, JToken jsonToken)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected I4, but got Unknown
		//IL_052c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0532: Expected O, but got Unknown
		//IL_07a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bd: Expected O, but got Unknown
		//IL_03be: Unknown result type (might be due to invalid IL or missing references)
		//IL_045e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0631: Unknown result type (might be due to invalid IL or missing references)
		if ((int)jsonToken.Type == 10)
		{
			return null;
		}
		type = Nullable.GetUnderlyingType(type) ?? type;
		JTokenType type2 = jsonToken.Type;
		JTokenType val = type2;
		switch (val - 1)
		{
		case 7:
		{
			string text = (string)jsonToken;
			if (type == typeof(string))
			{
				return text;
			}
			if (type == typeof(char) && text.Length == 1)
			{
				return text[0];
			}
			if (type == typeof(UIPath))
			{
				return new UIPath(text);
			}
			if (type == typeof(UInt32Color))
			{
				if ((text.Length == 7 || text.Length == 9) && text[0] == '#')
				{
					try
					{
						byte r = Convert.ToByte(text.Substring(1, 2), 16);
						byte g = Convert.ToByte(text.Substring(3, 2), 16);
						byte b = Convert.ToByte(text.Substring(5, 2), 16);
						byte a = ((text.Length == 7) ? byte.MaxValue : Convert.ToByte(text.Substring(7, 2), 16));
						return UInt32Color.FromRGBA(r, g, b, a);
					}
					catch
					{
					}
				}
				break;
			}
			if (type == typeof(PatchStyle))
			{
				if ((text.Length == 7 || text.Length == 9) && text[0] == '#')
				{
					return new PatchStyle((UInt32Color)JsonToMarkupValue(typeof(UInt32Color), jsonToken));
				}
				return new PatchStyle(text);
			}
			if (type.IsEnum)
			{
				try
				{
					return Enum.Parse(type, text);
				}
				catch
				{
				}
			}
			break;
		}
		case 5:
			if (type == typeof(int))
			{
				return (int)jsonToken;
			}
			if (type == typeof(float))
			{
				return (float)jsonToken;
			}
			if (type == typeof(double))
			{
				return (double)jsonToken;
			}
			if (type == typeof(decimal))
			{
				return (decimal)jsonToken;
			}
			break;
		case 6:
			if (type == typeof(float))
			{
				return (float)jsonToken;
			}
			if (type == typeof(double))
			{
				return (double)jsonToken;
			}
			if (type == typeof(decimal))
			{
				return (decimal)jsonToken;
			}
			break;
		case 8:
			if (type == typeof(bool))
			{
				return (bool)jsonToken;
			}
			break;
		case 1:
			if (type.IsGenericType)
			{
				Type genericTypeDefinition = type.GetGenericTypeDefinition();
				if (genericTypeDefinition == typeof(List<>))
				{
					IList list = (IList)Activator.CreateInstance(type);
					Type type3 = type.GenericTypeArguments[0];
					foreach (JToken item in (JArray)jsonToken)
					{
						list.Add(JsonToMarkupValue(type3, item));
					}
					return list;
				}
				if (genericTypeDefinition == typeof(IReadOnlyList<>))
				{
					Type type4 = typeof(List<>).MakeGenericType(type.GetGenericArguments()[0]);
					IList list2 = (IList)Activator.CreateInstance(type4);
					Type type5 = type.GenericTypeArguments[0];
					foreach (JToken item2 in (JArray)jsonToken)
					{
						list2.Add(JsonToMarkupValue(type5, item2));
					}
					return list2;
				}
			}
			if (type.IsArray)
			{
				JArray val7 = (JArray)jsonToken;
				Array array = (Array)Activator.CreateInstance(type, ((JContainer)val7).Count);
				Type elementType = type.GetElementType();
				for (int i = 0; i < array.Length; i++)
				{
					array.SetValue(JsonToMarkupValue(elementType, val7[i]), i);
				}
				return array;
			}
			break;
		case 0:
		{
			JObject val2 = (JObject)jsonToken;
			JToken val3 = default(JToken);
			JToken val4 = default(JToken);
			if (val2.TryGetValue("$Document", ref val3) && val2.TryGetValue("@Value", ref val4))
			{
				if (!Interface.InGameCustomUIProvider.TryGetDocument((string)val3, out var document))
				{
					throw new Exception($"Failed to find document {val3}");
				}
				if (!document.RootNode.TryResolveAs(new ResolutionContext(this), document.RootNode.Data.LocalNamedExpressions[(string)val4], type, out var value))
				{
					throw new Exception($"Failed to resolve expession for root named value {val4}");
				}
				return value;
			}
			JToken val5 = default(JToken);
			if (type == typeof(string) && val2.TryGetValue("MessageId", ref val5))
			{
				Dictionary<string, string> dictionary = null;
				JToken val6 = default(JToken);
				if (val2.TryGetValue("Params", ref val6))
				{
					dictionary = new Dictionary<string, string>();
					foreach (KeyValuePair<string, JToken> item3 in (JObject)val6)
					{
						dictionary.Add(item3.Key, (string)item3.Value);
					}
				}
				return GetText((string)val5, dictionary);
			}
			if (type == typeof(IList<Label.LabelSpan>))
			{
				return FormattedMessageConverter.GetLabelSpans(((JToken)val2).ToObject<FormattedMessage>(), Interface);
			}
			object obj = Activator.CreateInstance(type);
			foreach (KeyValuePair<string, JToken> item4 in val2)
			{
				GetFieldOrProperty(type, item4.Key, out var fieldInfo, out var propertyInfo);
				if (fieldInfo == null && propertyInfo == null)
				{
					throw new Exception($"Property {item4.Key} does not exist on {type}");
				}
				object value2 = JsonToMarkupValue(fieldInfo?.FieldType ?? propertyInfo.PropertyType, item4.Value);
				if (fieldInfo != null)
				{
					fieldInfo.SetValue(obj, value2);
				}
				else
				{
					propertyInfo.SetValue(obj, value2);
				}
			}
			return obj;
		}
		}
		throw new Exception($"Failed to convert JSON value ({jsonToken.Type}) to specified type ({type.Name})");
	}

	public CustomUIProvider(Interface @interface)
	{
		Interface = @interface;
	}

	protected override void DoDispose()
	{
		DisposeTextures();
	}

	public string FormatNumber(int value)
	{
		return Interface.FormatNumber(value);
	}

	public string FormatNumber(float value)
	{
		return Interface.FormatNumber(value);
	}

	public string FormatNumber(double value)
	{
		return Interface.FormatNumber(value);
	}

	public string FormatRelativeTime(DateTime time)
	{
		return Interface.FormatRelativeTime(time);
	}

	public FontFamily GetFontFamily(string name)
	{
		return Interface.GetFontFamily(name);
	}

	public string GetText(string key, Dictionary<string, string> parameters = null, bool returnFallback = true)
	{
		return Interface.GetServerText(key, parameters, returnFallback);
	}

	public void LoadDocuments()
	{
		_documentsLibrary.Clear();
		foreach (KeyValuePair<string, string> item in Interface.App.InGame.Instance.HashesByServerAssetPath)
		{
			if (item.Key.StartsWith("UI/Custom/") && item.Key.EndsWith(".ui"))
			{
				string text = item.Key.Substring("UI/Custom/".Length);
				Document value = DocumentParser.Parse(File.ReadAllText(AssetManager.GetAssetLocalPathUsingHash(item.Value)), text);
				_documentsLibrary.Add(text, value);
			}
		}
		foreach (KeyValuePair<string, Document> item2 in _documentsLibrary)
		{
			item2.Value.ResolveProperties(this);
		}
	}

	public void ClearDocuments()
	{
		_documentsLibrary.Clear();
	}

	public bool TryGetDocument(string path, out Document document)
	{
		return _documentsLibrary.TryGetValue(path, out document);
	}

	public void PlaySound(SoundStyle sound)
	{
		Engine engine = Interface.Engine;
		if (!engine.Audio.ResourceManager.WwiseEventIds.TryGetValue(sound.SoundPath.Value, out var value))
		{
			Logger.Warn("Unknown custom UI sound: {0}", sound.SoundPath.Value);
		}
		else
		{
			engine.Audio.PostEvent(value, AudioDevice.PlayerSoundObjectReference);
		}
	}

	public void LoadTextures(bool use2x)
	{
		_atlas?.Dispose();
		_atlasTextureAreas.Clear();
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>();
		ConcurrentDictionary<string, string> hashesByServerAssetPath = Interface.App.InGame.Instance.HashesByServerAssetPath;
		foreach (KeyValuePair<string, string> item3 in hashesByServerAssetPath)
		{
			if (item3.Key.StartsWith("UI/Custom/") && item3.Key.EndsWith(".png"))
			{
				hashSet.Add(item3.Key);
			}
		}
		foreach (string item4 in hashSet)
		{
			bool flag = item4.EndsWith("@2x.png");
			if (!use2x && flag)
			{
				string item = item4.Replace("@2x.png", ".png");
				if (hashSet.Contains(item))
				{
					continue;
				}
			}
			else if (use2x && !flag)
			{
				string item2 = item4.Replace(".png", "@2x.png");
				if (hashSet.Contains(item2))
				{
					continue;
				}
			}
			list.Add(item4);
		}
		int num = (use2x ? 8192 : 4096);
		_atlas = new Texture(Texture.TextureTypes.Texture2D);
		_atlas.CreateTexture2D(num, num, null, 0, GL.LINEAR_MIPMAP_LINEAR, GL.LINEAR);
		List<Image> list2 = new List<Image>();
		list2.Add(BaseInterface.MakeWhitePixelImage("Special:WhitePixel"));
		list2.Add(BaseInterface.MakeMissingImage("Special:Missing"));
		foreach (string item5 in list)
		{
			hashesByServerAssetPath.TryGetValue(item5, out var value);
			list2.Add(new Image(item5.Substring("UI/Custom/".Length), AssetManager.GetAssetUsingHash(value)));
		}
		list2.Sort(delegate(Image a, Image b)
		{
			int height = b.Height;
			return height.CompareTo(a.Height);
		});
		Dictionary<Image, Point> imageLocations;
		byte[] atlasPixels = Image.Pack(num, list2, out imageLocations, doPadding: true);
		_atlas.UpdateTexture2DMipMaps(Texture.BuildMipmapPixels(atlasPixels, num, _atlas.MipmapLevelCount));
		foreach (KeyValuePair<Image, Point> item6 in imageLocations)
		{
			Image key = item6.Key;
			Point value2 = item6.Value;
			string text = key.Name.Replace("\\", "/");
			int num2 = ((!text.EndsWith("@2x.png")) ? 1 : 2);
			if (num2 == 2)
			{
				text = text.Replace("@2x.png", ".png");
			}
			_atlasTextureAreas.Add(text, new TextureArea(_atlas, value2.X, value2.Y, key.Width, key.Height, num2));
		}
		WhitePixel = MakeTextureArea("Special:WhitePixel");
		MissingTexture = MakeTextureArea("Special:Missing");
	}

	public void DisposeTextures()
	{
		_atlas?.Dispose();
		_atlasTextureAreas.Clear();
	}

	public TextureArea MakeTextureArea(string path)
	{
		if (_atlasTextureAreas.TryGetValue(path, out var value))
		{
			return value.Clone();
		}
		return MissingTexture;
	}
}
