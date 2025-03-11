using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Skyline.DataMiner.Analytics.DataTypes;
using Skyline.DataMiner.Analytics.GenericInterface;
using Skyline.DataMiner.Analytics.Mad;
using Skyline.DataMiner.Automation;

namespace RelationalAnomalyGroupsDataSource
{
	/// <summary>
	/// We return a table with the group names, their parameters, updateModel value and AnomalyThreshold for all configured groups.
	/// </summary>
	public class RelationalAnomalyGroupsDataSource : IGQIDataSource, IGQIOnInit
	{
		private GQIDMS dms_;
		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[] { new GQIStringColumn("Name"), new GQIIntColumn("DataMiner Id"), new GQIStringColumn("Parameters"),
				new GQIStringColumn("UpdateModel"), new GQIStringColumn("AnomalyThreshold")};
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
				string parameterString = "";
				int dataMinerID = 0;
				foreach (ParameterKey parameter in groupInfo.Parameters)
				{
					parameterString += parameter.ToString() + "; ";
					dataMinerID = parameter.DataMinerID;
				}
				rows.Add(new GQIRow(
					new GQICell[]
					{
						new GQICell(){Value=groupName,},
						new GQICell(){Value=dataMinerID,},
						new GQICell(){Value=parameterString,},
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