using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blamite.Util
{
	/// <summary>
	/// Message severity levels.
	/// </summary>
	public enum MessageSeverity
	{
		Info,
		Warning,
		Error
	}

	/// <summary>
	/// A logged message.
	/// </summary>
	public class LoggedMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LoggedMessage"/> class.
		/// </summary>
		/// <param name="severity">The message's severity.</param>
		/// <param name="message">The message's text.</param>
		/// <param name="lineNumber">The one-based line number that the message originated from, or 0 if none.</param>
		/// <param name="columnNumber">The one-based column number that the message originated from, or 0 if none.</param>
		public LoggedMessage(MessageSeverity severity, string message, int lineNumber = 0, int columnNumber = 0)
		{
			Severity = severity;
			Message = message;
			LineNumber = lineNumber;
			ColumnNumber = columnNumber;
		}

		/// <summary>
		/// Gets the message's severity.
		/// </summary>
		public MessageSeverity Severity { get; private set; }

		/// <summary>
		/// Gets the message's text.
		/// </summary>
		public string Message { get; private set; }

		/// <summary>
		/// Gets the one-based line number that the message originated from.
		/// Can be 0 if no line number information is available.
		/// </summary>
		public int LineNumber { get; private set; }

		/// <summary>
		/// Gets the one-based column number that the message originated from.
		/// Can be 0 if no column number information is available.
		/// </summary>
		public int ColumnNumber { get; private set; }

		/// <summary>
		/// Gets whether or not the <see cref="LineNumber"/> property is valid.
		/// </summary>
		public bool HasLineNumber
		{
			get { return LineNumber > 0; }
		}

		/// <summary>
		/// Gets whether or not the <see cref="ColumnNumber"/> property is valid.
		/// </summary>
		public bool HasColumnNumber
		{
			get { return ColumnNumber > 0; }
		}
	}

	/// <summary>
	/// Holds a collection of logged messages.
	/// </summary>
	public class MessageLog
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MessageLog"/> class.
		/// The log will be created empty.
		/// </summary>
		public MessageLog()
		{
			Messages = new List<LoggedMessage>();
		}

		/// <summary>
		/// Gets a list of all logged messages.
		/// </summary>
		public List<LoggedMessage> Messages { get; private set; }

		/// <summary>
		/// Gets whether or not the message log contains any messages in it.
		/// </summary>
		public bool HasMessages
		{
			get { return Messages.Count > 0; }
		}

		/// <summary>
		/// Gets whether or not the message log contains any info messages.
		/// </summary>
		public bool HasInfo { get; private set; }

		/// <summary>
		/// Gets whether or not the message log contains any warning messages.
		/// </summary>
		public bool HasWarnings { get; private set; }

		/// <summary>
		/// Gets whether or not the message log contains any error messages.
		/// </summary>
		public bool HasErrors { get; private set; }

		/// <summary>
		/// Removes all messages from the log.
		/// </summary>
		public void Clear()
		{
			Messages.Clear();
			HasInfo = false;
			HasWarnings = false;
			HasErrors = false;
		}

		/// <summary>
		/// Logs an informational message.
		/// </summary>
		/// <param name="message">The message to log.</param>
		/// <param name="line">The one-based line number that the message originated from, or 0 if none.</param>
		/// <param name="column">The one-based column number that the message originated from, or 0 if none.</param>
		public void Info(string message, int line = 0, int column = 0)
		{
			HasInfo = true;
			Messages.Add(new LoggedMessage(MessageSeverity.Info, message, line, column));
		}

		/// <summary>
		/// Logs a warning message.
		/// </summary>
		/// <param name="message">The message to log.</param>
		/// <param name="line">The one-based line number that the message originated from, or 0 if none.</param>
		/// <param name="column">The one-based column number that the message originated from, or 0 if none.</param>
		public void Warning(string message, int line = 0, int column = 0)
		{
			HasWarnings = true;
			Messages.Add(new LoggedMessage(MessageSeverity.Warning, message, line, column));
		}

		/// <summary>
		/// Logs an error message.
		/// </summary>
		/// <param name="message">The message to log.</param>
		/// <param name="line">The one-based line number that the message originated from, or 0 if none.</param>
		/// <param name="column">The one-based column number that the message originated from, or 0 if none.</param>
		public void Error(string message, int line = 0, int column = 0)
		{
			HasErrors = true;
			Messages.Add(new LoggedMessage(MessageSeverity.Error, message, line, column));
		}

		/// <summary>
		/// Logs a message.
		/// </summary>
		/// <param name="severity">The severity of the message to log.</param>
		/// <param name="message">The message to log.</param>
		/// <param name="line">The one-based line number that the message originated from, or 0 if none.</param>
		/// <param name="column">The one-based column number that the message originated from, or 0 if none.</param>
		public void Log(MessageSeverity severity, string message, int line = 0, int column = 0)
		{
			switch (severity)
			{
				case MessageSeverity.Info:
					Info(message, line, column);
					break;
				case MessageSeverity.Warning:
					Warning(message, line, column);
					break;
				case MessageSeverity.Error:
					Error(message, line, column);
					break;
				default:
					throw new ArgumentException("Unrecognized message severity: " + severity);
			}
		}
	}
}
