using System.Collections.Generic;

namespace Assembly.Helpers.CodeCompletion.XML
{
	public class XMLAnalysis
	{
		/// <summary>
		///     Private constructor - create XMLAnalysis objects using the AnalyzeLine() factory method instead.
		/// </summary>
		private XMLAnalysis()
		{
		}

		/// <summary>
		///     Whether or not the caret is inside of a tag or attribute value.
		/// </summary>
		public bool IsCaretInsideTag { get; private set; }

		/// <summary>
		///     Whether or not the caret is inside of an attribute value.
		/// </summary>
		public bool IsCaretInsideAttributeValue { get; private set; }

		/// <summary>
		///     Whether or not an attribute can be legally inserted at the caret's position.
		/// </summary>
		public bool CanInsertAttribute
		{
			get { return (IsCaretInsideTag && !IsCaretInsideAttributeValue && CurrentTag != null); }
		}

		/// <summary>
		///     Attributes which have been defined for the current tag.
		/// </summary>
		public HashSet<string> Attributes { get; private set; }

		/// <summary>
		///     The name of the tag that the caret is currently inside. Can be null if not inside a tag.
		/// </summary>
		public string CurrentTag { get; private set; }

		/// <summary>
		///     The name of the attribute that the caret is currently defining. Can be null if not inside an attribute.
		/// </summary>
		public string CurrentAttribute { get; private set; }

		/// <summary>
		///     Analyzes a line of XML markup and retrieves information about it and where the caret is at.
		/// </summary>
		/// <param name="line">The line of markup to analyze.</param>
		/// <param name="caretColumn">The zero-based index of the column the caret is in.</param>
		/// <returns>The analysis results.</returns>
		public static XMLAnalysis AnalyzeLine(string line, int caretColumn)
		{
			var results = new XMLAnalysis();

			bool inTag = false;
			bool insideTagName = false;
			string currentTag = null;
			string currentAttribute = null;
			int firstLetterPos = -1;
			int lastLetterPos = -1;
			int lastOpenTagPos = -1;
			char quoteType = '\0';
			var attributes = new HashSet<string>();
			for (int i = 0; i <= line.Length; i++)
			{
				if (i == caretColumn)
				{
					// Store caret-position-related results
					results.IsCaretInsideTag = inTag;
					results.IsCaretInsideAttributeValue = (quoteType != '\0');
					results.CurrentTag = currentTag;
					results.Attributes = attributes;
					results.CurrentAttribute = currentAttribute;
				}
				if (i == line.Length)
					break;

				char ch = line[i];
				if (inTag && (ch == '"' || ch == '\''))
				{
					if (quoteType == ch)
					{
						quoteType = '\0'; // Left quotes
						currentAttribute = null;
					}
					else if (quoteType == '\0')
					{
						quoteType = ch; // Entered quotes
					}
				}
				else if (quoteType == '\0')
				{
					if (ch == '<')
					{
						lastOpenTagPos = i;
						inTag = true;
						insideTagName = true;
						firstLetterPos = -1;
						attributes = new HashSet<string>();
						currentTag = null;
						currentAttribute = null;
					}
					else if (ch == '>')
					{
						inTag = false;
						insideTagName = false;
						firstLetterPos = -1;
						currentTag = null;
						currentAttribute = null;
					}
					else if (char.IsLetter(ch) || (firstLetterPos > 0 && char.IsDigit(ch)))
					{
						if (firstLetterPos < 0)
							firstLetterPos = i;
						lastLetterPos = i;
						currentAttribute = null;
					}
					else if (firstLetterPos > 0)
					{
						if (insideTagName)
						{
							currentTag = line.Substring(firstLetterPos, lastLetterPos - firstLetterPos + 1);
							insideTagName = false;
						}
						else if (ch == '=')
						{
							currentAttribute = line.Substring(firstLetterPos, lastLetterPos - firstLetterPos + 1);
							attributes.Add(currentAttribute);
						}
						else
						{
							continue;
						}
						firstLetterPos = -1;
					}
				}
			}

			return results;
		}
	}
}