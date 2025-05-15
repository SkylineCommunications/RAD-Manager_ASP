namespace RadUtils
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Analytics.Rad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Class with helper functions to fetch messages from the DataMiner.
	/// </summary>
	public static class RadMessageHelper
	{
		public static List<string> FetchParameterGroups(Connection connection, int dataMinerID)
		{
			return FetchParameterGroups(connection.HandleSingleResponseMessage, dataMinerID);
		}

		public static List<string> FetchParameterGroups(IEngine engine, int dataMinerID)
		{
			return FetchParameterGroups(engine.SendSLNetSingleResponseMessage, dataMinerID);
		}

		public static IRadGroupBaseInfo FetchParameterGroupInfo(Connection connection, int dataMinerID, string groupName)
		{
			return FetchParameterGroupInfo(connection.HandleSingleResponseMessage, dataMinerID, groupName);
		}

		public static IRadGroupBaseInfo FetchParameterGroupInfo(IEngine engine, int dataMinerID, string groupName)
		{
			return FetchParameterGroupInfo(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName);
		}

		public static List<KeyValuePair<DateTime, double>> FetchAnomalyScoreData(Connection connection, int dataMinerID, string groupName,
			DateTime startTime, DateTime endTime)
		{
			return FetchAnomalyScoreData(connection.HandleSingleResponseMessage, dataMinerID, groupName, startTime, endTime);
		}

		public static List<KeyValuePair<DateTime, double>> FetchAnomalyScoreData(IEngine engine, int dataMinerID, string groupName,
			DateTime startTime, DateTime endTime)
		{
			return FetchAnomalyScoreData(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName, startTime, endTime);
		}

		/// <summary>
		/// Fetch the anomaly score data for a subgroup of a shared model group. Supported from TODO: version
		/// </summary>
		/// <param name="connection">The connection to use.</param>
		/// <param name="dataMinerID">The DataMinerID of the group.</param>
		/// <param name="groupName">The name of the shared model group.</param>
		/// <param name="subGroupName">The name of the subgroup</param>
		/// <param name="startTime">The start time of the time range to fetch the anomaly score from.</param>
		/// <param name="endTime">The end time of the time range to fetch the anomaly score from.</param>
		/// <exception cref="TypeLoadException">Thrown if <see cref="GetRADDataMessage"/> is not known.</exception>
		/// <exception cref="MissingMethodException">Thrown if the correct constructor of <see cref="GetRADDataMessage" /> is not known.</exception>
		/// <returns>The anomaly scores.</returns>
		public static List<KeyValuePair<DateTime, double>> FetchAnomalyScoreData(Connection connection, int dataMinerID, string groupName,
			string subGroupName, DateTime startTime, DateTime endTime)
		{
			return FetchRADAnomalyScoreData(connection.HandleSingleResponseMessage, dataMinerID, groupName, subGroupName, startTime, endTime);
		}

		/// <summary>
		/// Fetch the anomaly score data for a subgroup of a shared model group. Supported from TODO: version
		/// </summary>
		/// <param name="engine">The engine object to use to send the message.</param>
		/// <param name="dataMinerID">The DataMinerID of the group.</param>
		/// <param name="groupName">The name of the shared model group.</param>
		/// <param name="subGroupName">The name of the subgroup</param>
		/// <param name="startTime">The start time of the time range to fetch the anomaly score from.</param>
		/// <param name="endTime">The end time of the time range to fetch the anomaly score from.</param>
		/// <exception cref="TypeLoadException">Thrown if <see cref="GetRADDataMessage"/> is not known.</exception>
		/// <exception cref="MissingMethodException">Thrown if the correct constructor of <see cref="GetRADDataMessage" /> is not known.</exception>
		/// <returns>The anomaly scores.</returns>
		public static List<KeyValuePair<DateTime, double>> FetchAnomalyScoreData(IEngine engine, int dataMinerID, string groupName,
			string subGroupName, DateTime startTime, DateTime endTime)
		{
			return FetchRADAnomalyScoreData(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName, subGroupName, startTime, endTime);
		}

		/// <summary>
		/// Fetch the anomaly score data for a subgroup of a shared model group. Supported from TODO: version
		/// </summary>
		/// <param name="connection">The connection to use.</param>
		/// <param name="dataMinerID">The DataMinerID of the group.</param>
		/// <param name="groupName">The name of the shared model group.</param>
		/// <param name="subGroupID">The id of the subgroup</param>
		/// <param name="startTime">The start time of the time range to fetch the anomaly score from.</param>
		/// <param name="endTime">The end time of the time range to fetch the anomaly score from.</param>
		/// <exception cref="TypeLoadException">Thrown if <see cref="GetRADDataMessage"/> is not known.</exception>
		/// <exception cref="MissingMethodException">Thrown if the correct constructor of <see cref="GetRADDataMessage" /> is not known.</exception>
		/// <returns>The anomaly scores.</returns>
		public static List<KeyValuePair<DateTime, double>> FetchAnomalyScoreData(Connection connection, int dataMinerID, string groupName,
			Guid subGroupID, DateTime startTime, DateTime endTime)
		{
			return FetchRADAnomalyScoreData(connection.HandleSingleResponseMessage, dataMinerID, groupName, subGroupID, startTime, endTime);
		}

		/// <summary>
		/// Fetch the anomaly score data for a subgroup of a shared model group. Supported from TODO: version
		/// </summary>
		/// <param name="engine">The engine object to use to send the message.</param>
		/// <param name="dataMinerID">The DataMinerID of the group.</param>
		/// <param name="groupName">The name of the shared model group.</param>
		/// <param name="subGroupID">The id of the subgroup</param>
		/// <param name="startTime">The start time of the time range to fetch the anomaly score from.</param>
		/// <param name="endTime">The end time of the time range to fetch the anomaly score from.</param>
		/// <exception cref="TypeLoadException">Thrown if <see cref="GetRADDataMessage"/> is not known.</exception>
		/// <exception cref="MissingMethodException">Thrown if the correct constructor of <see cref="GetRADDataMessage" /> is not known.</exception>
		/// <returns>The anomaly scores.</returns>
		public static List<KeyValuePair<DateTime, double>> FetchAnomalyScoreData(IEngine engine, int dataMinerID, string groupName,
			Guid subGroupID, DateTime startTime, DateTime endTime)
		{
			return FetchRADAnomalyScoreData(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName, subGroupID, startTime, endTime);
		}

		public static void RemoveParameterGroup(Connection connection, int dataMinerID, string groupName)
		{
			RemoveParameterGroup(connection.HandleSingleResponseMessage, dataMinerID, groupName);
		}

		public static void RemoveParameterGroup(IEngine engine, int dataMinerID, string groupName)
		{
			RemoveParameterGroup(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName);
		}

		public static void AddParameterGroup(Connection connection, MADGroupInfo groupInfo)
		{
			AddParameterGroup(connection.HandleSingleResponseMessage, groupInfo);
		}

		public static void AddParameterGroup(IEngine engine, MADGroupInfo groupInfo)
		{
			AddParameterGroup(engine.SendSLNetSingleResponseMessage, groupInfo);
		}

		/// <summary>
		/// Rename a parameter group. Supported from TODO: version
		/// </summary>
		/// <param name="connection">The connection to use.</param>
		/// <param name="dataMinerID">The DataMiner ID of the group.</param>
		/// <param name="oldGroupName">The old group name.</param>
		/// <param name="newGroupName">The new group name.</param>
		/// <exception cref="TypeLoadException">Thrown if the <see cref="RenameRADParameterGroupMessage"/> is not known.</exception>
		public static void RenameParameterGroup(Connection connection, int dataMinerID, string oldGroupName, string newGroupName)
		{
			RenameParameterGroup(connection.HandleSingleResponseMessage, dataMinerID, oldGroupName, newGroupName);
		}

		/// <summary>
		/// Rename a parameter group. Supported from TODO: version
		/// </summary>
		/// <param name="engine">The engine object to use to send the message.</param>
		/// <param name="dataMinerID">The DataMiner ID of the group.</param>
		/// <param name="oldGroupName">The old group name.</param>
		/// <param name="newGroupName">The new group name.</param>
		/// <exception cref="TypeLoadException">Thrown if the RenameRADParmaeterGroupMessage is not known.</exception>
		public static void RenameParameterGroup(IEngine engine, int dataMinerID, string oldGroupName, string newGroupName)
		{
			RenameParameterGroup(engine.SendSLNetSingleResponseMessage, dataMinerID, oldGroupName, newGroupName);
		}

		public static void RetrainParameterGroup(Connection connection, int dataMinerID, string groupName, IEnumerable<TimeRange> timeRanges)
		{
			RetrainParameterGroup(connection.HandleSingleResponseMessage, dataMinerID, groupName, timeRanges);
		}

		public static void RetrainParameterGroup(IEngine engine, int dataMinerID, string groupName, IEnumerable<TimeRange> timeRanges)
		{
			RetrainParameterGroup(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName, timeRanges);
		}

#pragma warning disable CS0618 // Type or member is obsolete: messages are obsolete since 10.5.5, but replacements were only added in that version
		private static List<string> FetchParameterGroups(Func<DMSMessage, DMSMessage> sendMessageFunc, int dataMinerID)
		{
			GetMADParameterGroupsMessage request = new GetMADParameterGroupsMessage()
			{
				DataMinerID = dataMinerID,
			};

			var response = sendMessageFunc(request) as GetMADParameterGroupsResponseMessage;
			return response?.GroupNames;
		}

		/// <summary>
		/// Supported from TODO: version
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static IRadGroupBaseInfo FetchRADParameterGroupInfo(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName)
		{
			GetRADParameterGroupInfoMessage request = new GetRADParameterGroupInfoMessage(groupName)
			{
				DataMinerID = dataMinerID,
			};
			var response = sendMessageFunc(request);
			if (response is GetRADParameterGroupInfoResponseMessage parameterGroupInfoResponse)
			{
				if (parameterGroupInfoResponse.ParameterGroupInfo == null)
					return null;

				return new RadGroupInfo()
				{
					GroupName = parameterGroupInfoResponse.ParameterGroupInfo.Name,
					Parameters = parameterGroupInfoResponse.ParameterGroupInfo.Parameters,
					Options = new RadGroupOptions()
					{
						UpdateModel = parameterGroupInfoResponse.ParameterGroupInfo.UpdateModel,
						AnomalyThreshold = parameterGroupInfoResponse.ParameterGroupInfo.AnomalyThreshold,
						MinimalDuration = parameterGroupInfoResponse.ParameterGroupInfo.MinimumAnomalyDuration,
					},
					IsMonitored = parameterGroupInfoResponse.IsMonitored,
				};
			}
			else if (response is GetRADSharedModelGroupInfoResponseMessage sharedModelGroupInfoResponse)
			{
				if (sharedModelGroupInfoResponse.GroupInfo == null)
					return null;

				var subgroups = new List<RadSubgroupInfo>();
				foreach (var subgroup in sharedModelGroupInfoResponse.GroupInfo.ParameterGroups)
				{
					subgroups.Add(new RadSubgroupInfo()
					{
						Name = subgroup.Name,
						Parameters = subgroup.Parameters,
						Options = new RadSubgroupOptions()
						{
							AnomalyThreshold = subgroup.AnomalyThreshold,
							MinimalDuration = subgroup.MinimumAnomalyDuration,
						},
						IsMonitored = true, // TODO: add this
					});
				}

				return new RadSharedModelGroupInfo()
				{
					GroupName = sharedModelGroupInfoResponse.GroupInfo.Name,
					Options = new RadGroupOptions()
					{
						UpdateModel = sharedModelGroupInfoResponse.GroupInfo.UpdateModel,
						AnomalyThreshold = sharedModelGroupInfoResponse.GroupInfo.AnomalyThreshold,
						MinimalDuration = sharedModelGroupInfoResponse.GroupInfo.MinimumAnomalyDuration,
					},
					Subgroups = subgroups,
				};
			}
			else
			{
				return null;
			}
		}

		private static RadGroupInfo FetchMADParameterGroupInfo(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName)
		{
			GetMADParameterGroupInfoMessage request = new GetMADParameterGroupInfoMessage(groupName)
			{
				DataMinerID = dataMinerID,
			};

			var response = sendMessageFunc(request) as GetMADParameterGroupInfoResponseMessage;
			if (response?.GroupInfo == null)
				return null;

			return new RadGroupInfo()
			{
				GroupName = response.GroupInfo.Name,
				Parameters = response.GroupInfo.Parameters,
				Options = new RadGroupOptions()
				{
					UpdateModel = response.GroupInfo.UpdateModel,
					AnomalyThreshold = response.GroupInfo.AnomalyThreshold,
					MinimalDuration = response.GroupInfo.MinimumAnomalyDuration,
				},
			};
		}

		private static IRadGroupBaseInfo FetchParameterGroupInfo(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName)
		{
			try
			{
				return FetchRADParameterGroupInfo(sendMessageFunc, dataMinerID, groupName);
			}
			catch (TypeLoadException)
			{
			}
			catch (MissingMethodException)
			{
			}

			// New method failed, try the old one
			return FetchMADParameterGroupInfo(sendMessageFunc, dataMinerID, groupName);
		}

		/// <summary>
		/// Supported from TODO: version
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static List<KeyValuePair<DateTime, double>> FetchRADAnomalyScoreData(Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID, string groupName, string subGroupName, DateTime startTime, DateTime endTime)
		{
			GetRADDataMessage request = new GetRADDataMessage(groupName, subGroupName, startTime, endTime)
			{
				DataMinerID = dataMinerID,
			};
			var response = sendMessageFunc(request) as GetRADDataResponseMessage;
			return response?.DataPoints.Select(p => new KeyValuePair<DateTime, double>(p.Timestamp.ToUniversalTime(), p.AnomalyScore)).ToList();
		}

		/// <summary>
		/// Supported from TODO: version
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static List<KeyValuePair<DateTime, double>> FetchRADAnomalyScoreData(Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID, string groupName, Guid subGroupID, DateTime startTime, DateTime endTime)
		{
			GetRADDataMessage request = new GetRADDataMessage(groupName, subGroupID.ToString(), startTime, endTime) //TODO: remove the toString in the GUID
			{
				DataMinerID = dataMinerID,
			};
			var response = sendMessageFunc(request) as GetRADDataResponseMessage;
			return response?.DataPoints.Select(p => new KeyValuePair<DateTime, double>(p.Timestamp.ToUniversalTime(), p.AnomalyScore)).ToList();
		}

		private static List<KeyValuePair<DateTime, double>> FetchAnomalyScoreData(Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID, string groupName, DateTime startTime, DateTime endTime)
		{
			GetMADDataMessage request = new GetMADDataMessage(groupName, startTime, endTime)
			{
				DataMinerID = dataMinerID,
			};
			var response = sendMessageFunc(request) as GetMADDataResponseMessage;
			return response?.Data.Select(p => new KeyValuePair<DateTime, double>(p.Timestamp.ToUniversalTime(), p.AnomalyScore)).ToList();
		}

		private static RemoveMADParameterGroupResponseMessage RemoveParameterGroup(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName)
		{
			var request = new RemoveMADParameterGroupMessage(groupName)
			{
				DataMinerID = dataMinerID,
			};
			return sendMessageFunc(request) as RemoveMADParameterGroupResponseMessage;
		}

		private static AddMADParameterGroupResponseMessage AddParameterGroup(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			MADGroupInfo groupInfo)
		{
			var request = new AddMADParameterGroupMessage(groupInfo);
			return sendMessageFunc(request) as AddMADParameterGroupResponseMessage;
		}

		/// <summary>
		/// Supported from TODO: version
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static RenameRADParameterGroupResponseMessage RenameParameterGroup(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string oldGroupName,
			string newGroupName)
		{
			var request = new RenameRADParameterGroupMessage(oldGroupName, newGroupName)
			{
				DataMinerID = dataMinerID,
			};
			return sendMessageFunc(request) as RenameRADParameterGroupResponseMessage;
		}

		private static RetrainMADModelResponseMessage RetrainParameterGroup(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName,
			IEnumerable<TimeRange> timeRanges)
		{
			var request = new RetrainMADModelMessage(groupName, timeRanges.Select(r => new Skyline.DataMiner.Analytics.Mad.TimeRange(r.Start, r.End)).ToList())
			{
				DataMinerID = dataMinerID,
			};
			return sendMessageFunc(request) as RetrainMADModelResponseMessage;
		}
#pragma warning restore CS0618 // Type or member is obsolete
	}

	public class TimeRange
	{
		public TimeRange(DateTime start, DateTime end)
		{
			Start = start;
			End = end;
		}

		public DateTime Start { get; set; }

		public DateTime End { get; set; }
	}
}
