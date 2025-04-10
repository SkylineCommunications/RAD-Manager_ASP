namespace RelationalAnomalyScoreDataSource
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using RadUtils;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Helper;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	[GQIMetaData(Name = "Get Relational Anomaly Score")]
	public class RelationalAnomalyScoreDataSource : IGQIDataSource, IGQIOnInit, IGQIInputArguments, IGQIOnPrepareFetch
	{
		private static readonly GQIStringArgument GroupName = new GQIStringArgument("groupName");
		private static readonly GQIIntArgument DataMinerID = new GQIIntArgument("dataMinerID");
		private static readonly GQIDateTimeArgument StartTime = new GQIDateTimeArgument("startTime");
		private static readonly GQIDateTimeArgument EndTime = new GQIDateTimeArgument("endTime");
		private AnomalyScoreData _anomalyScoreData = new AnomalyScoreData(); //TODO: ask Dennis whether it is OK to make this non-static
		private string _groupName = string.Empty;
		private int _dataMinerID = -1;
		private DateTime? _startTime = null;
		private DateTime? _endTime = null;
		private IGQILogger _logger;

		public OnInitOutputArgs OnInit(OnInitInputArgs args)
		{
			ConnectionHelper.InitializeConnection(args.DMS);
			_logger = args.Logger;
			return default;
		}

		public GQIArgument[] GetInputArguments()
		{
			return new GQIArgument[] { GroupName, DataMinerID, StartTime, EndTime };
		}

		public OnArgumentsProcessedOutputArgs OnArgumentsProcessed(OnArgumentsProcessedInputArgs args)
		{
			if (!args.TryGetArgumentValue(GroupName, out _groupName))
				_logger.Error("No group name provided");

			if (!args.TryGetArgumentValue(DataMinerID, out _dataMinerID))
				_logger.Error("No DataMiner ID provided");

			if (args.TryGetArgumentValue(StartTime, out DateTime startTimeValue) && args.TryGetArgumentValue(EndTime, out DateTime endTimeValue))
			{
				_startTime = startTimeValue;
				_endTime = endTimeValue;//TODO: test that we indeed don't need universal time
			}
			else
			{
				_logger.Error("Unable to parse input times");
			}

			return default;
		}

		public OnPrepareFetchOutputArgs OnPrepareFetch(OnPrepareFetchInputArgs args)
		{
			if (string.IsNullOrEmpty(_groupName) || _startTime == null || _endTime == null)
			{
				_logger.Error("Group name or time range is empty");
				_anomalyScoreData.AnomalyScores.Clear();
				return default;
			}

			if (_groupName == _anomalyScoreData.GroupName && _anomalyScoreData.AnomalyScores.IsNotNullOrEmpty())
			{
				// ie we've already fetched the data for this group previously
				var lastTimestamp = _anomalyScoreData.AnomalyScores.Last().Key;
				if ((DateTime.UtcNow - lastTimestamp).TotalMinutes <= 5)
					return default;
			}

			try
			{
				_anomalyScoreData.AnomalyScores.Clear();
				GetMADDataMessage msg = new GetMADDataMessage(_groupName, DateTime.Now.AddMonths(-1), DateTime.Now)
				{
					DataMinerID = _dataMinerID,
				};
				var madDataReponse = ConnectionHelper.Connection.HandleSingleResponseMessage(msg) as GetMADDataResponseMessage;
				_logger.Information($"Number of points: {madDataReponse?.Data.Count}");
				if (madDataReponse != null)
				{
					foreach (MADDataPoint point in madDataReponse.Data)
					{
						_anomalyScoreData.AnomalyScores.Add(new KeyValuePair<DateTime, double>(point.Timestamp, point.AnomalyScore));
					}
				}

				_anomalyScoreData.GroupName = _groupName;
				_anomalyScoreData.DataMinerID = _dataMinerID;

				return default;
			}
			catch (Exception ex)
			{
				throw new DataMinerCommunicationException("Failed to fetch MAD data", ex);
			}
		}

		public GQIColumn[] GetColumns()
		{
			return new GQIColumn[] { new GQIDateTimeColumn("Time"), new GQIDoubleColumn("AnomalyScore") };
		}

		public GQIPage GetNextPage(GetNextPageInputArgs args)
		{
			var rows = new List<GQIRow>();

			foreach (var entry in _anomalyScoreData.AnomalyScores)
			{
				if (entry.Key < _startTime) //TODO: test this
				{
					continue;
				}
				else if (entry.Key > _endTime)
				{
					break;
				}

				GQICell[] cells = new GQICell[2];
				cells[0] = new GQICell { Value = entry.Key };
				cells[1] = new GQICell { Value = entry.Value };
				rows.Add(new GQIRow(cells));
			}

			return new GQIPage(rows.ToArray());
		}
	}

	public class AnomalyScoreData
	{
		public AnomalyScoreData()
		{
			AnomalyScores = new List<KeyValuePair<DateTime, double>>();
		}

		public string GroupName { get; set; }

		public int DataMinerID { get; set; }

		public List<KeyValuePair<DateTime, double>> AnomalyScores { get; set; }
	}
}