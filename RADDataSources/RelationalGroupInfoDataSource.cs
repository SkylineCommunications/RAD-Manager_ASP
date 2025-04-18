namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;

	/// <summary>
	/// The input is a group name. We return a table with the parameter keys, element names and parameter names for the selected group.
	/// </summary>
	[GQIMetaData(Name = "Get Relational Anomaly Group Info")]
	public class RelationalGroupInfoDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments, IGQIOnPrepareFetch
	{
		private static readonly GQIStringArgument GroupName = new GQIStringArgument("GroupName");
		private static readonly GQIIntArgument DataMinerID = new GQIIntArgument("DataMinerID");
		private ElementNameCache _elementNames;
		private ParametersCache _parameters;
		private IGQILogger _logger;
		private string _groupName = string.Empty;
		private int _dataMinerID = -1;
		private List<ParameterKey> _parameterKeys = new List<ParameterKey>();

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			ConnectionHelper.InitializeConnection(args.DMS);
			_logger = args.Logger;
			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { GroupName, DataMinerID };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(GroupName, out _groupName))
				_logger.Error("No group name provided");

			if (!args.TryGetArgumentValue(DataMinerID, out _dataMinerID))
				_logger.Error("No DataMiner ID provided");

			return default;
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			_elementNames = new ElementNameCache(_logger);
			_parameters = new ParametersCache(_logger);

			if (string.IsNullOrEmpty(_groupName))
			{
				_logger.Error("Group name is empty");
				_parameterKeys = new List<ParameterKey>();
				return default;
			}

			try
			{
				var groupInfo = RadMessageHelper.FetchParameterGroupInfo(ConnectionHelper.Connection, _dataMinerID, _groupName);
				if (groupInfo != null)
					_parameterKeys = groupInfo.Parameters;
			}
			catch (Exception ex)
			{
				throw new DataMinerCommunicationException($"Failed to fetch group info for group {_groupName} on agent {_dataMinerID}", ex);
			}

			// Fetch and cache elementNames and protocol info
			foreach (ParameterKey paramKey in _parameterKeys)
			{
				_elementNames.TryGet(paramKey.DataMinerID, paramKey.ElementID, out var _);
				_parameters.TryGet(paramKey.DataMinerID, paramKey.ElementID, out var _);
			}

			return default;
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[]
			{
				new GQIStringColumn("Parameter Key"),
				new GQIIntColumn("DataMiner ID"),
				new GQIIntColumn("Element ID"),
				new GQIIntColumn("Parameter ID"),
				new GQIStringColumn("Primary Key"),
				new GQIStringColumn("Display Key"),
				new GQIStringColumn("Element Name"),
				new GQIStringColumn("Parameter Name"),
			};
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var rows = new List<GQIRow>();

			foreach (ParameterKey pKey in _parameterKeys)
			{
				var paramID = new ParamID(pKey.DataMinerID, pKey.ElementID, pKey.ParameterID, pKey.Instance);
				var key = paramID.ToString();
				var cells = new GQICell[]
				{
					new GQICell(){ Value = pKey.ToString() },
					new GQICell(){ Value = pKey.DataMinerID },
					new GQICell(){ Value = pKey.ElementID },
					new GQICell(){ Value = pKey.ParameterID },
					new GQICell(){ Value = pKey.Instance },
					new GQICell(){ Value = pKey.DisplayInstance },
					new GQICell(){ Value = GetElementName(pKey) },
					new GQICell(){ Value = GetParameterName(pKey) },
				};
				var parameterMetaData = new ObjectRefMetadata() { Object = paramID };
				var rowMetaData = new GenIfRowMetadata(new[] { parameterMetaData });
				rows.Add(new GQIRow(key, cells) { Metadata = rowMetaData });
			}

			return new GQIPage(rows.ToArray());
		}

		#region Helper Methods

		private string GetElementName(ParameterKey paramKey)
		{
			if (_elementNames.TryGetFromCache(paramKey.DataMinerID, paramKey.ElementID, out string name))
				return name;
			else
				return "Unknown Element";
		}

		private string GetParameterName(ParameterKey paramKey)
		{
			if (!_parameters.TryGetFromCache(paramKey.DataMinerID, paramKey.ElementID, out var parameterInfos))
				return "Unknown Parameter";

			var paramInfo = parameterInfos?.FirstOrDefault(p => p.ID == paramKey.ParameterID);
			return paramInfo?.DisplayName ?? "Unknown Parameter";
		}

		#endregion
	}
}