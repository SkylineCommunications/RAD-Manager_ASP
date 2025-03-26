namespace AddParameterGroup
{
	using RADWidgets;
	using Skyline.DataMiner.Automation;

	public class MultiProtocolParameterSelector : MultiSelector<ProtocolParameterSelectorInfo>
    {
		public MultiProtocolParameterSelector(string protocolName, string protocolVersion, IEngine engine) :
			base(new ProtocolParameterSelector(protocolName, protocolVersion, engine))
		{
		}

		public void SetProtocol(string protocolName, string protocolVersion)
        {
			var selector = ItemSelector as ProtocolParameterSelector;
			selector.SetProtocol(protocolName, protocolVersion);
			SetSelected(null);
        }
    }
}
