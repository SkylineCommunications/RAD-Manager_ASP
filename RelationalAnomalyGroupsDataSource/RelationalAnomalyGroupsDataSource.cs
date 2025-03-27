namespace RelationalAnomalyGroupsDataSource
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// We return a table with the group names, their parameters, updateModel value and AnomalyThreshold for all configured groups.
	/// </summary>
	public class RelationalAnomalyGroupsDataSource : IGQIDataSource, IGQIOnInit, IGQIOnPrepareFetch
	{
		private static Connection connection_;
		private GQIDMS dms_;
		private IGQILogger logger_;
		private IEnumerator<int> dmaIDEnumerator_;
		private Dictionary<(int dmaId, int elementId), (string elementName, ParameterInfo[] parameters)> cache_ = new Dictionary<(int dmaId, int elementId), (string elementName, ParameterInfo[] parameters)>();

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			dms_ = args.DMS;
			logger_ = args.Logger;
			InitializeConnection(dms_);
			return default;
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				new GQIStringColumn("Name"),
				new GQIIntColumn("DataMiner Id"),
				new GQIStringColumn("Parameters"),
				new GQIStringColumn("UpdateModel"),
				new GQIStringColumn("AnomalyThreshold"),
				new GQIStringColumn("Minimal Anomaly Duration"),
			};
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			var infoMessages = dms_.SendMessages(new GetInfoMessage(InfoType.DataMinerInfo));
			dmaIDEnumerator_ = infoMessages.Select(m => m as GetDataMinerInfoResponseMessage).Where(m => m != null).Select(m => m.ID).ToList().GetEnumerator();

			return default;
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			if (!dmaIDEnumerator_.MoveNext())
				return new GQIPage(new GQIRow[0]);

			int dataMinerID = dmaIDEnumerator_.Current;
			GetMADParameterGroupsMessage request = new GetMADParameterGroupsMessage()
			{
				DataMinerID = dataMinerID,
			};
			var response = connection_.HandleSingleResponseMessage(request) as GetMADParameterGroupsResponseMessage;
			if (response == null)
			{
				logger_.Error($"Could not fetch RAD group names from agent {dataMinerID}: no response or response of the wrong type received");
				return new GQIPage(new GQIRow[0]);
			}

			var rows = new List<GQIRow>(response.GroupNames.Count);
			foreach (var groupName in response.GroupNames)
			{
				GetMADParameterGroupInfoMessage msg = new GetMADParameterGroupInfoMessage(groupName)
				{
					DataMinerID = dataMinerID,
				};
				var groupInfoResponse = connection_.HandleSingleResponseMessage(msg) as GetMADParameterGroupInfoResponseMessage;
				if (groupInfoResponse?.GroupInfo == null)
				{
					logger_.Error($"Could not fetch RAD group info for group {groupName}: no response or response of the wrong type received");
					continue;
				}

				MADGroupInfo groupInfo = groupInfoResponse.GroupInfo;
				var parameterStr = groupInfo.Parameters.Select(p => ParameterKeyToString(p));
				rows.Add(new GQIRow(
					new GQICell[]
					{
						new GQICell() { Value = groupName },
						new GQICell() { Value = dataMinerID },
						new GQICell() { Value = $"[{string.Join(", ", parameterStr)}]" },
						new GQICell() { Value = groupInfo.UpdateModel.ToString() },
						new GQICell() { Value = groupInfo.AnomalyThreshold?.ToString(CultureInfo.InvariantCulture) ?? "3" },
						new GQICell() { Value = (groupInfo.MinimumAnomalyDuration?.ToString(CultureInfo.InvariantCulture) ?? "5") + " min" },
					}));
			}

			return new GQIPage(rows.ToArray());
		}

		private static void InitializeConnection(GQIDMS dms)
		{
			if (connection_ == null)
			{
				connection_ = ConnectionHelper.CreateConnection(dms);
			}
		}

		private static string InnerParameterKeyToString(ParameterKey pKey, string elementName, ParameterInfo[] parameters)
		{
			if (string.IsNullOrEmpty(elementName))
				elementName = $"{pKey.DataMinerID}/{pKey.ElementID}";

			var parameter = parameters?.FirstOrDefault(p => p.ID == pKey.ParameterID);
			var parameterName = parameter?.DisplayName ?? pKey.ParameterID.ToString();

			string instance = string.Empty;
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
				var elementResponse = connection_.HandleSingleResponseMessage(elementRequest) as ElementInfoEventMessage;
				if (elementResponse == null)
					return InnerParameterKeyToString(pKey, null, null);

				var protocolRequest = new GetElementProtocolMessage(pKey.DataMinerID, pKey.ElementID);
				var protocolResponse = connection_.HandleSingleResponseMessage(protocolRequest) as GetElementProtocolResponseMessage;
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
	}
}