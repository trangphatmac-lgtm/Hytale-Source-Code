using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Markup;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.UI;

public class DocGen
{
	public static string GenerateMediaWikiPage()
	{
		XmlDocument doc = new XmlDocument();
		doc.Load("../../Documentation.xml");
		HashSet<Type> propertyTypes = new HashSet<Type>();
		HashSet<Type> enumTypes = new HashSet<Type>();
		string pageStr = "This is a generated list of UI elements accessible by markup. More information about the User Interface framework can be found [[User_Interface|here]].\n\n";
		Title("Elements", 1);
		List<ElementClassInfo> list = new List<ElementClassInfo>(Document.ElementClassInfos.Values);
		list.Sort((ElementClassInfo a, ElementClassInfo b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture));
		foreach (ElementClassInfo item in list)
		{
			string text = (item.Constructor.DeclaringType.IsGenericType ? item.Constructor.DeclaringType.GetGenericTypeDefinition().FullName : item.Constructor.DeclaringType.FullName);
			string description2 = GetXmlSummary("T:" + text);
			Title(item.Name, 2);
			Description(description2, item.AcceptsChildren);
			TableStart();
			foreach (MemberInfo publicTypeMember in GetPublicTypeMembers(item.Constructor.DeclaringType))
			{
				string text2 = (publicTypeMember.DeclaringType.IsGenericType ? publicTypeMember.DeclaringType.GetGenericTypeDefinition().FullName : publicTypeMember.DeclaringType.FullName);
				string text3 = text2 + "." + publicTypeMember.Name;
				string description3 = GetXmlSummary("P:" + text3) ?? GetXmlSummary("F:" + text3);
				Type type2 = null;
				MemberInfo memberInfo = publicTypeMember;
				MemberInfo memberInfo2 = memberInfo;
				if (!(memberInfo2 is FieldInfo fieldInfo))
				{
					if (memberInfo2 is PropertyInfo propertyInfo)
					{
						type2 = propertyInfo.PropertyType;
					}
				}
				else
				{
					type2 = fieldInfo.FieldType;
				}
				AddDataOrEnumType(type2);
				TableRow(publicTypeMember.Name, GetName(type2), description3);
			}
			TableEnd();
			List<Tuple<string, string>> list2 = GetEventCallbacks(item.Constructor.DeclaringType);
			if (list2.Count <= 0)
			{
				continue;
			}
			TableStart(hasType: false, "Event Callbacks");
			foreach (Tuple<string, string> item2 in list2)
			{
				TableRow(item2.Item1, null, item2.Item2);
			}
			TableEnd();
		}
		Title("Property Types", 1);
		List<Type> list3 = new List<Type>(propertyTypes);
		list3.Sort((Type a, Type b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture));
		foreach (Type item3 in list3)
		{
			string description4 = GetXmlSummary("T:" + item3.FullName);
			Title(item3.Name, 2);
			Description(description4);
			TableStart();
			FieldInfo[] fields = item3.GetFields();
			foreach (FieldInfo fieldInfo2 in fields)
			{
				if (fieldInfo2.IsPublic)
				{
					string description5 = GetXmlSummary("F:" + item3.FullName + "." + fieldInfo2.Name);
					TableRow(fieldInfo2.Name, GetName(fieldInfo2.FieldType), description5);
				}
			}
			PropertyInfo[] properties = item3.GetProperties();
			foreach (PropertyInfo propertyInfo2 in properties)
			{
				if (!(propertyInfo2.GetSetMethod() == null) && propertyInfo2.GetSetMethod().IsPublic)
				{
					string description6 = GetXmlSummary("P:" + item3.FullName + "." + propertyInfo2.Name);
					TableRow(propertyInfo2.Name, GetName(propertyInfo2.PropertyType), description6);
				}
			}
			TableEnd();
		}
		Title("Enums", 1);
		List<Type> list4 = new List<Type>(enumTypes);
		list4.Sort((Type a, Type b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCulture));
		foreach (Type item4 in enumTypes)
		{
			string description7 = GetXmlSummary("T:" + item4.FullName);
			Title(item4.Name, 2);
			Description(description7);
			TableStart(hasType: false);
			string[] names = Enum.GetNames(item4);
			foreach (string text4 in names)
			{
				string description8 = GetXmlSummary("F:" + item4.FullName + "." + text4);
				TableRow(text4, null, description8);
			}
			TableEnd();
		}
		return pageStr;
		void AddDataOrEnumType(Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(IList<>)))
			{
				type = type.GetGenericArguments()[0];
			}
			if (type.IsArray)
			{
				type = type.GetElementType();
			}
			if (type.IsEnum)
			{
				if (!enumTypes.Contains(type))
				{
					enumTypes.Add(type);
				}
			}
			else if (type.GetCustomAttribute<UIMarkupDataAttribute>() != null && !propertyTypes.Contains(type))
			{
				propertyTypes.Add(type);
				ScanDataType(type);
			}
		}
		void Description(string description, bool? children = null)
		{
			if (children.HasValue)
			{
				pageStr = pageStr + "Accepts child elements: ''" + (children.Value ? "Yes" : "No") + "''\n\n";
			}
			if (description != null)
			{
				pageStr = pageStr + description + "\n\n";
			}
		}
		List<Tuple<string, string>> GetEventCallbacks(Type type)
		{
			List<Tuple<string, string>> list5 = new List<Tuple<string, string>>();
			FieldInfo[] fields2 = type.GetFields();
			foreach (FieldInfo fieldInfo3 in fields2)
			{
				if (fieldInfo3.IsPublic && ((fieldInfo3.FieldType.IsGenericType && fieldInfo3.FieldType.GetGenericTypeDefinition() == typeof(Action<>)) || fieldInfo3.FieldType == typeof(Action)))
				{
					string xmlSummary = GetXmlSummary("F:" + fieldInfo3.DeclaringType.FullName + "." + fieldInfo3.Name);
					list5.Add(Tuple.Create(fieldInfo3.Name, xmlSummary));
				}
			}
			PropertyInfo[] properties2 = type.GetProperties();
			foreach (PropertyInfo propertyInfo3 in properties2)
			{
				if (!(propertyInfo3.GetSetMethod() == null) && propertyInfo3.GetSetMethod().IsPublic && ((propertyInfo3.PropertyType.IsGenericType && propertyInfo3.PropertyType.GetGenericTypeDefinition() == typeof(Action<>)) || propertyInfo3.PropertyType == typeof(Action)))
				{
					string xmlSummary2 = GetXmlSummary("P:" + propertyInfo3.DeclaringType.FullName + "." + propertyInfo3.Name);
					list5.Add(Tuple.Create(propertyInfo3.Name, xmlSummary2));
				}
			}
			return list5;
		}
		static string GetName(Type type)
		{
			type = Nullable.GetUnderlyingType(type) ?? type;
			if (type == typeof(int))
			{
				return "Integer";
			}
			if (type == typeof(string))
			{
				return "String";
			}
			if (type == typeof(double))
			{
				return "Double";
			}
			if (type == typeof(bool))
			{
				return "Boolean";
			}
			if (type == typeof(float))
			{
				return "Float";
			}
			if (type == typeof(long))
			{
				return "Long";
			}
			if (type == typeof(decimal))
			{
				return "Decimal";
			}
			if (type == typeof(UInt32Color))
			{
				return "[[User_Interface#Color|Color]]";
			}
			if (type == typeof(UIPath))
			{
				return "UI Path (String)";
			}
			if (type == typeof(UIFontName))
			{
				return "Font Name (String)";
			}
			string text5 = Link(type.Name) ?? "";
			if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(IList<>)))
			{
				string name2 = type.GetGenericArguments()[0].Name;
				text5 = "List<" + Link(name2) + ">";
			}
			else if (type.IsArray)
			{
				string name3 = type.GetElementType().Name;
				text5 = Link(name3) + "[]";
			}
			string text6 = text5;
			if (type == typeof(PatchStyle))
			{
				text6 += " / String";
			}
			return text6;
		}
		static List<MemberInfo> GetPublicTypeMembers(Type type)
		{
			List<MemberInfo> list6 = new List<MemberInfo>();
			UIMarkupElementAttribute customAttribute = type.GetCustomAttribute<UIMarkupElementAttribute>(inherit: false);
			if (customAttribute == null)
			{
				return list6;
			}
			MemberInfo[] members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public);
			foreach (MemberInfo memberInfo3 in members)
			{
				UIMarkupPropertyAttribute customAttribute2 = memberInfo3.GetCustomAttribute<UIMarkupPropertyAttribute>();
				if (customAttribute2 != null && (customAttribute.ExposeInheritedProperties || memberInfo3.DeclaringType == type) && (memberInfo3 is FieldInfo || memberInfo3 is PropertyInfo))
				{
					list6.Add(memberInfo3);
				}
			}
			return list6;
		}
		string GetXmlSummary(string nodePath)
		{
			XmlNode xmlNode = doc.SelectSingleNode("//member[@name='" + nodePath + "']");
			if (xmlNode != null)
			{
				XmlNode xmlNode2 = xmlNode.SelectSingleNode("summary");
				if (xmlNode2 == null)
				{
					return null;
				}
				string input = xmlNode2.InnerText.Trim();
				return Regex.Replace(input, "\\s+", " ");
			}
			return null;
		}
		static string Link(string p)
		{
			return "[[#" + p + "|" + p + "]]";
		}
		void ScanDataType(Type type)
		{
			FieldInfo[] fields3 = type.GetFields();
			foreach (FieldInfo fieldInfo4 in fields3)
			{
				if (fieldInfo4.IsPublic)
				{
					AddDataOrEnumType(fieldInfo4.FieldType);
				}
			}
			PropertyInfo[] properties3 = type.GetProperties();
			foreach (PropertyInfo propertyInfo4 in properties3)
			{
				if (!(propertyInfo4.GetSetMethod() == null) && propertyInfo4.GetSetMethod().IsPublic)
				{
					AddDataOrEnumType(propertyInfo4.PropertyType);
				}
			}
		}
		void TableEnd()
		{
			pageStr += "|}\n";
		}
		void TableRow(string name, string type, string description = null)
		{
			pageStr += "|-\n";
			pageStr = pageStr + "| '''" + name + "'''\n";
			if (type != null)
			{
				pageStr = pageStr + "| " + type + "\n";
			}
			pageStr = pageStr + "| " + description + "\n";
		}
		void TableStart(bool hasType = true, string title = "Properties")
		{
			pageStr = pageStr + "''' " + title + " '''\n";
			pageStr += "{| class=\"wikitable\"\n";
			pageStr += "|-\n";
			pageStr += "! scope=\"col\"| Name\n";
			if (hasType)
			{
				pageStr += "! scope=\"col\"| Type\n";
			}
			pageStr += "! scope=\"col\"| Description\n";
		}
		void Title(string str, int level)
		{
			switch (level)
			{
			case 1:
				pageStr = pageStr + "== " + str + " ==\n";
				break;
			case 2:
				pageStr = pageStr + "=== " + str + " ===\n";
				break;
			case 3:
				pageStr = pageStr + "==== " + str + " ====\n";
				break;
			default:
				throw new Exception("Invalid level");
			}
		}
	}
}
