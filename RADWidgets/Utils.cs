using System;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;

namespace RADWidgets
{
	public class Utils
	{
		public static string NormalizeScriptParameterValue(string parameterValue)
		{
			if (parameterValue == null)
				return null;
			else if (parameterValue == "[]")
				return "";
			else if (parameterValue.StartsWith("[\"", StringComparison.Ordinal) && parameterValue.EndsWith("\"]", StringComparison.Ordinal))
				return parameterValue.Substring(2, parameterValue.Length - 4);
			else
				return parameterValue;
		}

		public static void ShowMessageDialog(InteractiveController app, string title, string message)
		{
			var dialog = new MessageDialog(app.Engine, message);
			dialog.Title = title;
			dialog.OkButton.Pressed += (s, args) => app.Engine.ExitSuccess(message);

			app.ShowDialog(dialog);
		}

		public static void ShowExceptionDialog(InteractiveController app, string title, Exception ex)
		{
			var exceptionDialog = new ExceptionDialog(app.Engine, ex);
			exceptionDialog.Title = title;
			exceptionDialog.OkButton.Pressed += (s, args) => app.Engine.ExitSuccess(title);

			app.ShowDialog(exceptionDialog);
		}
	}
}
