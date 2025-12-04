using System;
using System.Collections.Generic;
using System.Reflection;
using HytaleClient.Interface.UI.Elements;

namespace HytaleClient.Interface.UI.Markup;

public class Document
{
	public readonly DocumentNode RootNode;

	public readonly Dictionary<string, string> NamedDocumentReferences = new Dictionary<string, string>();

	public static readonly Dictionary<string, ElementClassInfo> ElementClassInfos;

	public Document(TextParser parser)
	{
		RootNode = new DocumentNode
		{
			SourceSpan = new TextParserSpan(0, parser.Data.Length, parser)
		};
	}

	static Document()
	{
		ElementClassInfos = new Dictionary<string, ElementClassInfo>();
		Type[] types = Assembly.GetExecutingAssembly().GetTypes();
		foreach (Type type in types)
		{
			if (!type.IsClass || type.IsAbstract || !typeof(Element).IsAssignableFrom(type))
			{
				continue;
			}
			UIMarkupElementAttribute customAttribute = type.GetCustomAttribute<UIMarkupElementAttribute>(inherit: false);
			if (customAttribute == null)
			{
				continue;
			}
			ElementClassInfo elementClassInfo = new ElementClassInfo
			{
				Name = type.Name,
				AcceptsChildren = customAttribute.AcceptsChildren
			};
			ConstructorInfo[] constructors = type.GetConstructors();
			foreach (ConstructorInfo constructorInfo in constructors)
			{
				ParameterInfo[] parameters = constructorInfo.GetParameters();
				if (parameters.Length == 2 && !(parameters[0].ParameterType != typeof(Desktop)) && !(parameters[1].ParameterType != typeof(Element)))
				{
					elementClassInfo.Constructor = constructorInfo;
					break;
				}
			}
			if (elementClassInfo.Constructor == null)
			{
				throw new Exception(type.FullName + " has no constructor matching requirements for elements");
			}
			MemberInfo[] members = type.GetMembers(BindingFlags.Instance | BindingFlags.Public);
			foreach (MemberInfo memberInfo in members)
			{
				UIMarkupPropertyAttribute customAttribute2 = memberInfo.GetCustomAttribute<UIMarkupPropertyAttribute>();
				if (customAttribute2 != null && (customAttribute.ExposeInheritedProperties || memberInfo.DeclaringType == type))
				{
					if (memberInfo is FieldInfo fieldInfo)
					{
						elementClassInfo.PropertyTypes.Add(fieldInfo.Name, fieldInfo.FieldType);
					}
					else if (memberInfo is PropertyInfo propertyInfo)
					{
						elementClassInfo.PropertyTypes.Add(propertyInfo.Name, propertyInfo.PropertyType);
					}
				}
			}
			ElementClassInfos.Add(type.Name, elementClassInfo);
		}
	}

	public UIFragment Instantiate(Desktop desktop, Element root)
	{
		UIFragment fragment = new UIFragment();
		fragment.RootElements = Walk(RootNode.Data.Children, root);
		return fragment;
		static object CloneMarkupPropertyValue(object value)
		{
			if (value == null)
			{
				return null;
			}
			Type type = value.GetType();
			if (type.IsValueType || type == typeof(string))
			{
				return value;
			}
			if (type.GetCustomAttribute<UIMarkupDataAttribute>() == null)
			{
				return value;
			}
			object obj = Activator.CreateInstance(type);
			FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo[] array = fields;
			foreach (FieldInfo fieldInfo in array)
			{
				fieldInfo.SetValue(obj, CloneMarkupPropertyValue(fieldInfo.GetValue(value)));
			}
			return obj;
		}
		List<Element> Walk(List<DocumentNode> nodes, Element parent)
		{
			List<Element> list = new List<Element>();
			foreach (DocumentNode node in nodes)
			{
				Element element = (Element)node.ElementClassInfo.Constructor.Invoke(new object[2] { desktop, null });
				element.Name = node.Name;
				Type type2 = element.GetType();
				foreach (KeyValuePair<string, object> propertyValue in node.Data.PropertyValues)
				{
					object value2 = CloneMarkupPropertyValue(propertyValue.Value);
					FieldInfo field = type2.GetField(propertyValue.Key);
					if (field != null)
					{
						field.SetValue(element, value2);
					}
					else
					{
						type2.GetProperty(propertyValue.Key).SetValue(element, value2);
					}
				}
				list.Add(element);
				if (node.Name != null && !fragment.ElementsByName.ContainsKey(node.Name))
				{
					fragment.ElementsByName.Add(node.Name, element);
				}
				if (node.ElementClassInfo.AcceptsChildren)
				{
					Walk(node.Data.Children, element);
				}
				parent?.AddFromMarkup(element);
			}
			return list;
		}
	}

