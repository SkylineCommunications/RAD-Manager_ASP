namespace AddParameterGroup
{
	using RadWidgets;
	using Skyline.DataMiner.Automation;

	public class MultiProtocolParameterSelector : MultiSelector<ProtocolParameterSelectorInfo>
    {
		public MultiProtocolParameterSelector(string protocolName, string protocolVersion, IEngine engine) :
			base(new ProtocolParameterSelector(protocolName, protocolVersion, engine))
		{
			AddButtonTooltip = "Add the instance specified on the left to the parameter groups.";
			RemoveButtonTooltip = "Remove the instance(s) selected on the left from the parameter groups.";
			SelectedItemsViewTooltip = "The instances that will be added to the groups. For each element, only the instances available on that element will be added in the corresponding group.";
		}

		public void SetProtocol(string protocolName, string protocolVersion)
        {
			var selector = ItemSelector as ProtocolParameterSelector;
			selector.SetProtocol(protocolName, protocolVersion);
			SetSelected(null);
        }
    }
}
