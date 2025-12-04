#define DEBUG
using System.Diagnostics;
using System.Globalization;
using HytaleClient.Graphics;

namespace HytaleClient.Interface.UI.Markup;

internal class DocumentParser : TextParser
{
	public DocumentParser(string data, string sourcePath)
		: base(data, sourcePath)
	{
	}

	public static Document Parse(string data, string sourcePath)
	{
		DocumentParser documentParser = new DocumentParser(data, sourcePath);
		Document document = new Document(documentParser);
		while (true)
		{
			documentParser.TryEatWhitespaceOrComment();
			if (documentParser.IsEOF())
			{
				return document;
			}
			if (documentParser.TryEatNamedExpressionDeclaration(document.RootNode, document.RootNode.Data) || documentParser.TryEatNamedDocumentDeclaraction(document))
			{
				continue;
			}
			if (documentParser.TryEatNodeDefinition(out var node))
			{
				node.ParentNode = document.RootNode;
				document.RootNode.Data.Children.Add(node);
				continue;
			}
			if (!documentParser.IsEOF())
			{
				break;
			}
			Debug.Assert(documentParser._spanStack.Count == 0, "Span stack should be empty");
		}
		throw new TextParserException("Expected end of file", new TextParserSpan(documentParser.Cursor, documentParser.Cursor, documentParser));
	}

	public bool TryEatWhitespaceOrComment()
	{
		if (IsEOF())
		{
			return false;
		}
		bool result = false;
		do
		{
			char c = Data[Cursor];
			char c2 = ((Cursor < Data.Length - 1) ? Data[Cursor + 1] : ' ');
			bool flag = c == ' ' || c == '\n';
			bool flag2 = c == '/' && c2 == '*';
			bool flag3 = c == '/' && c2 == '/';
			if (!flag && !flag2 && !flag3)
			{
				break;
			}
			result = true;
			if (flag2)
			{
				EatMultilineComment();
			}
			else if (flag3)
			{
				EatLineEndComment();
			}
			else
			{
				Cursor++;
			}
		}
		while (!IsEOF());
		return result;
	}

	private void EatMultilineComment()
	{
		int startCursor = PushSpan();
		Eat("/*");
		while (true)
		{
			if (IsEOF())
			{
				throw new TextParserException("Encountered end of file while parsing comment", PopSpan(startCursor));
			}
			if (TryEat("*/"))
			{
				break;
			}
			Cursor++;
		}
		PopSpan(startCursor);
	}

	private void EatLineEndComment()
	{
		Eat("//");
		while (!IsEOF())
		{
			char c = Data[Cursor];
			Cursor++;
			if (c == '\n')
			{
				break;
			}
		}
	}

	public bool TryEatIdentifier(string kind, out string identifier)
	{
		identifier = null;
		if (IsEOF())
		{
			return false;
		}
		char c = Data[Cursor];
		if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
		{
			identifier = EatIdentifier(kind);
			return true;
		}
		return false;
	}

	public string EatIdentifier(string kind, bool allowDigitAsFirstChar = false)
	{
		int startCursor = PushSpan();
		if (IsEOF())
		{
			throw new TextParserException("Expected " + kind + ", found end of file", PopSpan(startCursor));
		}
		string text = "";
		while (!IsEOF())
		{
			char c = Data[Cursor];
			if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
			{
				text += c;
				Cursor++;
				continue;
			}
			if (c >= '0' && c <= '9')
			{
				if (text.Length == 0 && !allowDigitAsFirstChar)
				{
					throw new TextParserException("Expected " + kind + ", found digit", PopSpan(startCursor));
				}
				text += c;
				Cursor++;
				continue;
			}
			if (text.Length == 0)
			{
				throw new TextParserException($"Expected {kind}, found {c}", PopSpan(startCursor));
			}
			break;
		}
		PopSpan(startCursor);
		return text;
	}

