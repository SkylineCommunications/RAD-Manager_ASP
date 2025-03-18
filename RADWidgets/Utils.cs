using System;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;

namespace RADWidgets
{
	public class Utils
	{
		/// <summary>
		/// Get the group name and DataMiner ID from the script parameters.
		/// </summary>
		/// <param name="app">The app</param>
		/// <param name="noGroupSelectedMessage">The message to show in the message dialog when no parameter group was selected.</param>
		/// <param name="groupName">Will be set to the group name (if any).</param>
		/// <param name="dataMinerID">Will be set to the DataMiner ID (if any).</param>
		/// <returns>True if the arguments could be successfully parsed, false otherwise.</returns>
		/// <exception cref="FormatException">Thrown when the DataMiner ID script parameter could not be parsed.</exception>
		public static bool GetGroupNameAndDataMinerID(InteractiveController app, string noGroupSelectedMessage, out string groupName, out int dataMinerID)
		{
			groupName = NormalizeScriptParameterValue(app.Engine.GetScriptParam("GroupName")?.Value);
			string dataMinerIDStr = NormalizeScriptParameterValue(app.Engine.GetScriptParam("DataMinerID")?.Value);
			if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(dataMinerIDStr))
			{
				ShowMessageDialog(app, "No parameter group selected", noGroupSelectedMessage);
				dataMinerID = -1;
				return false;
			}

			if (!int.TryParse(dataMinerIDStr, out dataMinerID))
				throw new FormatException($"DataMinerID parameter is not a valid number, got '{dataMinerIDStr}'");

			return true;
		}

		/// <summary>
		/// Normalize the script parameter value by removing [" and "] if necessary.
		/// </summary>
		/// <param name="parameterValue">The parameter value.</param>
		/// <returns>The normalized value.</returns>
		public static string NormalizeScriptParameterValue(string parameterValue)
		{
			if (parameterValue == null)
				return null;
			else if (parameterValue == "[]")
				return string.Empty;
			else if (parameterValue.StartsWith("[\"", StringComparison.Ordinal) && parameterValue.EndsWith("\"]", StringComparison.Ordinal))
				return parameterValue.Substring(2, parameterValue.Length - 4);
			else
				return parameterValue;
		}

		/// <summary>
		/// Shows a message dialog with the given title and message. The script will exit when the OK button is pressed.
		/// </summary>
		/// <param name="app">The app.</param>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="message">The message to display on the dialog.</param>
		public static void ShowMessageDialog(InteractiveController app, string title, string message)
		{
			var dialog = new MessageDialog(app.Engine, message);
			dialog.Title = title;
			dialog.OkButton.Pressed += (s, args) => app.Engine.ExitSuccess(message);

			app.ShowDialog(dialog);
		}

		/// <summary>
		/// Shows an excpetion dialog with the given title and message. The script will exit when the OK button is pressed.
		/// </summary>
		/// <param name="app">The app.</param>
		/// <param name="title">The title of the dialog.</param>
		/// <param name="ex">The exception to display in the dialog.</param>
		public static void ShowExceptionDialog(InteractiveController app, string title, Exception ex)
		{
			var exceptionDialog = new ExceptionDialog(app.Engine, ex);
			exceptionDialog.Title = title;
			exceptionDialog.OkButton.Pressed += (s, args) => app.Engine.ExitSuccess(title);

			app.ShowDialog(exceptionDialog);
		}

		/// <summary>
		/// Fetch the protocol for the given element. Returns null and logs an exception if the protocol could not be fetched.
		/// </summary>
		/// <param name="engine">The engine.</param>
		/// <param name="dataMinerID">The DataMiner ID of the element.</param>
		/// <param name="elementID">The element ID.</param>
		/// <returns>The element protocol, or null if it could not be fetched.</returns>
		public static GetElementProtocolResponseMessage FetchElementProtocol(IEngine engine, int dataMinerID, int elementID)
		{
			try
			{
				var request = new GetElementProtocolMessage(dataMinerID, elementID);
				return engine.SendSLNetSingleResponseMessage(request) as GetElementProtocolResponseMessage;
			}
			catch (Exception e)
			{
				engine.Log($"Could not fetch protocol for element {dataMinerID}/{elementID}: {e}");
				return null;
			}
		}
	}
}
