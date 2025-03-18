using System;
using System.Collections.Generic;
using System.Linq;
using EditRADParameterGroup;
using RADWidgets;
using Skyline.DataMiner.Analytics.DataTypes;
using Skyline.DataMiner.Analytics.Mad;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages;
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

			if (!Utils.GetGroupNameAndDataMinerID(app, "Please select the parameter group you want to edit first", out string groupName, out int dataMinerID))
				return;

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

				var parameters = new List<ParameterSelectorInfo>();
				foreach (var parameter in response.GroupInfo.Parameters)
				{
					var element = engine.FindElement(parameter.DataMinerID, parameter.ElementID);
					var protocol = Utils.FetchElementProtocol(engine, parameter.DataMinerID, parameter.ElementID);
					var paramInfo = protocol?.Parameters.FirstOrDefault(p => p.ID == parameter.ParameterID);
					parameters.Add(new ParameterSelectorInfo()
					{
						ElementName = element?.ElementName ?? "Unknown element",
						ParameterName = paramInfo?.DisplayName ?? "Unknown parameter",
						DataMinerID = parameter.DataMinerID,
						ElementID = parameter.ElementID,
						ParameterID = parameter.ParameterID,
						DisplayKeyFilter = parameter.DisplayInstance,
					});
				}

				settings = new RADGroupSettings()
				{
					GroupName = response.GroupInfo.Name,
					Parameters = parameters,
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

	private List<ParameterKey> GetParameterKeys(int dataMinerID, int elementID, int parameterID, string displayKeyFilter)
	{
		//TODO: put in utils, I guess
		if (string.IsNullOrEmpty(displayKeyFilter))
			return new List<ParameterKey>() { new ParameterKey(dataMinerID, elementID, parameterID) };

		var protocolRequest = new GetElementProtocolMessage(dataMinerID, elementID);
		var protocolResponse = app.Engine.SendSLNetSingleResponseMessage(protocolRequest) as GetElementProtocolResponseMessage;
		if (protocolResponse == null)
		{
			app.Engine.Log($"Could not fetch protocol for element {dataMinerID}/{elementID}", LogType.Error, 5);
			return new List<ParameterKey>();
		}

		var parameter = protocolResponse.Parameters.FirstOrDefault(p => p.ID == parameterID);
		if (parameter == null)
		{
			app.Engine.Log($"Could not find parameter {parameterID} in element protocol for element {dataMinerID}/{elementID}", LogType.Error, 5);
			return new List<ParameterKey>();
		}

		if (!parameter.IsTableColumn || parameter.ParentTable == null)
			return new List<ParameterKey>() { new ParameterKey(dataMinerID, elementID, parameterID, displayKeyFilter) };

		var indicesRequest = new GetDynamicTableIndices(dataMinerID, elementID, parameter.ParentTable.ID)
		{
			KeyFilter = displayKeyFilter,
			KeyFilterType = GetDynamicTableIndicesKeyFilterType.DisplayKey,
		};
		var indicesResponse = app.Engine.SendSLNetSingleResponseMessage(indicesRequest) as DynamicTableIndicesResponse;
		if (indicesResponse == null)
		{
			app.Engine.Log($"Could not fetch primary keys for element {dataMinerID}/{elementID} parameter {parameterID} with filter {displayKeyFilter}", LogType.Error, 5);
			return new List<ParameterKey>();
		}

		return indicesResponse.Indices.Select(i => new ParameterKey(dataMinerID, elementID, parameterID, i.IndexValue, i.DisplayValue)).ToList();
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
			var pKeys = settings.Parameters.SelectMany(p => GetParameterKeys(p.DataMinerID, p.ElementID, p.ParameterID, p.DisplayKeyFilter)).ToList();
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