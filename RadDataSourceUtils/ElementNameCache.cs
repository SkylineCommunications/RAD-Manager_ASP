namespace RadDataSourceUtils
{
	using System;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	public class ElementNameCache : Cache<string>
	{
		private IGQILogger logger_ = null;
		private IConnection connection_ = null;

		public ElementNameCache(IConnection connection, IGQILogger logger)
		{
			connection_ = connection;
			logger_ = logger;
		}

		protected override string Fetch(int dataMinerID, int elementID)
		{
			try
			{
				var elementRequest = new GetElementByIDMessage(dataMinerID, elementID);
				var elementResponse = connection_.HandleSingleResponseMessage(elementRequest) as ElementInfoEventMessage;

				if (elementResponse == null)
				{
					logger_.Error($"Failed to fetch element info for element {dataMinerID}/{elementID}: Received no response or response of the wrong type");
					return null;
				}

				return elementResponse?.Name;
			}
			catch (Exception ex)
			{
				logger_.Error($"Failed to fetch element name for element {dataMinerID}/{elementID}: {ex.Message}");
				return null;
			}
		}
	}
}