	public bool TryEatNamedExpressionDeclaration(DocumentNode scopeNode, DocumentNode.NodeData nodeData)
	{
		int cursor = Cursor;
		if (!TryEat('@'))
		{
			return false;
		}
		int startCursor = PushSpan();
		string text = EatIdentifier("expression name");
		TextParserSpan span = PopSpan(startCursor);
		TryEatWhitespaceOrComment();
		if (!TryEat('='))
		{
			Cursor = cursor;
			return false;
		}
		TryEatWhitespaceOrComment();
		Expression value = EatExpression(scopeNode);
		if (nodeData.LocalNamedExpressions.ContainsKey(text))
		{
			throw new TextParserException("Named expression with same key " + text + " has already been defined", span);
		}
		nodeData.LocalNamedExpressions.Add(text, value);
		TryEatWhitespaceOrComment();
		Eat(';');
		TryEatWhitespaceOrComment();
		return true;
	}

	private Expression EatNamedExpressionReference()
	{
		Expression expression = null;
		if (Peek('$'))
		{
			int startCursor = PushSpan();
			Eat('$');
			string text = EatIdentifier("document name");
			expression = new Expression(new Expression.Identifier("$", text), PopSpan(startCursor));
			Eat('.');
		}
		int startCursor2 = PushSpan();
		Eat('@');
		Expression expression2 = new Expression(new Expression.Identifier("@", EatIdentifier("expression name")), PopSpan(startCursor2));
		if (expression != null)
		{
			return new Expression(Expression.ExpressionOperator.MemberAccess, expression, expression2, new TextParserSpan(expression.SourceSpan.Start, expression2.SourceSpan.End, this));
		}
		return expression2;
	}

	private Expression EatNamedExpressionMemberReference()
	{
		Expression expression = null;
		if (Peek('$'))
		{
			int startCursor = PushSpan();
			Eat('$');
			string text = EatIdentifier("document name");
			expression = new Expression(new Expression.Identifier("$", text), PopSpan(startCursor));
			Eat('.');
		}
		int startCursor2 = PushSpan();
		Eat('@');
		Expression expression2 = new Expression(new Expression.Identifier("@", EatIdentifier("expression name")), PopSpan(startCursor2));
		Expression expression3 = ((expression == null) ? expression2 : new Expression(Expression.ExpressionOperator.MemberAccess, expression, expression2, new TextParserSpan(expression.SourceSpan.Start, expression2.SourceSpan.End, this)));
		while (TryEat('.'))
		{
			int cursor = Cursor;
			int startCursor3 = PushSpan();
			Expression expression4 = new Expression(new Expression.Identifier(null, EatIdentifier("member")), PopSpan(startCursor3));
			expression3 = new Expression(Expression.ExpressionOperator.MemberAccess, expression3, expression4, new TextParserSpan(expression3.SourceSpan.Start, expression4.SourceSpan.End, this));
		}
		return expression3;
	}

	public bool TryEatNamedDocumentDeclaraction(Document doc)
	{
		int cursor = Cursor;
		if (!TryEat('$'))
		{
			return false;
		}
		string key = EatIdentifier("document name");
		TryEatWhitespaceOrComment();
		if (!TryEat('='))
		{
			Cursor = cursor;
			return false;
		}
		TryEatWhitespaceOrComment();
		int startCursor = PushSpan();
		string text = EatDoubleQuotedString();
		if (!Expression.TryResolvePath(text, SourcePath, out var resolvedPath))
		{
			throw new TextParserException("Could not resolve relative path: " + text, PopSpan(startCursor));
		}
		PopSpan(startCursor);
		doc.NamedDocumentReferences.Add(key, resolvedPath);
		Eat(';');
		return true;
	}

	public bool TryEatNodeDefinition(out DocumentNode node)
	{
		node = new DocumentNode();
		int startCursor = PushSpan();
		int startCursor2 = PushSpan();
		if (Peek('$') || Peek('@'))
		{
			Expression namedTemplateExpression = EatNamedExpressionReference();
			node.TemplateSetup = new DocumentNode.NodeTemplateSetup
			{
				NamedTemplateExpression = namedTemplateExpression
			};
		}
		else
		{
			int startCursor3 = PushSpan();
			if (!TryEatIdentifier("node type", out var identifier))
			{
				return false;
			}
			TextParserSpan span = PopSpan(startCursor3);
			if (!Document.ElementClassInfos.TryGetValue(identifier, out node.ElementClassInfo))
			{
				throw new TextParserException("Unknown node type: " + identifier, span);
			}
		}
		node.SourceSpan = PopSpan(startCursor2);
		TryEatWhitespaceOrComment();
		if (TryEat('#'))
		{
			node.Name = EatIdentifier("node name");
			node.SourceSpan = PopSpan(startCursor);
			TryEatWhitespaceOrComment();
		}
		else
		{
			PopSpan(startCursor);
		}
		EatNodeDefinitionBody(node);
		return true;
	}

