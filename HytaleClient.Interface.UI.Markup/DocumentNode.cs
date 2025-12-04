using System;
using System.Collections.Generic;
using System.Reflection;
using HytaleClient.Graphics;
using HytaleClient.Interface.UI.Styles;

namespace HytaleClient.Interface.UI.Markup;

public class DocumentNode
{
	public class NodeData
	{
		public readonly Dictionary<string, Expression> LocalNamedExpressions = new Dictionary<string, Expression>();

		public readonly Dictionary<string, Expression> PropertyExpressions = new Dictionary<string, Expression>();

		public readonly Dictionary<string, object> PropertyValues = new Dictionary<string, object>();

		public readonly List<DocumentNode> Children = new List<DocumentNode>();
	}

	public class NodeTemplateSetup
	{
		public Expression NamedTemplateExpression;

		public readonly Dictionary<string, NodeData> Inserts = new Dictionary<string, NodeData>();
	}

	public TextParserSpan SourceSpan;

	public ElementClassInfo ElementClassInfo;

	public string Name;

	public DocumentNode ParentNode;

	public NodeTemplateSetup TemplateSetup;

	public DocumentNode SourceTemplateNode;

	public NodeData Data = new NodeData();

	public string SourcePath => SourceSpan.Parser.SourcePath;

	public bool TryGetNamedExpression(string name, HashSet<Expression> already, out Expression expression)
	{
		if (!Data.LocalNamedExpressions.TryGetValue(name, out expression))
		{
			return false;
		}
		if (!already.Add(expression))
		{
			throw new Expression.ExpressionResolutionException("Cyclic reference detected.", expression);
		}
		return true;
	}

	public DocumentNode Clone(DocumentNode newParentNode)
	{
		DocumentNode documentNode = new DocumentNode
		{
			SourceSpan = SourceSpan,
			TemplateSetup = TemplateSetup,
			ElementClassInfo = ElementClassInfo,
			Name = Name,
			ParentNode = newParentNode
		};
		foreach (KeyValuePair<string, Expression> localNamedExpression in Data.LocalNamedExpressions)
		{
			documentNode.Data.LocalNamedExpressions.Add(localNamedExpression.Key, localNamedExpression.Value);
		}
		foreach (KeyValuePair<string, Expression> propertyExpression in Data.PropertyExpressions)
		{
			documentNode.Data.PropertyExpressions.Add(propertyExpression.Key, propertyExpression.Value);
		}
		foreach (DocumentNode child in Data.Children)
		{
			documentNode.Data.Children.Add(child.Clone(documentNode));
		}
		return documentNode;
	}

	public bool TryResolve<T>(ResolutionContext context, Expression expression, out T value)
	{
		if (DoTryResolve<T>(context, expression, out value))
		{
			return true;
		}
		if (SourceTemplateNode != null && SourceTemplateNode.TryResolve<T>(context, expression, out value))
		{
			return true;
		}
		if (ParentNode != null && ParentNode.TryResolve<T>(context, expression, out value))
		{
			return true;
		}
		return false;
	}

	public bool TryResolveAs(ResolutionContext context, Expression expression, Type type, out object value)
	{
		if (DoTryResolveAs(context, expression, type, out value))
		{
			return true;
		}
		if (SourceTemplateNode != null && SourceTemplateNode.TryResolveAs(context, expression, type, out value))
		{
			return true;
		}
		if (ParentNode != null && ParentNode.TryResolveAs(context, expression, type, out value))
		{
			return true;
		}
		return false;
	}

	private bool DoTryResolve<T>(ResolutionContext context, Expression expression, out T value)
	{
		if (TryResolveAs(context, expression, typeof(T), out var value2))
		{
			if (typeof(T) == typeof(decimal) && value2.GetType() == typeof(int))
			{
				value = (T)(object)(decimal)(int)value2;
			}
			else
			{
				value = (T)value2;
			}
			return true;
		}
		value = default(T);
		return false;
	}

