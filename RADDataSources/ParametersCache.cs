namespace RadDataSources
{
	using Skyline.DataMiner.Analytics.GenericInterface;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Cache for parameters.
	/// </summary>
	public class ParametersCache : RadUtils.ParametersCache
	{
		private readonly IGQILogger _logger = null;
		private readonly ConnectionHelper _connectionHelper;

		public ParametersCache(IGQILogger logger, ConnectionHelper connectionHelper)
		{
			_logger = logger;
			_connectionHelper = connectionHelper ?? throw new System.ArgumentNullException(nameof(connectionHelper));
		}

		protected override void LogError(string message)
		{
			_logger.Error(message);
		}

		protected override DMSMessage SendSingleResponseMessage(DMSMessage request)
		{
			return _connectionHelper.Connection.HandleSingleResponseMessage(request);
		}
	}
}
