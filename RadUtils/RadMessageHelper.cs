namespace RadUtils
{
	using System;
	using System.Collections.Generic;
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

		public static RemoveRADParameterGroupResponseMessage RemoveParameterGroup(Connection connection, int dataMinerID, string groupName)
		{
			return RemoveParameterGroup(connection.HandleSingleResponseMessage, dataMinerID, groupName);
		}

		public static RemoveRADParameterGroupResponseMessage RemoveParameterGroup(IEngine engine, int dataMinerID, string groupName)
		{
			return RemoveParameterGroup(engine.SendSLNetSingleResponseMessage, dataMinerID, groupName);
		}

		public static AddRADParameterGroupResponseMessage AddParameterGroup(Connection connection, MADGroupInfo groupInfo)
		{
			return AddParameterGroup(connection.HandleSingleResponseMessage, groupInfo);
		}

		public static AddRADParameterGroupResponseMessage AddParameterGroup(IEngine engine, MADGroupInfo groupInfo)
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
			GetMADParameterGroupInfoMessage msg = new GetMADParameterGroupInfoMessage(groupName)
			{
				DataMinerID = dataMinerID,
			};

			var response = sendMessageFunc(sendMessageFunc(msg)) as GetMADParameterGroupInfoResponseMessage;
			return response?.GroupInfo;
		}

		private static GetMADDataResponseMessage FetchRADData(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName,
			DateTime startTime,
			DateTime endTime)
		{
			GetMADDataMessage msg = new GetMADDataMessage(groupName, startTime, endTime)
			{
				DataMinerID = dataMinerID,
			};
			return sendMessageFunc(sendMessageFunc(msg)) as GetMADDataResponseMessage;
		}

		private static RemoveRADParameterGroupResponseMessage RemoveParameterGroup(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			int dataMinerID,
			string groupName)
		{
			var request = new RemoveMADParameterGroupMessage(groupName)
			{
				DataMinerID = dataMinerID,
			};
			return sendMessageFunc(request) as RemoveRADParameterGroupResponseMessage;
		}

		private static AddRADParameterGroupResponseMessage AddParameterGroup(
			Func<DMSMessage, DMSMessage> sendMessageFunc,
			MADGroupInfo groupInfo)
		{
			var request = new AddMADParameterGroupMessage(groupInfo);
			return sendMessageFunc(request) as AddRADParameterGroupResponseMessage;
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