	private bool DoTryResolveAs(ResolutionContext context, Expression expression, Type type, out object value)
	{
		type = Nullable.GetUnderlyingType(type) ?? type;
		if (expression.Operator == Expression.ExpressionOperator.MemberAccess)
		{
			if (expression.Left.IdentifierValue?.Prefix == "$" && expression.Right.IdentifierValue.Prefix == "@")
			{
				string sourcePath = expression.SourceSpan.Parser.SourcePath;
				if (!context.Provider.TryGetDocument(sourcePath, out var document) || !document.NamedDocumentReferences.TryGetValue(expression.Left.IdentifierValue.Text, out var value2))
				{
					throw new Expression.ExpressionResolutionException("Could not find a document reference named " + expression.Left.IdentifierValue.Text, expression.Left);
				}
				if (!context.Provider.TryGetDocument(value2, out var document2))
				{
					throw new Expression.ExpressionResolutionException("Could not find a document with path " + value2, expression.Left);
				}
				if (!document2.RootNode.TryGetNamedExpression(expression.Right.IdentifierValue.Text, context.ExpressionPath, out var expression2))
				{
					throw new Expression.ExpressionResolutionException("Could not find an expression named " + expression.Right.IdentifierValue.Text + " in document " + value2, expression);
				}
				context.ExpressionPath.Add(expression2);
				bool result = document2.RootNode.TryResolveAs(context, expression2, type, out value);
				context.ExpressionPath.Remove(expression2);
				return result;
			}
			if (expression.Right.IdentifierValue.Prefix == null)
			{
				if (!TryResolveAs(context, expression.Left, typeof(object), out var value3))
				{
					throw new Expression.ExpressionResolutionException("Failed to resolve left-hand side of member access expression", expression.Left);
				}
				FieldInfo field = value3.GetType().GetField(expression.Right.IdentifierValue.Text);
				if (field == null)
				{
					throw new Expression.ExpressionResolutionException("Could not find a field named " + expression.Right.IdentifierValue.Text, expression);
				}
				value = field.GetValue(value3);
				return true;
			}
		}
		else if (expression.IdentifierValue != null)
		{
			switch (expression.IdentifierValue.Prefix)
			{
			case "@":
			{
				if (TryGetNamedExpression(expression.IdentifierValue.Text, context.ExpressionPath, out var expression3))
				{
					context.ExpressionPath.Add(expression3);
					bool result2 = TryResolveAs(context, expression3, type, out value);
					context.ExpressionPath.Remove(expression3);
					return result2;
				}
				break;
			}
			case "%":
				value = context.Provider.GetText(expression.IdentifierValue.Text);
				return true;
			case null:
				if (type.IsEnum)
				{
					try
					{
						value = Enum.Parse(type, expression.IdentifierValue.Text);
						return true;
					}
					catch
					{
					}
				}
				break;
			}
			value = null;
			return false;
		}
		object newValue;
		decimal value20;
		if (type == typeof(UInt32Color))
		{
			if (expression.ColorValue.HasValue)
			{
				value = expression.ColorValue.Value;
				return true;
			}
		}
		else if (type == typeof(decimal))
		{
			if (expression.DecimalValue.HasValue)
			{
				value = expression.DecimalValue.Value;
				return true;
			}
			if (expression.OperatorCategory == Expression.ExpressionOperatorCategory.Arithmetic && TryResolve<decimal>(context, expression.Left, out var value4) && TryResolve<decimal>(context, expression.Right, out var value5))
			{
				switch (expression.Operator)
				{
				case Expression.ExpressionOperator.Add:
					value = value4 + value5;
					return true;
				case Expression.ExpressionOperator.Subtract:
					value = value4 - value5;
					return true;
				case Expression.ExpressionOperator.Multiply:
					value = value4 * value5;
					return true;
				case Expression.ExpressionOperator.Divide:
					value = value4 / value5;
					return true;
				}
			}
		}
		else if (type == typeof(float))
		{
			if (TryResolve<decimal>(context, expression, out var value6))
			{
				value = (float)value6;
				return true;
			}
		}
		else if (type == typeof(int))
		{
			if (TryResolve<decimal>(context, expression, out var value7))
			{
				value = (int)System.Math.Round(value7, MidpointRounding.AwayFromZero);
				return true;
			}
		}
		else if (type == typeof(bool))
		{
			if (expression.BooleanValue.HasValue)
			{
				value = expression.BooleanValue.Value;
				return true;
			}
			switch (expression.OperatorCategory)
			{
			case Expression.ExpressionOperatorCategory.Comparison:
			{
				if (TryResolve<decimal>(context, expression.Left, out var value10) && TryResolve<decimal>(context, expression.Right, out var value11))
				{
					switch (expression.Operator)
					{
					case Expression.ExpressionOperator.EqualTo:
						value = value10 == value11;
						return true;
					case Expression.ExpressionOperator.NotEqualTo:
						value = value10 != value11;
						return true;
					case Expression.ExpressionOperator.GreaterThan:
						value = value10 > value11;
						return true;
					case Expression.ExpressionOperator.LessThan:
						value = value10 < value11;
						return true;
					case Expression.ExpressionOperator.GreaterThanOrEqualTo:
						value = value10 >= value11;
						return true;
					case Expression.ExpressionOperator.LessThanOrEqualTo:
						value = value10 <= value11;
						return true;
					}
				}
				break;
			}
			case Expression.ExpressionOperatorCategory.BooleanLogic:
			{
				if (TryResolve<bool>(context, expression.Left, out var value8) && TryResolve<bool>(context, expression.Right, out var value9))
				{
					switch (expression.Operator)
					{
					case Expression.ExpressionOperator.And:
						value = value8 && value9;
						return true;
					case Expression.ExpressionOperator.Or:
						value = value8 || value9;
						return true;
					}
				}
				break;
			}
			}
		}
		else if (type == typeof(UIPath))
		{
			if (TryResolve<string>(context, expression, out var value12))
			{
				DocumentNode documentNode = this;
				while (documentNode.SourceTemplateNode != null)
				{
					documentNode = documentNode.SourceTemplateNode;
				}
				if (!Expression.TryResolvePath(value12, documentNode.SourcePath, out var resolvedPath))
				{
					throw new Expression.ExpressionResolutionException("Could not resolve relative path: " + value12, expression);
				}
				value = new UIPath(resolvedPath);
				return true;
			}
		}
		else if (type == typeof(UIFontName))
		{
			if (TryResolve<string>(context, expression, out var value13))
			{
				if (context.Provider.GetFontFamily(value13) == null)
				{
					throw new Expression.ExpressionResolutionException("Font does not exist: " + value13, expression);
				}
				value = new UIFontName(value13);
				return true;
			}
		}
		else if (type == typeof(char))
		{
			if (expression.StringValue != null && expression.StringValue.Length == 1)
			{
				value = expression.StringValue[0];
				return true;
			}
		}
		else if (type == typeof(string))
		{
			if (expression.StringValue != null)
			{
				value = expression.StringValue;
				return true;
			}
			if (expression.Operator == Expression.ExpressionOperator.Add && TryResolve<string>(context, expression.Left, out var value14) && TryResolve<string>(context, expression.Right, out var value15))
			{
				value = value14 + value15;
				return true;
			}
		}
		else if (type == typeof(DocumentNode))
		{
			if (expression.DocumentNode != null)
			{
				value = expression.DocumentNode;
				return true;
			}
		}
		else if (expression.ObjectDictionary != null)
		{
			if (type == typeof(Expression.ExpressionDictionary))
			{
				value = expression.ObjectDictionary;
				return true;
			}
			if (!Expression.RegisteredDataTypes.TryGetValue(expression.ObjectDictionary.TypeName ?? type.Name, out var value16))
			{
				throw new Expression.ExpressionResolutionException("Tried to resolve expression to an unregistered markup data type " + (expression.ObjectDictionary.TypeName ?? type.Name), expression);
			}
			if (value16 != type)
			{
				if (!(type == typeof(object)))
				{
					throw new Expression.ExpressionResolutionException("Type mismatch between " + value16.FullName + " and " + type.FullName, expression);
				}
				type = value16;
			}
			if (type.Name == expression.ObjectDictionary.TypeName || type == typeof(object) || expression.ObjectDictionary.TypeName == null)
			{
				newValue = Activator.CreateInstance(type);
				ApplyRecursive(expression.ObjectDictionary, this, expression);
				value = newValue;
				return true;
			}
		}
		else if (type == typeof(PatchStyle))
		{
			if (TryResolve<UIPath>(context, expression, out var value17))
			{
				value = new PatchStyle
				{
					TexturePath = value17
				};
				return true;
			}
			if (TryResolve<UInt32Color>(context, expression, out var value18))
			{
				value = new PatchStyle
				{
					Color = value18
				};
				return true;
			}
		}
		else if (type == typeof(SoundStyle))
		{
			if (TryResolve<UIPath>(context, expression, out var value19))
			{
				value = new SoundStyle
				{
					SoundPath = value19
				};
				return true;
			}
		}
		else if (type == typeof(Padding) && TryResolve<decimal>(context, expression, out value20))
		{
			value = new Padding((int)System.Math.Round(value20, MidpointRounding.AwayFromZero));
			return true;
		}
		value = null;
		return false;
		void ApplyRecursive(Expression.ExpressionDictionary dictionary, DocumentNode scopeNode, Expression dictionaryExpression)
		{
			foreach (Expression spreadReference in dictionary.SpreadReferences)
			{
				if (!scopeNode.TryResolve<Expression.ExpressionDictionary>(context, spreadReference, out var value21))
				{
					throw new Expression.ExpressionResolutionException("Could not resolve spread expression to type " + type.Name, expression);
				}
				ApplyRecursive(value21, value21.ScopeNode, spreadReference);
			}
			if (type.Name != dictionary.TypeName && type != typeof(object) && dictionary.TypeName != null)
			{
				throw new Expression.ExpressionResolutionException("Cannot resolve dictionary of type " + dictionary.TypeName + " to type " + type.Name, dictionaryExpression);
			}
			foreach (KeyValuePair<string, Expression> entry in dictionary.Entries)
			{
				FieldInfo field2 = type.GetField(entry.Key, BindingFlags.Instance | BindingFlags.Public);
				PropertyInfo property = type.GetProperty(entry.Key, BindingFlags.Instance | BindingFlags.Public);
				Type type2;
				if (field2 != null)
				{
					type2 = field2.FieldType;
				}
				else
				{
					if (!(property != null))
					{
						throw new Expression.ExpressionResolutionException("Could not find field " + entry.Key + " in type " + type.Name, entry.Value);
					}
					type2 = property.PropertyType;
				}
				type2 = Nullable.GetUnderlyingType(type2) ?? type2;
				if (!scopeNode.TryResolveAs(context, entry.Value, type2, out var value22))
				{
					throw new Expression.ExpressionResolutionException("Could not resolve expression for property " + entry.Key + " to type " + type2.Name, entry.Value);
				}
				if (field2 != null)
				{
					field2.SetValue(newValue, value22);
				}
				else
				{
					property.SetValue(newValue, value22);
				}
			}
		}
	}
}
