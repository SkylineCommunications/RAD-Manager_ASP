using RADWidgets;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;
using System;
using System.Text.RegularExpressions;

namespace AddParameterGroup
{
    public class ProtocolParameterSelectorInfo : MultiSelectorItem
    {
        public string ParameterName { get; set; }
		public int ParameterID { get; set; }
		public string DisplayKeyFilter { get; set; }

		public override string GetKey()
		{
			if (!string.IsNullOrEmpty(DisplayKeyFilter))
				return $"{ParameterID}/{DisplayKeyFilter}";
			else
				return $"{ParameterID}";
		}

		public override string GetDisplayValue()
        {
            if (!string.IsNullOrEmpty(DisplayKeyFilter))
                return $"{ParameterName}/{DisplayKeyFilter}";
            else
                return $"{ParameterName}";
        }
    }

    public class ProtocolParameterSelector : ParameterSelectorBase<ProtocolParameterSelectorInfo>
    {
        public override ProtocolParameterSelectorInfo SelectedItem
		{
            get
            {
                var parameter = parametersDropDown_.Selected;
                if (parameter == null)
                    return null;

                return new ProtocolParameterSelectorInfo
                {
                    ParameterName = parameter.DisplayName,
                    ParameterID = parameter.ID,
                    DisplayKeyFilter = parameter.IsTableColumn ? instanceTextBox_.Text : ""
                };
            }
        }

        public void SetProtocol(string protocolName, string protocolVersion)
        {
            if (string.IsNullOrEmpty(protocolName) || string.IsNullOrEmpty(protocolVersion))
            {
                ClearPossibleParameters();
                return;
            }
            var request = new GetProtocolMessage(protocolName, protocolVersion);
            var response = engine_.SendSLNetSingleResponseMessage(request) as GetProtocolInfoResponseMessage;
            SetPossibleParameters(response);
        }

        public ProtocolParameterSelector(string protocolName, string protocolVersion, IEngine engine) : base(engine, false)
        {
            SetProtocol(protocolName, protocolVersion);
        }
    }
}
