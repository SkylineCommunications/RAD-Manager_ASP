using System;
using System.Linq;
using RadUtils;
using RadWidgets;
using RetrainRADModel;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;
using Skyline.DataMiner.Utils.RadToolkit;

public class Script
{
	private InteractiveController _app;
	private RadHelper _radHelper;

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
			_radHelper = RadWidgets.Utils.GetRadHelper(engine);

			var groupIDs = RadWidgets.Utils.ParseGroupIDParameter(_app);
			if (groupIDs.Count == 0)
			{
				RadWidgets.Utils.ShowMessageDialog(_app, "No relational anomaly group selected", "Please select the relational anomaly group you want to retrain first");
				return;
			}

			var parentGroups = groupIDs.Select(id => new RadGroupID(id.DataMinerID, id.GroupName)).Distinct();
			if (parentGroups.Count() > 1)
			{
				RadWidgets.Utils.ShowMessageDialog(_app, "Multiple relational anomaly groups selected", "Please select a single relational anomaly group you want to retrain");
				return;
			}

			var groupID = parentGroups.First();
			RadGroupInfo groupInfo;
			try
			{
				groupInfo = _radHelper.FetchParameterGroupInfo(groupID.DataMinerID, groupID.GroupName);
			}
			catch (Exception e)
			{
				engine.Log($"Failed to fetch group info for relational anomaly group {groupID.DataMinerID}/{groupID.GroupName}: {e}");
				RadWidgets.Utils.ShowExceptionDialog(_app, "Failed to fetch relational anomaly group info", e);
				return;
			}

			var dialog = new RetrainRadModelDialog(engine, groupID, groupInfo);
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
		_app.Engine.ExitSuccess("Removing relational anomaly group cancelled");
	}

	private void Dialog_Accepted(object sender, EventArgs e)
	{
		var dialog = sender as RetrainRadModelDialog;
		if (dialog == null)
			throw new ArgumentException("Invalid sender type");

		try
		{
			var excludedSubgroups = dialog.GetExcludedSubgroupIDs();
			if (excludedSubgroups.Count > 0)
				_radHelper.RetrainParameterGroup(dialog.GroupID.DataMinerID, dialog.GroupID.GroupName, dialog.GetSelectedTimeRanges(), excludedSubgroups);
			else
				_radHelper.RetrainParameterGroup(dialog.GroupID.DataMinerID, dialog.GroupID.GroupName, dialog.GetSelectedTimeRanges());
		}
		catch (Exception ex)
		{
			RadWidgets.Utils.ShowExceptionDialog(_app, "Failed to retrain relational anomaly group", ex, dialog);
			return;
		}

		_app.Engine.ExitSuccess("Successfully retrained RAD model");
	}
}
