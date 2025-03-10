using AddParameterGroup;
using Skyline.DataMiner.Analytics.DataTypes;
using Skyline.DataMiner.Analytics.Mad;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;
using System;
using System.Collections.Generic;
using System.Linq;

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

            var dialog = new AddParameterGroupDialog(engine);
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

    private void AddGroup(string groupName, List<ParameterKey> parameters, bool updateModel, double? anomalyThreshold, int? minimalDuration)
    {
        var groupInfo = new MADGroupInfo(groupName, parameters, updateModel, anomalyThreshold, minimalDuration);
        var message = new AddMADParameterGroupMessage(groupInfo);
        app.Engine.SendSLNetSingleResponseMessage(message);
    }

    private List<ParameterKey> GetParameterKeys(int dataMinerID, int elementID, int parameterID, string displayKeyFilter)
    {
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
            KeyFilterType = GetDynamicTableIndicesKeyFilterType.DisplayKey
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
        var dialog = sender as AddParameterGroupDialog;
        if (dialog == null)
            throw new ArgumentException("Invalid sender type");

		try
		{
			if (dialog.AddType == AddGroupType.Single)
			{
				var pKeys = dialog.Parameters.SelectMany(p => GetParameterKeys(p.DataMinerID, p.ElementID, p.ParameterID, p.DisplayKeyFilter)).ToList();
				AddGroup(dialog.GroupName, pKeys, dialog.UpdateModel, dialog.AnomalyThreshold, dialog.MinimalDuration);
			}
			else
			{
				var elements = app.Engine.FindElementsByProtocol(dialog.ProtocolName, dialog.ProtocolVersion);
				foreach (var element in elements)
				{
					var pKeys = dialog.ProtolParameters.SelectMany(p => GetParameterKeys(element.DmaId, element.ElementId, p.ParameterID, p.DisplayKeyFilter)).ToList();
					AddGroup($"{dialog.GroupName} ({element.ElementName})", pKeys, dialog.UpdateModel, dialog.AnomalyThreshold, dialog.MinimalDuration);
				}
			}
		}
		catch (Exception ex)
		{
			var exceptionDialog = new ExceptionDialog(app.Engine, ex);
			app.ShowDialog(exceptionDialog);
			exceptionDialog.Forward += (s, args) => app.Engine.ExitFail("Failed to add parameter group(s) to RAD configuration");
			return;
		}

		app.Engine.ExitSuccess("Successfully added parameter group(s) to RAD configuration");
	}

    //TODO: remove reference to internal SLAnalyticsTypes
}