namespace RadDataSources
{
	using System;
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Cache for element names.
	/// </summary>
	public class ElementNameCache : Cache<string>
	{
		private readonly IGQILogger logger_ = null;

		public ElementNameCache(IGQILogger logger)
		{
			logger_ = logger;
		}

		protected override bool Fetch(int dataMinerID, int elementID, out string value)
		{
			try
			{
				var elementRequest = new GetElementByIDMessage(dataMinerID, elementID);
				var elementResponse = ConnectionHelper.Connection.HandleSingleResponseMessage(elementRequest) as ElementInfoEventMessage;

				if (elementResponse == null)
				{
					logger_.Error($"Failed to fetch element info for element {dataMinerID}/{elementID}: Received no response or response of the wrong type");
					value = string.Empty;
					return false;
				}

				value = elementResponse.Name;
				return true;
			}
			catch (Exception ex)
			{
				logger_.Error($"Failed to fetch element name for element {dataMinerID}/{elementID}: {ex.Message}");
				value = string.Empty;
				return false;
			}
		}
	}
}
