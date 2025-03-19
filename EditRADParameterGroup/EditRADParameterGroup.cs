using System;
using System.Collections.Generic;
using System.Linq;
using EditRADParameterGroup;
using RADWidgets;
using Skyline.DataMiner.Analytics.DataTypes;
using Skyline.DataMiner.Analytics.Mad;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Net.Messages.SLDataGateway;
using Skyline.DataMiner.Net.ReportsAndDashboards;
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

			var groupNamesAndIds = Utils.GetGroupNameAndDataMinerID(app);
			if (groupNamesAndIds.Count == 0)
			{
				Utils.ShowMessageDialog(app, "No parameter group selected", "Please select the parameter group you want to edit first");
				return;
			}
			else if (groupNamesAndIds.Count > 1)
			{
				Utils.ShowMessageDialog(app, "Multiple parameter groups selected", "Please select a single parameter group you want to edit");
				return;
			}

			int dataMinerID = groupNamesAndIds[0].Item1;
			string groupName = groupNamesAndIds[0].Item2;
			RADGroupSettings settings = null;
			try
			{
				var request = new GetMADParameterGroupInfoMessage(groupName)
				{
					DataMinerID = dataMinerID,
				};
				var response = app.Engine.SendSLNetSingleResponseMessage(request) as GetMADParameterGroupInfoResponseMessage;
				if (response?.GroupInfo == null)
					throw new Exception("No response or a response of the wrong type received");

				settings = new RADGroupSettings()
				{
					GroupName = response.GroupInfo.Name,
					Parameters = response.GroupInfo.Parameters,
					Options = new RADGroupOptions()
					{
						UpdateModel = response.GroupInfo.UpdateModel,
						AnomalyThreshold = response.GroupInfo.AnomalyThreshold,
						MinimalDuration = response.GroupInfo.MinimumAnomalyDuration,
					},
				};
			}
			catch (Exception ex)
			{
				Utils.ShowExceptionDialog(app, "Failed to fetch parameter group information", ex);
				return;
			}

			var dialog = new EditParameterGroupDialog(engine, settings, dataMinerID);
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
		app.Engine.ExitSuccess("Adding parameter group cancelled");
	}

	private void Dialog_Accepted(object sender, EventArgs e)
	{
		var dialog = sender as EditParameterGroupDialog;
		if (dialog == null)
			throw new ArgumentException("Invalid sender type");

		try
		{
			var removeMessage = new RemoveMADParameterGroupMessage(dialog.OriginalGroupName)
			{
				DataMinerID = dialog.DataMinerID,
			};
			app.Engine.SendSLNetSingleResponseMessage(removeMessage);

			var settings = dialog.GroupSettings;
			var pKeys = settings.Parameters.ToList();
			var groupInfo = new MADGroupInfo(settings.GroupName, pKeys, settings.Options.UpdateModel, settings.Options.AnomalyThreshold, settings.Options.MinimalDuration);
			var message = new AddMADParameterGroupMessage(groupInfo);
			app.Engine.SendSLNetSingleResponseMessage(message);
		}
		catch (Exception ex)
		{
			Utils.ShowExceptionDialog(app, "Failed to add parameter group(s) to RAD configuration", ex);
			return;
		}

		app.Engine.ExitSuccess("Successfully added parameter group(s) to RAD configuration");
	}
}