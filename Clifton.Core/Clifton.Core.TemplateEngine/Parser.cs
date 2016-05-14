using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Clifton.Core.ExtensionMethods;

namespace Clifton.Core.TemplateEngine
{
	public class Parser
	{
		protected bool inCode = false;
		protected bool inLiteralBlock = false;

		public string Parse(string text)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("StringBuilder sb = new StringBuilder();");
			List<string> lines = GetLines(text);

			// Here we assume that the START_CODE_BLOCK and END_CODE_BLOCK are always at the beginning of a line.
			// Embedded code with { } (or other tokens) are always indented!

			lines.Where(l=>!String.IsNullOrEmpty(l)).ForEachWithIndex((line, idx) =>
			{
				switch (inCode)
				{
					case false:
						if (inLiteralBlock)
						{
							AppendCodeOrLiteralLine(sb, line, ref inCode);
						}
						else
						{
							AppendNonCodeLine(sb, line, ref inCode);
						}
						break;

					case true:
						AppendCodeOrLiteralLine(sb, line, ref inCode);
						break;
				}
			});

			return sb.ToString();
		}

		/// <summary>
		/// Returns the text split into lines with any trailing whitespace trimmed.
		/// </summary>
		private List<string> GetLines(string text)
		{
			return text.Split(new char[] { '\r', '\n' }).Select(s => s.TrimEnd()).ToList();
		}

		private void AppendNonCodeLine(StringBuilder sb, string line, ref bool inCode)
		{
			if (line.BeginsWith(Constants.START_CODE_BLOCK))
			{
				inCode = true;
			}
			else
			{
				// Append a non-code line.
				string parsedLine = VariableReplacement(line);
				parsedLine = parsedLine.Replace("\"", "\\\"");
				sb.AppendLine("sb.Append" + (parsedLine + Constants.CRLF).Quote().Parens() + ";");
			}
		}

		private void AppendCodeOrLiteralLine(StringBuilder sb, string line, ref bool inCode)
		{
			if (line.BeginsWith(Constants.END_CODE_BLOCK))
			{
				inCode = false;
			}
			else if (line.Trim().BeginsWith(Constants.LITERAL) || inLiteralBlock)
			{
				if (line.BeginsWith(Constants.END_LITERAL_BLOCK))
				{
					inLiteralBlock = false;
				}
				else
				{
					// Preserve leading whitespace.
					string literal = line.LeftOf(Constants.LITERAL) + line.RightOf(Constants.LITERAL);
					string parsedLiteral = InLiteralVariableReplacement(literal);
					parsedLiteral = parsedLiteral.Replace("\"", "\\\"");
					parsedLiteral = parsedLiteral.Replace("@>", "\"");
					sb.AppendLine("sb.Append" + (parsedLiteral + Constants.CRLF).Quote().Parens() + ";");
				}
			}
			else if (line.BeginsWith(Constants.START_LITERAL_BLOCK))
			{
				inLiteralBlock = true;
			}
			else
			{
				// Append a code line.
				sb.AppendLine(line);
			}
		}

		private string InLiteralVariableReplacement(string line)
		{
			string remainder = line;
			string parsedLine = String.Empty;

			// Special handling for cases when you don't want to terminate an embedded variable with a whitespace
			// Example usage: @:alert("<@model.Str@>")

			while (remainder.Contains("<@"))
			{
				string left = remainder.LeftOf("<@");
				string right = remainder.RightOf("@>");

				// Force close of string builder, inject variable name, append with new string builder.
				parsedLine += left + "@> + " + remainder.Between("<@", "@>") + ".ToString() +@>";
				remainder = right;
			}

			parsedLine += remainder;

			return parsedLine;
		}
  
		private string VariableReplacement(string line)
		{
			string remainder = line;
			string parsedLine = String.Empty;

			// Regular handling:
			// Example usage: @:alert("foobar")

			while (remainder.Contains("@"))
			{
				string left = remainder.LeftOf('@');
				string right = remainder.RightOf('@');

				// TODO: @@ translates to an inline @, so ignore.
				if ((right.Length > 0) && (right[0] == '@'))
				{
					parsedLine += left + "@";
					remainder = right.Substring(1);		// move past second @
				}
				else
				{
					// Force close quote, inject variable name, append with + "
					parsedLine += left + "\" + " + right.LeftOf(' ') + ".ToString() + \"";
					remainder = " " + right.RightOf(' ');		// everything after the token.
				}
			}

			parsedLine += remainder;

			return parsedLine;
		}
	}
}
