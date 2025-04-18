namespace RadUtils
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Analytics.Mad;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Class with helper functions to fetch messages from the DataMiner.
	/// </summary>
	public static class RadMessageHelper
	{
		public static GetMADParameterGroupsResponseMessage FetchParameterGroups(Connection connection, int dataMinerID)
		{
			return FetchParameterGroups(connection.HandleSingleResponseMessage, dataMinerID);
		}

		public static GetMADParameterGroupsResponseMessage FetchParameterGroups(IEngine engine, int dataMinerID)
		{
			return FetchParameterGroups(engine.SendSLNetSingleResponseMessage, dataMinerID);
		}

		public static MADGroupInfo FetchParameterGroupInfo(Connection connection, int dataMinerID, string groupName)
		{
			return FetchParameterGroupInfo(connection.HandleSingleResponseMessage, dataMinerID, groupName);
		}

		public static MADGroupInfo FetchParameterGroupInfo(IEngine engine, int dataMinerID, string groupName)
		{
			return FetchParameterGroupInfo(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName);
		}

		public static GetMADDataResponseMessage FetchRADData(Connection connection, int dataMinerID, string groupName, DateTime startTime, DateTime endTime)
		{
			return FetchRADData(connection.HandleSingleResponseMessage, dataMinerID, groupName, startTime, endTime);
		}

		public static GetMADDataResponseMessage FetchRADData(IEngine engine, int dataMinerID, string groupName, DateTime startTime, DateTime endTime)
		{
			return FetchRADData(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName, startTime, endTime);
		}

		public static RemoveMADParameterGroupResponseMessage RemoveParameterGroup(Connection connection, int dataMinerID, string groupName)
		{
			return RemoveParameterGroup(connection.HandleSingleResponseMessage, dataMinerID, groupName);
		}

		public static RemoveMADParameterGroupResponseMessage RemoveParameterGroup(IEngine engine, int dataMinerID, string groupName)
		{
			return RemoveParameterGroup(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName);
		}

		public static AddMADParameterGroupResponseMessage AddParameterGroup(Connection connection, MADGroupInfo groupInfo)
		{
			return AddParameterGroup(connection.HandleSingleResponseMessage, groupInfo);
		}

		public static AddMADParameterGroupResponseMessage AddParameterGroup(IEngine engine, MADGroupInfo groupInfo)
		{
			return AddParameterGroup(engine.SendSLNetSingleResponseMessage, groupInfo);
		}

		public static RetrainMADModelResponseMessage RetrainParameterGroup(Connection connection, int dataMinerID, string groupName, List<Skyline.DataMiner.Analytics.Mad.TimeRange> timeRanges)
		{
			return RetrainParameterGroup(connection.HandleSingleResponseMessage, dataMinerID, groupName, timeRanges);
		}

		public static RetrainMADModelResponseMessage RetrainParameterGroup(IEngine engine, int dataMinerID, string groupName, List<Skyline.DataMiner.Analytics.Mad.TimeRange> timeRanges)
		{
			return RetrainParameterGroup(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName, timeRanges);
		}

#pragma warning disable CS0618 // Type or member is obsolete: messages are obsolete since 10.5.5, but replacements were only added in that version
		private static GetMADParameterGroupsResponseMessage FetchParameterGroups(Func<DMSMessage, DMSMessage> sendMessageFunc, int dataMinerID)
		{
			GetMADParameterGroupsMessage request = new GetMADParameterGroupsMessage()
			{
				DataMinerID = dataMinerID,
			};

			return sendMessageFunc(request) as GetMADParameterGroupsResponseMessage;
		}

		private static MADGroupInfo FetchParameterGroupInfo(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName)
		{
			GetMADParameterGroupInfoMessage request = new GetMADParameterGroupInfoMessage(groupName)
			{
				DataMinerID = dataMinerID,
			};

			var response = sendMessageFunc(request) as GetMADParameterGroupInfoResponseMessage;
			return response?.GroupInfo;
		}

		private static GetMADDataResponseMessage FetchRADData(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName,
			DateTime startTime,
			DateTime endTime)
		{
			GetMADDataMessage request = new GetMADDataMessage(groupName, startTime, endTime)
			{
				DataMinerID = dataMinerID,
			};
			return sendMessageFunc(request) as GetMADDataResponseMessage;
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

		private static RetrainMADModelResponseMessage RetrainParameterGroup(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName,
			List<Skyline.DataMiner.Analytics.Mad.TimeRange> timeRanges)
		{
			var request = new RetrainMADModelMessage(groupName, timeRanges)
			{
				DataMinerID = dataMinerID,
			};
			return sendMessageFunc(request) as RetrainMADModelResponseMessage;
		}
#pragma warning restore CS0618 // Type or member is obsolete
	}
}