	public void EatNodeDefinitionBody(DocumentNode node)
	{
		Eat('{');
		TryEatWhitespaceOrComment();
		while (TryEatNamedExpressionDeclaration(node, node.Data))
		{
			TryEatWhitespaceOrComment();
		}
		EatNodeProperties(node, node.Data);
		if (node.TemplateSetup != null)
		{
			while (TryEatTemplateInsert(node))
			{
				TryEatWhitespaceOrComment();
			}
		}
		DocumentNode node2;
		while (TryEatNodeDefinition(out node2))
		{
			node2.ParentNode = node;
			node.Data.Children.Add(node2);
			TryEatWhitespaceOrComment();
		}
		Eat('}');
	}

	private bool TryEatTemplateInsert(DocumentNode templateNode)
	{
		if (!TryEat('#'))
		{
			return false;
		}
		string key = EatIdentifier("template node name");
		TryEatWhitespaceOrComment();
		DocumentNode.NodeData nodeData = new DocumentNode.NodeData();
		Eat('{');
		TryEatWhitespaceOrComment();
		while (TryEatNamedExpressionDeclaration(templateNode, nodeData))
		{
			TryEatWhitespaceOrComment();
		}
		EatNodeProperties(templateNode, nodeData);
		DocumentNode node;
		while (TryEatNodeDefinition(out node))
		{
			nodeData.Children.Add(node);
			TryEatWhitespaceOrComment();
		}
		Eat('}');
		templateNode.TemplateSetup.Inserts.Add(key, nodeData);
		return true;
	}

	private void EatNodeProperties(DocumentNode scopeNode, DocumentNode.NodeData nodeData)
	{
		int cursor = Cursor;
		TextParserSpan span;
		while (true)
		{
			if (Peek('}') || Peek('$') || Peek('@') || Peek('#'))
			{
				return;
			}
			int startCursor = PushSpan();
			string key = EatIdentifier("property name or node type name");
			span = PopSpan(startCursor);
			TryEatWhitespaceOrComment();
			if (!TryEat(':'))
			{
				Cursor = cursor;
				return;
			}
			if (nodeData.PropertyExpressions.ContainsKey(key))
			{
				break;
			}
			TryEatWhitespaceOrComment();
			nodeData.PropertyExpressions.Add(key, EatExpression(scopeNode));
			Eat(';');
			TryEatWhitespaceOrComment();
			cursor = Cursor;
		}
		throw new TextParserException("A property cannot be set twice", span);
	}

