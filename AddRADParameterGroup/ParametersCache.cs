namespace AddRadParameterGroup
{
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Cache for parameters.
	/// </summary>
	public class ParametersCache : RadUtils.ParametersCache
	{
		private readonly IEngine _engine;

		public ParametersCache(IEngine engine)
		{
			_engine = engine;
		}

		protected override void LogError(string message)
		{
			_engine.Log(message);
		}

		protected override DMSMessage SendSingleResponseMessage(DMSMessage request)
		{
			return _engine.SendSLNetSingleResponseMessage(request);
		}
	}
}
