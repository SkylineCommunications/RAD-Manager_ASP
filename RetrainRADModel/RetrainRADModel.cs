using System;
using RADWidgets;
using RetrainRADModel;
using Skyline.DataMiner.Analytics.Mad;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.SLConfiguration;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;

public class Script
{
	private InteractiveController app;

	/// <summary>
	/// The Script entry point.
	/// </summary>
	/// <param name="engine">Link with SLAutomation process.</param>
	public void Run(IEngine engine)
	{
		// DO NOT REMOVE THE COMMENTED OUT CODE BELOW OR THE SCRIPT WONT RUN!
		// Interactive scripts need to be launched differently.
		// This is determined by a simple string search looking for "engine.ShowUI" in the source code.
		// However, due to the NuGet package, this string can no longer be detected.
		// This comment is here as a temporary workaround until it has been fixed.
		//// engine.ShowUI();

		try
		{
			app = new InteractiveController(engine);

			string groupName = Utils.NormalizeScriptParameterValue(app.Engine.GetScriptParam("GroupName")?.Value);
			string dataMinerIDStr = Utils.NormalizeScriptParameterValue(app.Engine.GetScriptParam("DataMinerID")?.Value);
			if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(dataMinerIDStr))
			{
				Utils.ShowMessageDialog(app, "No parameter group selected", "Please select the parameter group you want to retrain first");
				return;
			}

			if (!int.TryParse(dataMinerIDStr, out int dataMinerID))
				throw new ArgumentException($"DataMinerID parameter is not a valid number, got '{dataMinerIDStr}'");

			var dialog = new RetrainRADModelDialog(engine, groupName, dataMinerID);
			dialog.Accepted += Dialog_Accepted;
			dialog.Cancelled += Dialog_Cancelled;

			app.ShowDialog(dialog);
		}
		catch (ScriptAbortException)
		{
			throw;
		}
		catch (ScriptForceAbortException)
		{
			throw;
		}
		catch (ScriptTimeoutException)
		{
			throw;
		}
		catch (InteractiveUserDetachedException)
		{
			throw;
		}
		catch (Exception e)
		{
			engine.ExitFail(e.ToString());
		}
	}

	private void Dialog_Cancelled(object sender, EventArgs e)
	{
		app.Engine.ExitSuccess("Removing parameter group cancelled");
	}

	private void Dialog_Accepted(object sender, EventArgs e)
	{
		var dialog = sender as RetrainRADModelDialog;
		if (dialog == null)
			throw new ArgumentException("Invalid sender type");

		try
		{
			var message = new RetrainMADModelMessage(dialog.GroupName, dialog.TimeRanges)
			{
				DataMinerID = dialog.DataMinerID,
			};
			app.Engine.SendSLNetSingleResponseMessage(message);
		}
		catch (Exception ex)
		{
			Utils.ShowExceptionDialog(app, "Failed to retrain parameter group", ex);
			return;
		}

		app.Engine.ExitSuccess("Successfully retrained RAD model");
	}
}
