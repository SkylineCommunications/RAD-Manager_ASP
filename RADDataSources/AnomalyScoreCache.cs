namespace RadDataSources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Utils.RadToolkit;

	public class AnomalyScoreData
	{
		public AnomalyScoreData(string userDomainName, int dataMinerID, string groupName, string subGroupName, Guid subGroupID,
			DateTime cacheTime, DateTime requestStartTime, DateTime requestEndTime, List<KeyValuePair<DateTime, double>> anomalyScores)
		{
			UserDomainName = userDomainName;
			DataMinerID = dataMinerID;
			GroupName = groupName;
			SubGroupName = subGroupName;
			SubGroupID = subGroupID;
			CacheTime = cacheTime;
			RequestStartTime = requestStartTime;
			RequestEndTime = requestEndTime;
			AnomalyScores = anomalyScores ?? new List<KeyValuePair<DateTime, double>>();
		}

		public string UserDomainName { get; set; }

		public int DataMinerID { get; set; }

		public string GroupName { get; set; }

		public string SubGroupName { get; set; }

		public Guid SubGroupID { get; set; }

		public List<KeyValuePair<DateTime, double>> AnomalyScores { get; set; }

		public DateTime CacheTime { get; set; }

		public DateTime RequestStartTime { get; set; }

		public DateTime RequestEndTime { get; set; }

		public bool IsSameUserAndSubgroup(string userDomainName, int dataMinerID, string groupName, string subGroupName, Guid subGroupID)
		{
			return UserDomainName == userDomainName &&
				DataMinerID == dataMinerID &&
				GroupName == groupName &&
				SubGroupName == subGroupName &&
				SubGroupID == subGroupID;
			}

		public bool IsValidEntry(string userDomainName, int dataMinerID, string groupName, string subGroupName, Guid subGroupID,
			DateTime startTime, DateTime endTime)
		{
			return IsSameUserAndSubgroup(userDomainName, dataMinerID, groupName, subGroupName, subGroupID) &&
				DateTime.UtcNow <= CacheTime.AddMinutes(5) &&
				startTime >= RequestStartTime.AddMinutes(-5) &&
				endTime <= RequestEndTime.AddMinutes(5);
		}
	}

	public class AnomalyScoreCache
	{
		private const int MAX_CACHE_SIZE = 5;
		private readonly object _anomalyScoreDataLock = new object();
		private List<AnomalyScoreData> _anomalyScoreData = new List<AnomalyScoreData>();

		public List<KeyValuePair<DateTime, double>> GetAnomalyScores(ConnectionHelper helper, int dataMinerID, string groupName, string subGroupName,
			Guid subGroupID, DateTime startTime, DateTime endTime, bool skipCache)
		{
			lock (_anomalyScoreDataLock)
			{
				AnomalyScoreData scoreData = null;

				if (!skipCache)
				{
					scoreData = _anomalyScoreData
						.FirstOrDefault(p => p.IsValidEntry(helper.Connection.UserDomainName, dataMinerID, groupName, subGroupName, subGroupID,
						startTime, endTime));
				}

				if (scoreData == null)
				{
					scoreData = UpdateAnomalyScoreData(helper, dataMinerID, groupName, subGroupName, subGroupID, startTime, endTime);
				}

				return scoreData.AnomalyScores
						.Where(p => p.Key >= startTime && p.Key <= endTime)
						.ToList();
			}
		}

		private AnomalyScoreData UpdateAnomalyScoreData(ConnectionHelper helper, int dataMinerID, string groupName, string subGroupName, Guid subGroupID, DateTime startTime,
			DateTime endTime)
		{
			DateTime now = DateTime.UtcNow;

			try
			{
				var requestStartTime = Min(now.AddDays(-7), startTime);
				var requestEndTime = Max(now, endTime);
				List<KeyValuePair<DateTime, double>> anomalyScores = null;

				anomalyScores = FetchAnomalyScore(helper.RadHelper, dataMinerID, groupName, subGroupName, subGroupID, requestStartTime, requestEndTime);
				if (anomalyScores == null)
					throw new DataMinerCommunicationException("No response or a response of the wrong type received");

				_anomalyScoreData.RemoveAll(p => p.IsSameUserAndSubgroup(helper.Connection.UserDomainName, dataMinerID, groupName,
					subGroupName, subGroupID));
				if (_anomalyScoreData.Count >= MAX_CACHE_SIZE)
					_anomalyScoreData.RemoveAt(0); // Remove the oldest entry if cache size exceeds limit

				var newScoreData = new AnomalyScoreData(helper.Connection.UserDomainName, dataMinerID, groupName, subGroupName, subGroupID,
					now, requestStartTime, requestEndTime, anomalyScores);
				_anomalyScoreData.Add(newScoreData);
				return newScoreData;
			}
			catch (Exception ex)
			{
				throw new DataMinerCommunicationException("Failed to fetch RAD data", ex);
			}
		}

		private List<KeyValuePair<DateTime, double>> FetchAnomalyScore(RadHelper helper, int dataMinerID, string groupName, string subGroupName,
			Guid subGroupID, DateTime startTime, DateTime endTime)
		{
			try
			{
				if (subGroupID != Guid.Empty)
					return helper.FetchAnomalyScoreData(dataMinerID, groupName, subGroupID, startTime, endTime);
				if (!string.IsNullOrEmpty(subGroupName))
					return helper.FetchAnomalyScoreData(dataMinerID, groupName, subGroupName, startTime, endTime);
			}
			catch (NotSupportedException)
			{
				// If the method is not supported, we fall back to the method without fetching on subgroup
			}

			return helper.FetchAnomalyScoreData(dataMinerID, groupName, startTime, endTime);
		}

		private DateTime Min(DateTime time1, DateTime time2)
		{
			return time1 < time2 ? time1 : time2;
		}

		private DateTime Max(DateTime time1, DateTime time2)
		{
			return time1 > time2 ? time1 : time2;
		}
	}
}
