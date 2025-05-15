using System;
using System.Linq;
using EditRADParameterGroup;
using RadUtils;
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

			var groupIDs = RadWidgets.Utils.ParseGroupIDParameters(_app);
			if (groupIDs.Count == 0)
			{
				RadWidgets.Utils.ShowMessageDialog(_app, "No parameter group selected", "Please select the parameter group you want to edit first");
				return;
			}
			else if (groupIDs.Count > 1)
			{
				RadWidgets.Utils.ShowMessageDialog(_app, "Multiple parameter groups selected", "Please select a single parameter group you want to edit");
				return;
			}

			var groupID = groupIDs.First();
			RadGroupSettings settings = null;
			try
			{
				settings = RadMessageHelper.FetchParameterGroupInfo(_app.Engine, groupID.DataMinerID, groupID.GroupName) as RadGroupSettings; //TODO
				if (settings == null)
				{
					RadWidgets.Utils.ShowMessageDialog(
						_app,
						"Failed to fetch parameter group information",
						"Failed to fetch parameter group information: no response or a response of the wrong type received");
					return;
				}
			}
			catch (Exception ex)
			{
				RadWidgets.Utils.ShowExceptionDialog(_app, "Failed to fetch parameter group information", ex);
				return;
			}

			var dialog = new EditParameterGroupDialog(engine, settings, groupID.DataMinerID);
			dialog.Accepted += (sender, args) => Dialog_Accepted(sender as EditParameterGroupDialog, settings);
			dialog.Cancelled += (sender, args) => Dialog_Cancelled();

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

	private void Dialog_Cancelled()
	{
		_app.Engine.ExitSuccess("Adding parameter group cancelled");
	}

	private void Dialog_Accepted(EditParameterGroupDialog dialog, RadGroupSettings originalSettings)
	{
		if (dialog == null)
			throw new ArgumentException("Invalid sender type");

		try
		{
			var newSettings = dialog.GroupSettings;
			if (!originalSettings.GroupName.Equals(newSettings.GroupName, StringComparison.OrdinalIgnoreCase))
			{
				if (originalSettings.HasSameParameters(newSettings))
				{
					try
					{
						RadMessageHelper.RenameParameterGroup(_app.Engine, dialog.DataMinerID, originalSettings.GroupName, newSettings.GroupName);
					}
					catch (TypeLoadException)
					{
						// We can't rename, so remove the old group instead
						RadMessageHelper.RemoveParameterGroup(_app.Engine, dialog.DataMinerID, originalSettings.GroupName);
					}
				}
				else
				{
					RadMessageHelper.RemoveParameterGroup(_app.Engine, dialog.DataMinerID, originalSettings.GroupName);
				}
			}

			var pKeys = newSettings.Parameters.ToList();
			var groupInfo = new MADGroupInfo(newSettings.GroupName, pKeys, newSettings.Options.UpdateModel, newSettings.Options.AnomalyThreshold, newSettings.Options.MinimalDuration);
			RadMessageHelper.AddParameterGroup(_app.Engine, groupInfo);
		}
		catch (Exception ex)
		{
			RadWidgets.Utils.ShowExceptionDialog(_app, "Failed to add parameter group(s) to RAD configuration", ex, dialog);
			return;
		}

		_app.Engine.ExitSuccess("Successfully added parameter group(s) to RAD configuration");
	}
}