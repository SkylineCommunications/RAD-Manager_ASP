namespace RelationalAnomalyScoreDataSource
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using RelationalAnomalyGroupsDataSource;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Helper;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class RelationalAnomalyScoreDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments, IGQIOnPrepareFetch
	{
		private static readonly GQIStringArgument GroupName = new GQIStringArgument("groupName");
		private static readonly GQIDateTimeArgument StartTime = new GQIDateTimeArgument("startTime");
		private static readonly GQIDateTimeArgument EndTime = new GQIDateTimeArgument("endTime");
		private static AnomalyScoreData anomalyScoreData_ = new AnomalyScoreData();
		private static Connection connection_;
		private string groupName_;
		private DateTime startTime_;
		private DateTime endTime_;
		private string lastError_ = string.Empty;
		private IGQILogger logger_;
		private GQIDMS dms_;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			dms_ = args.DMS;
			InitializeConnection(dms_);
			logger_ = args.Logger;
			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { GroupName, StartTime, EndTime };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			try
			{
				if (args.TryGetArgumentValue(GroupName, out string groupNameValue))
				{
					groupName_ = groupNameValue;
				}
				else
				{
					groupName_ = string.Empty;
					lastError_ = "No group name provided";
					logger_.Error("Clearing data");
					anomalyScoreData_ = new AnomalyScoreData();
					return new OnArgumentsProcessedOutputArgs();
				}

				if (args.TryGetArgumentValue(StartTime, out DateTime startTimeValue) && args.TryGetArgumentValue(EndTime, out DateTime endTimeValue))
				{
					startTime_ = startTimeValue.ToUniversalTime();
					endTime_ = endTimeValue.ToUniversalTime();
				}
				else
				{
					throw new Exception("Unable to parse input times");
				}
			}
			catch (Exception ex)
			{
				logger_.Error("Cant parse input, abort");
				lastError_ = ex.Message;
			}

			return new OnArgumentsProcessedOutputArgs();
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			// An error occurred while parsing input arguments, so we shouldn't fetch data
			if (lastError_.IsNotNullOrEmpty())
			{
				return new OnPrepareFetchOutputArgs();
			}

			try
			{
				bool fetchData = true;
				if (groupName_ == anomalyScoreData_.GroupName && anomalyScoreData_.AnomalyScores.IsNotNullOrEmpty())
				{
					// ie we've already fetched the data for this group previously
					var lastTimestamp = anomalyScoreData_.AnomalyScores.Last().Key;
					if ((DateTime.UtcNow - lastTimestamp).TotalMinutes <= 5)
					{
						fetchData = false;
					}
				}

				if (fetchData)
				{
					anomalyScoreData_.AnomalyScores.Clear();
					GetMADDataMessage msg = new GetMADDataMessage(groupName_, DateTime.Now.AddMonths(-1), DateTime.Now);
					var madDataReponse = connection_.HandleSingleResponseMessage(msg) as GetMADDataResponseMessage;
					logger_.Error($"Number of points: {madDataReponse.Data.Count}");
					if (madDataReponse?.Parameters != null)
					{
						foreach (MADDataPoint point in madDataReponse.Data)
						{
							anomalyScoreData_.AnomalyScores.Add(new KeyValuePair<DateTime, double>(point.Timestamp, point.AnomalyScore));
						}
					}

					anomalyScoreData_.GroupName = groupName_;
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
			logger_.Error("onGetColumns");
			return new GQIColumn[] { new GQIDateTimeColumn("Time"), new GQIDoubleColumn("AnomalyScore") };
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			// An error occurrend while parsing the input arguments of fetching the data
			if (lastError_.IsNotNullOrEmpty())
			{
				return Error();
			}

			var rows = new List<GQIRow>();
			try
			{
				foreach (var entry in anomalyScoreData_.AnomalyScores)
				{
					if (entry.Key.ToUniversalTime() < startTime_)
					{
						continue;
					}
					else if (entry.Key.ToUniversalTime() > endTime_)
					{
						break;
					}

					GQICell[] cells = new GQICell[2];
					cells[0] = new GQICell { Value = entry.Key.ToUniversalTime() };
					cells[1] = new GQICell { Value = entry.Value };
					rows.Add(new GQIRow(cells));
				}
			}
			catch (Exception)
			{
				return Error();
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
			var rows = new List<GQIRow>();
			lastError_ = null;
			return new GQIPage(rows.ToArray());
		}

		#endregion
	}

	public class AnomalyScoreData
	{
		public AnomalyScoreData()
		{
			AnomalyScores = new List<KeyValuePair<DateTime, double>>();
		}

		public string GroupName { get; set; }

		public List<KeyValuePair<DateTime, double>> AnomalyScores { get; set; }
	}
}