	public Expression EatExpression(DocumentNode scopeNode, int precedence = 0)
	{
		int startCursor = PushSpan();
		if (IsEOF())
		{
			throw new TextParserException("Expected expression, found end of file", PopSpan(startCursor));
		}
		char c = Data[Cursor];
		Expression expression;
		Expression.ExpressionDictionary dictionary;
		if (c == '-' || (c >= '0' && c <= '9'))
		{
			decimal value = EatDecimal();
			expression = new Expression(value, PopSpan(startCursor));
		}
		else if (TryEatObjectDictionary(scopeNode, null, out dictionary))
		{
			expression = new Expression(dictionary, PopSpan(startCursor));
		}
		else
		{
			switch (c)
			{
			case '(':
				Eat('(');
				TryEatWhitespaceOrComment();
				expression = EatExpression(scopeNode);
				Eat(')');
				PopSpan(startCursor);
				break;
			case '#':
			{
				UInt32Color value5 = EatColor();
				expression = new Expression(value5, PopSpan(startCursor));
				break;
			}
			case '%':
			{
				Eat('%');
				string text2 = EatIdentifier("i18n key part");
				while (TryEat('.'))
				{
					text2 = text2 + "." + EatIdentifier("i18n key part");
				}
				expression = new Expression(new Expression.Identifier("%", text2), PopSpan(startCursor));
				break;
			}
			case '$':
				Eat('$');
				expression = new Expression(new Expression.Identifier("$", EatIdentifier("document name")), PopSpan(startCursor));
				break;
			case '@':
				Eat('@');
				expression = new Expression(new Expression.Identifier("@", EatIdentifier("expression name")), PopSpan(startCursor));
				break;
			default:
			{
				if (TryEatBoolean(out var value2))
				{
					expression = new Expression(value2, PopSpan(startCursor));
					break;
				}
				if (TryEatDoubleQuotedString(out var value3))
				{
					expression = new Expression(value3, PopSpan(startCursor));
					break;
				}
				string text = EatIdentifier("expression");
				TextParserSpan textParserSpan = PopSpan(startCursor);
				TryEatWhitespaceOrComment();
				if (Peek('{'))
				{
					if (!Document.ElementClassInfos.TryGetValue(text, out var value4))
					{
						throw new TextParserException("Unknown node type: " + text, textParserSpan);
					}
					DocumentNode documentNode = new DocumentNode
					{
						ElementClassInfo = value4,
						ParentNode = scopeNode
					};
					EatNodeDefinitionBody(documentNode);
					documentNode.SourceSpan = new TextParserSpan(textParserSpan.Start, Cursor, this);
					expression = new Expression(documentNode, textParserSpan);
				}
				else
				{
					expression = ((!TryEatObjectDictionary(scopeNode, text, out dictionary)) ? new Expression(new Expression.Identifier(null, text), textParserSpan) : new Expression(dictionary, textParserSpan));
				}
				break;
			}
			}
		}
		TryEatWhitespaceOrComment();
		if (TryEatCompositeExpression(scopeNode, expression, precedence, out var result))
		{
			return result;
		}
		return expression;
	}

	private bool TryEatCompositeExpression(DocumentNode scopeNode, Expression left, int precedence, out Expression result)
	{
		result = null;
		if (IsEOF())
		{
			return false;
		}
		char c = Data[Cursor];
		string text = c.ToString();
		string key = text + ((Cursor + 1 < Data.Length) ? Data[Cursor + 1].ToString() : "");
		if (!Expression.OperatorPrecedences.TryGetValue(key, out var value) && !Expression.OperatorPrecedences.TryGetValue(text, out value))
		{
			return false;
		}
		if (value <= precedence)
		{
			return false;
		}
		Eat(c);
		if (Expression.Operators.TryGetValue(key, out var value2))
		{
			Cursor++;
		}
		else
		{
			value2 = Expression.Operators[text];
		}
		TryEatWhitespaceOrComment();
		result = new Expression(value2, left, EatExpression(scopeNode, value), new TextParserSpan(left.SourceSpan.Start, Cursor, this));
		if (TryEatCompositeExpression(scopeNode, result, precedence, out var result2))
		{
			result = result2;
		}
		return true;
	}

	private bool TryEatObjectDictionary(DocumentNode scopeNode, string typeName, out Expression.ExpressionDictionary dictionary)
	{
		int cursor = Cursor;
		if (!TryEat('('))
		{
			dictionary = null;
			return false;
		}
		Expression.ExpressionDictionary newDictionary = (dictionary = new Expression.ExpressionDictionary
		{
			TypeName = typeName,
			ScopeNode = scopeNode
		});
		TryEatWhitespaceOrComment();
		if (TryEat(')'))
		{
			return true;
		}
		while (TryEatSpread())
		{
			TryEatWhitespaceOrComment();
			if (!TryEat(','))
			{
				break;
			}
			TryEatWhitespaceOrComment();
		}
		while (true)
		{
			bool flag = newDictionary.SpreadReferences.Count > 0 || newDictionary.Entries.Count > 0;
			int startCursor = PushSpan();
			if (!TryEatIdentifier("key", out var identifier))
			{
				PopSpan(startCursor);
				if (!flag)
				{
					Cursor = cursor;
					dictionary = null;
					return false;
				}
				if (TryEat(')'))
				{
					break;
				}
				EatIdentifier("key");
			}
			TextParserSpan span = PopSpan(startCursor);
			TryEatWhitespaceOrComment();
			if (!Peek(':') && !flag)
			{
				Cursor = cursor;
				dictionary = null;
				return false;
			}
			Eat(':');
			TryEatWhitespaceOrComment();
			int startCursor2 = PushSpan();
			Expression value;
			if (TryEatObjectDictionary(scopeNode, null, out var dictionary2))
			{
				value = new Expression(dictionary2, PopSpan(startCursor2));
			}
			else
			{
				value = EatExpression(scopeNode, 1);
				PopSpan(startCursor2);
			}
			TryEatWhitespaceOrComment();
			if (newDictionary.Entries.ContainsKey(identifier))
			{
				throw new TextParserException("Duplicate key " + identifier + " in dictionary", span);
			}
			newDictionary.Entries.Add(identifier, value);
			if (TryEat(')'))
			{
				break;
			}
			Eat(',');
			TryEatWhitespaceOrComment();
		}
		return true;
		bool TryEatSpread()
		{
			int num = PushSpan();
			if (!TryEat("..."))
			{
				PopSpan(num);
				Cursor = num;
				return false;
			}
			Expression item = EatNamedExpressionMemberReference();
			PopSpan(num);
			newDictionary.SpreadReferences.Add(item);
			return true;
		}
	}

