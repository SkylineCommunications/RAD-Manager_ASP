using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Skyline.DataMiner.Analytics.DataTypes;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Analytics.Mad;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Net.Messages.SLDataGateway;

namespace RelationalGroupInfoDataSource
{
	/// <summary>
	/// The input is a group name. We return a table with the parameter keys, element names and parameter names for the selected group.
	/// </summary>
	public class RelationalGroupInfoDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments, IGQIOnPrepareFetch
	{
		private static readonly GQIStringArgument GroupName = new GQIStringArgument("GroupName");
		private GQIDMS dms_;
		private IGQILogger logger_;
		private string groupName_;
		private string lastError_;
		private List<ParameterKey> parameterKeys_ = new List<ParameterKey>();
		Dictionary<(int dmaId, int elementId), string> elementNames_ = new Dictionary<(int dmaId, int elementId), string>();
		Dictionary<(int dmaId, int elementId), ParameterInfo[]> protocolCache_ = new Dictionary<(int dmaId, int elementId), ParameterInfo[]>();
		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			dms_ = args.DMS;
			logger_ = args.Logger;
			return default;
		}
		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { GroupName };
		}
		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (args.TryGetArgumentValue(GroupName, out string groupName))
			{
				groupName_ = groupName;
			}
			return new OnArgumentsProcessedOutputArgs();
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			try
			{
				if (string.IsNullOrEmpty(groupName_))
				{
					lastError_ = "Group name is empty";
					return new OnPrepareFetchOutputArgs();
				}

				lastError_ = null;

				GetMADParameterGroupInfoMessage msg = new GetMADParameterGroupInfoMessage(groupName_);
				var groupInfoResponse = dms_.SendMessage(msg) as GetMADParameterGroupInfoResponseMessage;

				if (groupInfoResponse?.GroupInfo != null) //Fetch elementNames and protocol info
				{
					parameterKeys_ = groupInfoResponse.GroupInfo.Parameters;
					foreach (ParameterKey paramKey in parameterKeys_)
					{
						var elementKey = (paramKey.DataMinerID, paramKey.ElementID);

						// Get element name if not already cached
						if (!elementNames_.ContainsKey(elementKey))
						{
							var elementRequest = new GetElementByIDMessage(paramKey.DataMinerID, paramKey.ElementID);
							var elementResponse = dms_.SendMessage(elementRequest) as ElementInfoEventMessage;
							if (elementResponse != null)
							{
								elementNames_[elementKey] = elementResponse.Name;
							}
						}

						// Get protocol information if not already cached for this element
						if (!protocolCache_.ContainsKey(elementKey))
						{
							var protocolRequest = new GetElementProtocolMessage(paramKey.DataMinerID, paramKey.ElementID);
							var protocolResponse = dms_.SendMessage(protocolRequest) as GetElementProtocolResponseMessage;
							if (protocolResponse?.AllParameters != null)
							{
								protocolCache_[elementKey] = protocolResponse.AllParameters;
							}
						}
					}
				}
				return new OnPrepareFetchOutputArgs();
			}
			catch (Exception ex)
			{
				lastError_ = ex.Message;
				return new OnPrepareFetchOutputArgs();
			}
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[] { new GQIStringColumn("Parameter Key"), new GQIIntColumn("DataMiner ID"), new GQIIntColumn("Element ID"),
				new GQIIntColumn("Parameter ID"), new GQIStringColumn("Primary Key"), new GQIStringColumn("Display Key"), new GQIStringColumn("Element Name"),
				new GQIStringColumn("Parameter Name")
				};
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var rows = new List<GQIRow>();
			if (lastError_ != null)
			{
				//var paramID = new ParamID(1, 1, 1, "");
				//var key = paramID.ToString();
				//var cells = new GQICell[0] { };
				//var parameterMetaData = new ObjectRefMetadata { Object = paramID };
				//var rowMetaData = new GenIfRowMetadata(new[] { parameterMetaData });
				//rows.Add(new GQIRow(key, }) { Metadata = rowMetaData });
				return new GQIPage(rows.ToArray());
			}

			foreach (ParameterKey pKey in parameterKeys_)
			{
				var paramID = new ParamID(pKey.DataMinerID, pKey.ElementID, pKey.ParameterID, pKey.Instance);
				var key = paramID.ToString();
				var cells = new GQICell[]
				{
					new GQICell(){Value=pKey.ToString(),},
					new GQICell(){Value=pKey.DataMinerID,},
					new GQICell(){Value=pKey.ElementID,},
					new GQICell(){Value=pKey.ParameterID,},
					new GQICell(){Value=pKey.Instance,},
					new GQICell(){Value=pKey.DisplayInstance,},
					new GQICell(){Value=GetElementName(pKey),},
					new GQICell(){Value=GetParameterName(pKey),}
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
			var elementKey = (paramKey.DataMinerID, paramKey.ElementID);
			return elementNames_.TryGetValue(elementKey, out var name) ? name : "Unknown Element";
		}

		private string GetParameterName(ParameterKey paramKey)
		{
			var elementKey = (paramKey.DataMinerID, paramKey.ElementID);
			if (!protocolCache_.TryGetValue(elementKey, out var parameterInfos))
			{
				return "Unknown Parameter";
			}

			var paramInfo = parameterInfos.FirstOrDefault(p => p.ID == paramKey.ParameterID);
			return paramInfo?.Name ?? "Unknown Parameter";
		}

		#endregion

	}
}