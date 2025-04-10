namespace RadDataSources
{
	using System;
	using System.Linq;
	using RadUtils;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Cache for parameters.
	/// </summary>
	public class ParametersCache : Cache<ParameterInfo[]>
	{
		private readonly IGQILogger logger_ = null;

		public ParametersCache(IGQILogger logger)
		{
			logger_ = logger;
		}

		protected override bool Fetch(int dataMinerID, int elementID, out ParameterInfo[] value)
		{
			try
			{
				var protocolRequest = new GetElementProtocolMessage(dataMinerID, elementID);
				var protocolResponse = ConnectionHelper.Connection.HandleSingleResponseMessage(protocolRequest) as GetElementProtocolResponseMessage;

				if (protocolResponse == null)
				{
					logger_.Error($"Failed to fetch protocol for element {dataMinerID}/{elementID}: Received no response or response of the wrong type");
					value = new ParameterInfo[0];
					return false;
				}

				value = protocolResponse.AllParameters.Where(p => p.IsRadSupported()).ToArray();
				return true;
			}
			catch (Exception ex)
			{
				logger_.Error($"Failed to fetch element name for element {dataMinerID}/{elementID}: {ex.Message}");
				value = new ParameterInfo[0];
				return false;
			}
		}
	}
}