	public bool TryEatBoolean(out bool value)
	{
		value = false;
		if (IsEOF())
		{
			return false;
		}
		int length = "true".Length;
		int length2 = "false".Length;
		if (Cursor + length < Data.Length && Data.Substring(Cursor, length) == "true")
		{
			Cursor += length;
			value = true;
			return true;
		}
		if (Cursor + length2 < Data.Length && Data.Substring(Cursor, length2) == "false")
		{
			Cursor += length2;
			value = false;
			return true;
		}
		return false;
	}

	public bool EatBoolean()
	{
		int startCursor = PushSpan();
		if (IsEOF())
		{
			throw new TextParserException("Expected boolean, found end of file", PopSpan(startCursor));
		}
		if (!TryEatBoolean(out var value))
		{
			throw new TextParserException($"Expected boolean, found {Data[Cursor]}", PopSpan(startCursor));
		}
		PopSpan(startCursor);
		return value;
	}

	public string EatDoubleQuotedString()
	{
		int startCursor = PushSpan();
		Eat('"');
		string text = "";
		while (!IsEOF())
		{
			char c = Data[Cursor];
			char c2 = ((Cursor + 1 < Data.Length) ? Data[Cursor + 1] : ' ');
			switch (c)
			{
			case '\\':
				if (c2 == '\\' || c2 == '"')
				{
					text += c2;
					Cursor += 2;
					break;
				}
				throw new TextParserException("\\ must be followed by \\ or \"", PopSpan(startCursor));
			default:
				text += c;
				Cursor++;
				break;
			case '"':
				Eat('"');
				PopSpan(startCursor);
				return text;
			}
		}
		throw new TextParserException("Encountered end of file while parsing double-quoted string", PopSpan(startCursor));
	}

	public bool TryEatDoubleQuotedString(out string value)
	{
		if (IsEOF() || Data[Cursor] != '"')
		{
			value = null;
			return false;
		}
		value = EatDoubleQuotedString();
		return true;
	}

	public UInt32Color EatColor()
	{
		int startCursor = PushSpan();
		Eat('#');
		if (!TryReadHexByte(out var value2) || !TryReadHexByte(out var value3) || !TryReadHexByte(out var value4))
		{
			throw new TextParserException("Expected color literal", PopSpan(startCursor));
		}
		if (!TryReadHexByte(out var value5))
		{
			value5 = byte.MaxValue;
			if (TryEat('('))
			{
				decimal num = EatDecimal();
				if (num < 0m || num > 1m)
				{
					throw new TextParserException("Alpha value must be in 0-1 range within color literal", PopSpan(startCursor));
				}
				value5 = (byte)(255m * num);
				Eat(')');
			}
		}
		PopSpan(startCursor);
		return UInt32Color.FromRGBA(value2, value3, value4, value5);
		bool TryReadHexByte(out byte value)
		{
			if (Cursor + 1 >= Data.Length)
			{
				value = 0;
				return false;
			}
			string s = Data.Substring(Cursor, 2);
			if (!byte.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value))
			{
				value = 0;
				return false;
			}
			Cursor += 2;
			return true;
		}
	}
}
