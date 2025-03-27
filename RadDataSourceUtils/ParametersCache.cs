namespace RadDataSourceUtils
{
	using System;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	public class ParametersCache : Cache<ParameterInfo[]>
	{
		private IGQILogger logger_ = null;
		private IConnection connection_ = null;

		public ParametersCache(IConnection connection, IGQILogger logger)
		{
			connection_ = connection;
			logger_ = logger;
		}

		protected override ParameterInfo[] Fetch(int dataMinerID, int elementID)
		{
			try
			{
				var protocolRequest = new GetElementProtocolMessage(dataMinerID, elementID);
				var protocolResponse = connection_.HandleSingleResponseMessage(protocolRequest) as GetElementProtocolResponseMessage;

				if (protocolResponse == null)
				{
					logger_.Error($"Failed to fetch protocol for element {dataMinerID}/{elementID}: Received no response or response of the wrong type");
					return null;
				}

				return protocolResponse?.AllParameters;
			}
			catch (Exception ex)
			{
				logger_.Error($"Failed to fetch element name for element {dataMinerID}/{elementID}: {ex.Message}");
				return null;
			}
		}
	}
}
