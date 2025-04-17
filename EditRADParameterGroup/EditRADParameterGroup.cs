using System;
using System.Linq;
using EditRADParameterGroup;
using RadWidgets;
using Skyline.DataMiner.Analytics.Mad;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;

public class Script
{
	private InteractiveController _app;

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
			_app = new InteractiveController(engine);

			var groupNamesAndIds = Utils.GetGroupNameAndDataMinerID(_app);
			if (groupNamesAndIds.Count == 0)
			{
				Utils.ShowMessageDialog(_app, "No parameter group selected", "Please select the parameter group you want to edit first");
				return;
			}
			else if (groupNamesAndIds.Count > 1)
			{
				Utils.ShowMessageDialog(_app, "Multiple parameter groups selected", "Please select a single parameter group you want to edit");
				return;
			}

			int dataMinerID = groupNamesAndIds[0].Item1;
			string groupName = groupNamesAndIds[0].Item2;
			RadGroupSettings settings = null;
			try
			{
				var request = new GetMADParameterGroupInfoMessage(groupName)
				{
					DataMinerID = dataMinerID,
				};
				var response = _app.Engine.SendSLNetSingleResponseMessage(request) as GetMADParameterGroupInfoResponseMessage;
				if (response?.GroupInfo == null)
				{
					Utils.ShowMessageDialog(
						_app,
						"Failed to fetch parameter group information",
						"Failed to fetch parameter group information: no response or a response of the wrong type received");
					return;
				}

				settings = new RadGroupSettings()
				{
					GroupName = response.GroupInfo.Name,
					Parameters = response.GroupInfo.Parameters,
					Options = new RadGroupOptions()
					{
						UpdateModel = response.GroupInfo.UpdateModel,
						AnomalyThreshold = response.GroupInfo.AnomalyThreshold,
						MinimalDuration = response.GroupInfo.MinimumAnomalyDuration,
					},
				};
			}
			catch (Exception ex)
			{
				Utils.ShowExceptionDialog(_app, "Failed to fetch parameter group information", ex);
				return;
			}

			var dialog = new EditParameterGroupDialog(engine, settings, dataMinerID);
			dialog.Accepted += Dialog_Accepted;
			dialog.Cancelled += Dialog_Cancelled;

			_app.ShowDialog(dialog);
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
		_app.Engine.ExitSuccess("Adding parameter group cancelled");
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
			_app.Engine.SendSLNetSingleResponseMessage(removeMessage);

			var settings = dialog.GroupSettings;
			var pKeys = settings.Parameters.ToList();
			var groupInfo = new MADGroupInfo(settings.GroupName, pKeys, settings.Options.UpdateModel, settings.Options.AnomalyThreshold, settings.Options.MinimalDuration);
			var message = new AddMADParameterGroupMessage(groupInfo);
			_app.Engine.SendSLNetSingleResponseMessage(message);
		}
		catch (Exception ex)
		{
			Utils.ShowExceptionDialog(_app, "Failed to add parameter group(s) to RAD configuration", ex, dialog);
			return;
		}

		_app.Engine.ExitSuccess("Successfully added parameter group(s) to RAD configuration");
	}
}