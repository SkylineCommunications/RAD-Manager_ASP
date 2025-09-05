namespace AddRadParameterGroup.GroupByProtocolCreator
{
	using RadWidgets.Widgets.Generic;
	using Skyline.DataMiner.Automation;

	public class MultiProtocolParameterSelector : MultiSelector<ProtocolParameterSelectorInfo>
    {
		private bool _parameterAlreadySelected = false;

		public MultiProtocolParameterSelector(string protocolName, string protocolVersion, IEngine engine) :
			base(new ProtocolParameterSelector(protocolName, protocolVersion, engine), null, "No parameters selected")
		{
			AddButtonTooltip = "Add the instance specified on the left to the relational anomaly groups.";
			RemoveButtonTooltip = "Remove the instance(s) selected on the left from the relational anomaly groups.";

			var selector = ItemSelector as ProtocolParameterSelector;
			selector.Changed += (sender, args) => OnChanged();
			Changed += (sender, args) => OnChanged();
		}

		public void SetProtocol(string protocolName, string protocolVersion)
		{
			var selector = ItemSelector as ProtocolParameterSelector;
			selector.SetProtocol(protocolName, protocolVersion);
			SetSelected(null);
		}

		protected override bool AddItem(ProtocolParameterSelectorInfo item)
		{
			if (!base.AddItem(item))
			{
				var selector = ItemSelector as ProtocolParameterSelector;
				selector.ValidationState = UIValidationState.Invalid;
				selector.ValidationText = "Parameter already selected";
				_parameterAlreadySelected = true;
				return false;
			}

			return true;
		}

		private void OnChanged()
		{
			if (_parameterAlreadySelected)
			{
				// If an item has been added or removed, then the validation state of the selector should be reset
				var selector = ItemSelector as ProtocolParameterSelector;
				selector.ValidationState = UIValidationState.Valid;
				selector.ValidationText = string.Empty;
				_parameterAlreadySelected = false;
			}
		}
	}
}
