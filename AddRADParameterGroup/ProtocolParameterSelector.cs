using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;
using System;
using System.Text.RegularExpressions;

namespace AddParameterGroup
{
    public class SlimProtocolParameterSelectorInfo : ParameterSelectorBaseInfo
    {
        public int ParameterID { get; set; }
        public string DisplayKeyFilter { get; set; }

        public override string ToString()
        {
            return ToParsableString();
        }

        public override string ToParsableString()
        {
            if (!string.IsNullOrEmpty(DisplayKeyFilter))
                return $"{ParameterID}/{DisplayKeyFilter}";
            else
                return $"{ParameterID}";
        }

        public static SlimProtocolParameterSelectorInfo Parse(string s)
        {
            var parts = s.Split(new char[] { '/' }, 2);
            if (!int.TryParse(parts[0], out int parameterId))
                throw new ArgumentException($"Invalid parameter ID {parts[0]} in {s}");
            string instance = parts.Length == 2 ? parts[1] : "";
            
            return new SlimProtocolParameterSelectorInfo()
            {
                ParameterID = parameterId,
                DisplayKeyFilter = instance
            };
        }
    }

    public class ProtocolParameterSelectorInfo : SlimProtocolParameterSelectorInfo
    {
        public string ParameterName { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(DisplayKeyFilter))
                return $"{ParameterName}/{DisplayKeyFilter}";
            else
                return $"{ParameterName}";
        }
    }

    public class ProtocolParameterSelector : ParameterSelectorBase
    {
        public override ParameterSelectorBaseInfo Parameter
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
