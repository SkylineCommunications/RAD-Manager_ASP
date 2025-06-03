using System;
using System.Collections.Generic;
using System.Linq;
using EditRADParameterGroup;
using RadWidgets;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;
using Skyline.DataMiner.Utils.RadToolkit;

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

			var groupIDs = Utils.ParseGroupIDParameter(_app);
			if (groupIDs.Count == 0)
			{
				Utils.ShowMessageDialog(_app, "No parameter group selected", "Please select the parameter group you want to edit first.");
				return;
			}
			else if (groupIDs.Count > 1)
			{
				Utils.ShowMessageDialog(_app, "Multiple parameter groups selected", "Please select a single parameter group you want to edit.");
				return;
			}

			var groupID = groupIDs.First();
			IRadGroupBaseInfo settings = null;
			try
			{
				settings = _app.Engine.GetRadHelper().FetchParameterGroupInfo(groupID.DataMinerID, groupID.GroupName);
			}
			catch (Exception ex)
			{
				Utils.ShowExceptionDialog(_app, "Failed to fetch parameter group information", ex);
				return;
			}

			if (settings is RadGroupInfo groupSettings)
			{
				var dialog = new EditParameterGroupDialog(engine, groupSettings, groupID.DataMinerID);
				dialog.Accepted += (sender, args) => Dialog_Accepted(sender as EditParameterGroupDialog, groupSettings);
				dialog.Cancelled += (sender, args) => Dialog_Cancelled();
				_app.ShowDialog(dialog);
			}
			else if (settings is RadSharedModelGroupInfo sharedModelGroupSettings)
			{
				Guid? subgroupGUID = null;
				if (groupID is RadSubgroupID subgroupID)
				{
					if (subgroupID.SubgroupID != null)
					{
						subgroupGUID = subgroupID.SubgroupID;
					}
					else
					{
						var subgroup = sharedModelGroupSettings.Subgroups.FirstOrDefault(s => string.Equals(s.Name, subgroupID.GroupName, StringComparison.OrdinalIgnoreCase));
						subgroupGUID = subgroup?.ID;
					}
				}

				var dialog = new EditSharedModelGroupDialog(engine, sharedModelGroupSettings, subgroupGUID, groupID.DataMinerID);
				dialog.Accepted += (sender, args) => Dialog_Accepted(sender as EditSharedModelGroupDialog, sharedModelGroupSettings);
				dialog.Cancelled += (sender, args) => Dialog_Cancelled();
				_app.ShowDialog(dialog);
			}
			else
			{
				Utils.ShowMessageDialog(
					_app,
					"Failed to fetch parameter group information",
					"Failed to fetch parameter group information: no response or a response of the wrong type received");
				return;
			}
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
		_app.Engine.ExitSuccess("Editing parameter group cancelled");
	}

	private void Dialog_Accepted(EditParameterGroupDialog dialog, RadGroupSettings originalSettings)
	{
		if (dialog == null)
			throw new ArgumentException("Invalid sender type");

		try
		{
			var newSettings = dialog.GroupSettings;
			var radHelper = _app.Engine.GetRadHelper();
			if (!originalSettings.GroupName.Equals(newSettings.GroupName, StringComparison.OrdinalIgnoreCase))
			{
				if (originalSettings.HasSameParameters(newSettings))
				{
					try
					{
						radHelper.RenameParameterGroup(dialog.DataMinerID, originalSettings.GroupName, newSettings.GroupName);
					}
					catch (NotSupportedException)
					{
						// We can't rename, so remove the old group instead
						radHelper.RemoveParameterGroup(dialog.DataMinerID, originalSettings.GroupName);
					}
				}
				else
				{
					radHelper.RemoveParameterGroup(dialog.DataMinerID, originalSettings.GroupName);
				}
			}

			radHelper.AddParameterGroup(newSettings);
		}
		catch (Exception ex)
		{
			Utils.ShowExceptionDialog(_app, "Failed to add parameter group(s) to RAD configuration", ex, dialog);
			return;
		}

		_app.Engine.ExitSuccess("Successfully added parameter group(s) to RAD configuration");
	}

	private void GetAddedAndRemovedSubgroups(List<RadSubgroupSettings> newSubgroups, List<RadSubgroupInfo> oldSubgroups,
		out List<RadSubgroupSettings> addedSubgroups, out List<RadSubgroupSettings> removedSubgroups)
	{
		foreach (var subgroup in oldSubgroups)
			subgroup.NormalizeParameters();

		bool[] matchedOldSubgroups = new bool[oldSubgroups.Count];
		addedSubgroups = new List<RadSubgroupSettings>();
		removedSubgroups = new List<RadSubgroupSettings>();
		foreach (var subgroup in newSubgroups)
		{
			bool matched = false;

			subgroup.NormalizeParameters();
			for (int i = 0; i < oldSubgroups.Count; ++i)
			{
				if (matchedOldSubgroups[i])
					continue;
				if (oldSubgroups[i].HasSameOrderedParameters(subgroup))
				{
					matchedOldSubgroups[i] = true;
					matched = true;
					break;
				}
			}

			if (!matched)
				addedSubgroups.Add(subgroup);
		}

		for (int i = 0; i < matchedOldSubgroups.Length; ++i)
		{
			if (!matchedOldSubgroups[i])
				removedSubgroups.Add(oldSubgroups[i]);
		}
	}

	private void Dialog_Accepted(EditSharedModelGroupDialog dialog, RadSharedModelGroupInfo originalSettings)
	{
		if (dialog == null)
			throw new ArgumentException("Invalid sender type");

		try
		{
			var newSettings = dialog.GroupSettings;
			var radHelper = _app.Engine.GetRadHelper();

			GetAddedAndRemovedSubgroups(newSettings.Subgroups, originalSettings.Subgroups,
				out List<RadSubgroupSettings> addedSubgroups, out List<RadSubgroupSettings> removedSubgroups);
			if (addedSubgroups.Count == newSettings.Subgroups.Count)
			{
				// No subgroup is preserved, so we remove the entire group
				radHelper.RemoveParameterGroup(dialog.DataMinerID, originalSettings.GroupName);
			}
			else
			{
				// Add least one of the original subgroups is preserved, so do not remove the entire group
				if (!originalSettings.GroupName.Equals(newSettings.GroupName, StringComparison.OrdinalIgnoreCase))
					radHelper.RenameParameterGroup(dialog.DataMinerID, originalSettings.GroupName, newSettings.GroupName);

				foreach (var removedSubgroup in removedSubgroups)
					radHelper.RemoveSubgroup(dialog.DataMinerID, newSettings.GroupName, removedSubgroup.ID);
				foreach (var addedSubgroup in addedSubgroups)
					radHelper.AddSubgroup(dialog.DataMinerID, newSettings.GroupName, addedSubgroup);
			}

			radHelper.AddParameterGroup(newSettings);
		}
		catch (Exception ex)
		{
			Utils.ShowExceptionDialog(_app, "Failed to add parameter group(s) to RAD configuration", ex, dialog);
			return;
		}

		_app.Engine.ExitSuccess("Successfully added parameter group(s) to RAD configuration");
	}
}