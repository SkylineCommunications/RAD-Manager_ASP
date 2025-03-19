using RADWidgets;
using Skyline.DataMiner.Automation;

namespace AddParameterGroup
{
    public class MultiProtocolParameterSelector : MultiSelector<ProtocolParameterSelectorInfo>
    {
        public void SetProtocol(string protocolName, string protocolVersion)
        {
			var selector = ItemSelector as ProtocolParameterSelector;
			selector.SetProtocol(protocolName, protocolVersion);
			SelectedItems = null;
        }

        public MultiProtocolParameterSelector(string protocolName, string protocolVersion, IEngine engine) : base(new ProtocolParameterSelector(protocolName, protocolVersion, engine)) { }
    }
}
