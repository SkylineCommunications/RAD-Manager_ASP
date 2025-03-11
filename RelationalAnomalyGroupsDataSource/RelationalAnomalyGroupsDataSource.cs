using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Skyline.DataMiner.Analytics.DataTypes;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Analytics.Mad;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages;

namespace RelationalAnomalyGroupsDataSource
{
	/// <summary>
	/// We return a table with the group names, their parameters, updateModel value and AnomalyThreshold for all configured groups.
	/// </summary>
	public class RelationalAnomalyGroupsDataSource : IGQIDataSource, IGQIOnInit
	{
		private GQIDMS dms_;
		private Dictionary<(int dmaId, int elementId), (string elementName, ParameterInfo[] parameters)> cache_ = new Dictionary<(int dmaId, int elementId), (string elementName, ParameterInfo[] parameters)>();

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[] { new GQIStringColumn("Name"), new GQIIntColumn("DataMiner Id"), new GQIStringColumn("Parameters"),
				new GQIStringColumn("UpdateModel"), new GQIStringColumn("AnomalyThreshold")};
		}

		private string InnerParameterKeyToString(ParameterKey pKey, string elementName, ParameterInfo[] parameters)
		{
			if (string.IsNullOrEmpty(elementName))
				elementName = $"{pKey.DataMinerID}/{pKey.ElementID}";

			var parameter = parameters?.FirstOrDefault(p => p.ID == pKey.ParameterID);
			var parameterName = parameter?.DisplayName ?? pKey.ParameterID.ToString();

			string instance = "";
			if (!string.IsNullOrEmpty(pKey.DisplayInstance))
				instance = pKey.DisplayInstance;
			else
				instance = pKey.Instance;

			if (string.IsNullOrEmpty(instance))
				return $"{elementName}/{parameterName}";
			else
				return $"{elementName}/{parameterName}/{instance}";
		}

		private string ParameterKeyToString(ParameterKey pKey)
		{
			if (cache_.TryGetValue((pKey.DataMinerID, pKey.ElementID), out var cacheInfo))
				return InnerParameterKeyToString(pKey, cacheInfo.elementName, cacheInfo.parameters);

			try
			{
				var elementRequest = new GetElementByIDMessage(pKey.DataMinerID, pKey.ElementID);
				var elementResponse = dms_.SendMessage(elementRequest) as ElementInfoEventMessage;
				if (elementResponse == null)
					return InnerParameterKeyToString(pKey, null, null);

				var protocolRequest = new GetElementProtocolMessage(pKey.DataMinerID, pKey.ElementID);
				var protocolResponse = dms_.SendMessage(protocolRequest) as GetElementProtocolResponseMessage;
				if (protocolResponse == null)
					return InnerParameterKeyToString(pKey, null, null);

				cache_.Add((pKey.DataMinerID, pKey.ElementID), (elementResponse.Name, protocolResponse.AllParameters));
				return InnerParameterKeyToString(pKey, elementResponse.Name, protocolResponse.AllParameters);
			}
			catch (Exception)
			{
				return InnerParameterKeyToString(pKey, null, null);
			}
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var rows = new List<GQIRow>();

			GetMADParameterGroupsMessage request = new Skyline.DataMiner.Analytics.Mad.GetMADParameterGroupsMessage();
			var response = dms_.SendMessage(request) as GetMADParameterGroupsResponseMessage;
			foreach (var groupName in response.GroupNames)
			{
				GetMADParameterGroupInfoMessage msg = new GetMADParameterGroupInfoMessage(groupName);
				var groupInfoResponse = dms_.SendMessage(msg) as GetMADParameterGroupInfoResponseMessage;
				MADGroupInfo groupInfo = groupInfoResponse.GroupInfo;
				var parameterStr = groupInfo.Parameters.Select(p => ParameterKeyToString(p));
				int dataMinerID = groupInfo.Parameters.FirstOrDefault()?.DataMinerID ?? 0;

				rows.Add(new GQIRow(
					new GQICell[]
					{
						new GQICell(){Value=groupName,},
						new GQICell(){Value=dataMinerID,},
						new GQICell(){Value=$"[{string.Join(", ", parameterStr)}]",},
						new GQICell(){Value=groupInfo.UpdateModel.ToString(),},
						new GQICell(){Value=groupInfo.AnomalyThreshold?.ToString(CultureInfo.InvariantCulture) ?? "3",},
					}
					));
			}
			return new GQIPage(rows.ToArray());
		}

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			dms_ = args.DMS;
			return default;
		}
	}
}