	public void ResolveProperties(IUIProvider provider)
	{
		ResolutionContext context = new ResolutionContext(provider);
		Walk(RootNode);
		static void ApplyInsert(DocumentNode node, DocumentNode.NodeData insert)
		{
			foreach (DocumentNode child in insert.Children)
			{
				child.ParentNode = node;
				node.Data.Children.Add(child);
			}
			foreach (KeyValuePair<string, Expression> localNamedExpression in insert.LocalNamedExpressions)
			{
				node.Data.LocalNamedExpressions[localNamedExpression.Key] = localNamedExpression.Value;
			}
			foreach (KeyValuePair<string, Expression> propertyExpression in insert.PropertyExpressions)
			{
				node.Data.PropertyExpressions[propertyExpression.Key] = propertyExpression.Value;
			}
		}
		static DocumentNode FindNodeByName(DocumentNode parent, string name)
		{
			foreach (DocumentNode child2 in parent.Data.Children)
			{
				if (child2.Name == name)
				{
					return child2;
				}
				DocumentNode documentNode = FindNodeByName(child2, name);
				if (documentNode != null)
				{
					return documentNode;
				}
			}
			return null;
		}
		void Walk(DocumentNode node)
		{
			if (node.TemplateSetup != null)
			{
				DocumentNode.NodeTemplateSetup templateSetup = node.TemplateSetup;
				node.TemplateSetup = null;
				DocumentNode.NodeData data = node.Data;
				if (!node.ParentNode.TryResolve<DocumentNode>(context, templateSetup.NamedTemplateExpression, out node.SourceTemplateNode))
				{
					throw new Expression.ExpressionResolutionException("Could not resolve template node", templateSetup.NamedTemplateExpression);
				}
				node.ElementClassInfo = node.SourceTemplateNode.ElementClassInfo;
				node.Data = new DocumentNode.NodeData();
				foreach (DocumentNode child3 in node.SourceTemplateNode.Data.Children)
				{
					DocumentNode item = child3.Clone(node);
					node.Data.Children.Add(item);
				}
				foreach (KeyValuePair<string, Expression> localNamedExpression2 in node.SourceTemplateNode.Data.LocalNamedExpressions)
				{
					node.Data.LocalNamedExpressions.Add(localNamedExpression2.Key, localNamedExpression2.Value);
				}
				foreach (KeyValuePair<string, Expression> propertyExpression2 in node.SourceTemplateNode.Data.PropertyExpressions)
				{
					node.Data.PropertyExpressions.Add(propertyExpression2.Key, propertyExpression2.Value);
				}
				ApplyInsert(node, data);
				foreach (KeyValuePair<string, DocumentNode.NodeData> insert in templateSetup.Inserts)
				{
					DocumentNode documentNode2 = FindNodeByName(node, insert.Key);
					if (documentNode2 == null)
					{
						throw new TextParser.TextParserException("Could not find node named " + insert.Key + " for insertion", node.SourceSpan);
					}
					ApplyInsert(documentNode2, insert.Value);
				}
			}
			foreach (KeyValuePair<string, Expression> propertyExpression3 in node.Data.PropertyExpressions)
			{
				if (!node.ElementClassInfo.PropertyTypes.TryGetValue(propertyExpression3.Key, out var value))
				{
					throw new TextParser.TextParserException("Unknown property " + propertyExpression3.Key + " on node of type " + node.ElementClassInfo.Name, node.SourceSpan);
				}
				if (!node.TryResolveAs(context, propertyExpression3.Value, value, out var value2))
				{
					Type type = Nullable.GetUnderlyingType(value) ?? value;
					throw new Expression.ExpressionResolutionException("Could not resolve expression for property " + propertyExpression3.Key + " to type " + type.Name, propertyExpression3.Value);
				}
				node.Data.PropertyValues.Add(propertyExpression3.Key, value2);
			}
			if (node.Data.Children.Count > 0 && node.ElementClassInfo != null && !node.ElementClassInfo.AcceptsChildren)
			{
				throw new TextParser.TextParserException("Node of type " + node.ElementClassInfo.Name + " can't have children.", node.SourceSpan);
			}
			foreach (DocumentNode child4 in node.Data.Children)
			{
				Walk(child4);
			}
		}
	}

	public T ResolveNamedValue<T>(IUIProvider provider, string name)
	{
		ResolutionContext context = new ResolutionContext(provider);
		if (!RootNode.TryResolve<T>(context, RootNode.Data.LocalNamedExpressions[name], out var value))
		{
			throw new Exception("Could not resolve expession for root named value " + name);
		}
		return value;
	}

	public bool TryResolveNamedValue<T>(IUIProvider provider, string name, out T value)
	{
		ResolutionContext context = new ResolutionContext(provider);
		if (!RootNode.Data.LocalNamedExpressions.TryGetValue(name, out var value2))
		{
			value = default(T);
			return false;
		}
		return RootNode.TryResolve<T>(context, value2, out value);
	}
}
