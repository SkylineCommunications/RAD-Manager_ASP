namespace RelationalTrendDataSource
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RelationalAnomalyGroupsDataSource;
	using Skyline.DataMiner.Analytics.DataTypes;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// The input is a concatenated string of parameter keys and the groupName.
	/// The output is a table with the values of the selected parameters, so they can be displayed on the trend graph.
	/// </summary>
	public class RelationalTrendDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments, IGQIOnPrepareFetch
	{
		private static readonly GQIStringArgument GroupName = new GQIStringArgument("groupName");
		private static readonly GQIStringArgument ParameterKeysString = new GQIStringArgument("parameterKeysString");
		private static GroupTrendData groupTrendData_ = new GroupTrendData();
		private static Connection connection_;
		private string groupName_;
		private List<ParameterKey> parameterKeys_ = new List<ParameterKey>();
		private string lastError_ = string.Empty;
		private List<int> parameterIndices_ = new List<int>();
		private int numberOfColums_ = 1;
		private GQIDMS dms_;
		private IGQILogger logger_;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			dms_ = args.DMS;
			InitializeConnection(dms_);
			logger_= args.Logger;
			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { GroupName, ParameterKeysString };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			// Parse which parameterKeys are selected
			try
			{
				if (args.TryGetArgumentValue(ParameterKeysString, out string concatenatedKeys))
				{
					var keys = concatenatedKeys.Split(',');
					if (keys.IsNullOrEmpty())
					{
						lastError_ = "No parameter keys selected";
						return new OnArgumentsProcessedOutputArgs();
					}

					foreach (string key in keys)
					{
						var parts = key.Split('/');
						if (parts.Length == 5)
						{
							var parameterKey = new ParameterKey(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), parts[3], parts[4]);
							parameterKeys_.Add(parameterKey);
						}
						else
						{
							lastError_ = "Invalid parameter key format";
							return new OnArgumentsProcessedOutputArgs();
						}
					}
				}
				else
				{
					lastError_ = "Can not parse parameterkeys";
					return new OnArgumentsProcessedOutputArgs();
				}
			}
			catch (Exception ex)
			{
				lastError_ = ex.Message;
				return new OnArgumentsProcessedOutputArgs();
			}

			// Parse the group name
			try
			{
				if (args.TryGetArgumentValue(GroupName, out string groupNameStr))
				{
					groupName_ = groupNameStr;
				}
				else
				{
					lastError_ = "No group name selected";
					return new OnArgumentsProcessedOutputArgs();
				}
			}
			catch (Exception ex)
			{
				lastError_ = ex.Message;
			}

			return new OnArgumentsProcessedOutputArgs();
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			try
			{
				if (lastError_.IsNotNullOrEmpty())
				{
					return new OnPrepareFetchOutputArgs();
				}

				// Check if data needs to be refetched
				if (groupName_ != groupTrendData_.GroupName || !groupTrendData_.Data.Any() || (DateTime.UtcNow - groupTrendData_.Data.Last().Timestamp.ToUniversalTime()).TotalMinutes > 5)
				{
					logger_.Error("Fetching data");
					GetMADDataMessage msg = new GetMADDataMessage(groupName_, DateTime.Now.AddMonths(-1), DateTime.Now);
					var madDataReponse = connection_.HandleSingleResponseMessage(msg) as GetMADDataResponseMessage;
					groupTrendData_ = new GroupTrendData(madDataReponse, groupName_);
				}
				else
				{
					logger_.Debug("No need to refetch data");
				}

				if (groupTrendData_?.Parameters != null)
				{
					parameterIndices_.Clear();
					for (int i = 0; i < groupTrendData_.Parameters.Count; ++i)
					{
						var parameter = groupTrendData_.Parameters[i];
						var matchingKey = parameterKeys_.FirstOrDefault(pk =>
								pk.DataMinerID == parameter.DataMinerID &&
								pk.ElementID == parameter.ElementID &&
								pk.ParameterID == parameter.ParameterID_ &&
								pk.Instance == parameter.Instance);

						if (matchingKey != null)
						{
							parameterIndices_.Add(i);
						}
					}

					if (parameterIndices_.IsNullOrEmpty())
					{
						lastError_ = "No matching parameters found";
						return new OnPrepareFetchOutputArgs();
					}
				}
				else
				{
					throw new Exception("No parameters found in the groupTrendData");
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
			var columns = new List<GQIColumn>
			{
				new GQIDateTimeColumn("Time"),
			};

			columns.AddRange(parameterKeys_.Select(pKey => new GQIDoubleColumn(pKey.ToString())));
			numberOfColums_ = columns.Count;
			return columns.ToArray();
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			if (lastError_.IsNotNullOrEmpty())
			{
				return Error();
			}

			var rows = new List<GQIRow>();
			try
			{
				foreach (var dataPoint in groupTrendData_.Data)
				{
					var cells = new GQICell[numberOfColums_];
					cells[0] = new GQICell { Value = dataPoint.Timestamp.ToUniversalTime() };
					for (int i = 0; i < parameterIndices_.Count; ++i)
					{
						cells[i + 1] = new GQICell { Value = dataPoint.Values[parameterIndices_[i]] };
					}

					rows.Add(new GQIRow(cells));
				}
			}
			catch (Exception)
			{
				Error();
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

		#region Helper Methods

		private GQIPage Error()
		{
			var error = lastError_;
			lastError_ = null;
			return new GQIPage(new GQIRow[0] {});
		}

		#endregion
	}

	public class GroupTrendData
	{
		public GroupTrendData()
		{
			Data = new List<MADDataPoint>();
		}

		public GroupTrendData(GetMADDataResponseMessage response, string groupName)
		{
			GroupName = groupName;
			Data = response.Data;
			Parameters = response.Parameters;
		}

		public string GroupName { get; set; }

		public List<MADDataPoint> Data { get; set; }

		public List<ParameterID> Parameters { get; set; }
	}
}