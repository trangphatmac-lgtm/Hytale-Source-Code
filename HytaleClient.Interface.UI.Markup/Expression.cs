using System;
using System.Collections.Generic;
using System.Reflection;
using HytaleClient.Graphics;

namespace HytaleClient.Interface.UI.Markup;

public class Expression
{
	public class ExpressionResolutionException : TextParser.TextParserException
	{
		public readonly Expression Expression;

		public ExpressionResolutionException(string message, Expression expression, ExpressionResolutionException inner = null)
			: base(message, expression.SourceSpan, inner)
		{
			Expression = expression;
		}
	}

	public class ExpressionDictionary
	{
		public string TypeName;

		public readonly List<Expression> SpreadReferences = new List<Expression>();

		public readonly Dictionary<string, Expression> Entries = new Dictionary<string, Expression>();

		public DocumentNode ScopeNode;
	}

	public enum ExpressionOperatorCategory
	{
		None,
		Arithmetic,
		Comparison,
		BooleanLogic,
		Other
	}

	public enum ExpressionOperator
	{
		None,
		Sequence,
		Multiply,
		Divide,
		Add,
		Subtract,
		EqualTo,
		NotEqualTo,
		GreaterThan,
		LessThan,
		GreaterThanOrEqualTo,
		LessThanOrEqualTo,
		And,
		Or,
		MemberAccess
	}

	public class Identifier
	{
		public readonly string Prefix;

		public readonly string Text;

		public Identifier(string prefix, string text)
		{
			Prefix = prefix;
			Text = text;
		}
	}

	public static readonly Dictionary<string, Type> RegisteredDataTypes;

	public static readonly Dictionary<string, ExpressionOperator> Operators;

	public const int SequenceOperatorPrecedence = 1;

	public static readonly Dictionary<string, int> OperatorPrecedences;

	public TextParserSpan SourceSpan;

	public readonly decimal? DecimalValue;

	public readonly bool? BooleanValue;

	public readonly string StringValue;

	public readonly Identifier IdentifierValue;

	public readonly UInt32Color? ColorValue;

	public readonly ExpressionDictionary ObjectDictionary;

	public readonly DocumentNode DocumentNode;

	public readonly ExpressionOperator Operator;

	public readonly ExpressionOperatorCategory OperatorCategory;

	public readonly Expression Left;

	public readonly Expression Right;

	static Expression()
	{
		RegisteredDataTypes = new Dictionary<string, Type>();
		Operators = new Dictionary<string, ExpressionOperator>
		{
			{
				",",
				ExpressionOperator.Sequence
			},
			{
				"*",
				ExpressionOperator.Multiply
			},
			{
				"/",
				ExpressionOperator.Divide
			},
			{
				"+",
				ExpressionOperator.Add
			},
			{
				"-",
				ExpressionOperator.Subtract
			},
			{
				"==",
				ExpressionOperator.EqualTo
			},
			{
				"!=",
				ExpressionOperator.NotEqualTo
			},
			{
				">",
				ExpressionOperator.GreaterThan
			},
			{
				"<",
				ExpressionOperator.LessThan
			},
			{
				"<=",
				ExpressionOperator.GreaterThanOrEqualTo
			},
			{
				">=",
				ExpressionOperator.LessThanOrEqualTo
			},
			{
				"&&",
				ExpressionOperator.And
			},
			{
				"||",
				ExpressionOperator.Or
			},
			{
				".",
				ExpressionOperator.MemberAccess
			}
		};
		OperatorPrecedences = new Dictionary<string, int>
		{
			{ ",", 1 },
			{ "||", 2 },
			{ "&&", 3 },
			{ "!=", 4 },
			{ "==", 4 },
			{ "<", 5 },
			{ ">", 5 },
			{ "<=", 5 },
			{ ">=", 5 },
			{ "+", 6 },
			{ "-", 6 },
			{ "*", 7 },
			{ "/", 7 },
			{ ".", 8 }
		};
		Type[] types = Assembly.GetExecutingAssembly().GetTypes();
		foreach (Type type in types)
		{
			if (type.GetCustomAttribute<UIMarkupDataAttribute>() != null)
			{
				RegisteredDataTypes.Add(type.Name, type);
			}
		}
	}

	private Expression(TextParserSpan span)
	{
		SourceSpan = span;
	}

	public Expression(decimal value, TextParserSpan span)
		: this(span)
	{
		DecimalValue = value;
	}

	public Expression(bool value, TextParserSpan span)
		: this(span)
	{
		BooleanValue = value;
	}

	public Expression(string value, TextParserSpan span)
		: this(span)
	{
		StringValue = value;
	}

	public Expression(Identifier value, TextParserSpan span)
		: this(span)
	{
		IdentifierValue = value;
	}

	public Expression(UInt32Color value, TextParserSpan span)
		: this(span)
	{
		ColorValue = value;
	}

	public Expression(ExpressionDictionary dictionary, TextParserSpan span)
		: this(span)
	{
		ObjectDictionary = dictionary;
	}

	public Expression(DocumentNode node, TextParserSpan span)
		: this(span)
	{
		DocumentNode = node;
	}

	public Expression(ExpressionOperator op, Expression left, Expression right, TextParserSpan span)
		: this(span)
	{
		Operator = op;
		if (op >= ExpressionOperator.Multiply && op <= ExpressionOperator.Subtract)
		{
			OperatorCategory = ExpressionOperatorCategory.Arithmetic;
		}
		else if (op >= ExpressionOperator.EqualTo && op <= ExpressionOperator.LessThanOrEqualTo)
		{
			OperatorCategory = ExpressionOperatorCategory.Comparison;
		}
		else if (op >= ExpressionOperator.And && op <= ExpressionOperator.Or)
		{
			OperatorCategory = ExpressionOperatorCategory.BooleanLogic;
		}
		else if (op >= ExpressionOperator.MemberAccess)
		{
			OperatorCategory = ExpressionOperatorCategory.Other;
		}
		Left = left;
		Right = right;
	}

	public static bool TryResolvePath(string relativePath, string basePath, out string resolvedPath)
	{
		resolvedPath = null;
		List<string> list = new List<string>(basePath.Split(new char[1] { '/' }));
		list.RemoveAt(list.Count - 1);
		string[] array = relativePath.Split(new char[1] { '/' });
		foreach (string text in array)
		{
			if (text == "..")
			{
				if (list.Count == 0)
				{
					return false;
				}
				list.RemoveAt(list.Count - 1);
			}
			else
			{
				list.Add(text);
			}
		}
		resolvedPath = string.Join("/", list);
		return true;
	}
}
