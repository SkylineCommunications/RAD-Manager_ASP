using Skyline.DataMiner.Analytics.DataTypes;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.AutomationUI.Objects;
using System.Collections.Generic;
using System.Linq;

namespace AddParameterGroup
{
    public class MultiProtocolParameterSelector : MultiParameterSelectorBase
    {
        public List<SlimProtocolParameterSelectorInfo> SelectedParameters
        {
            get
            {
                return SelectedItems.Select(s => SlimProtocolParameterSelectorInfo.Parse(s)).ToList();
            }
        }

        public void SetProtocol(string protocolName, string protocolVersion)
        {
            (addSelector_ as ProtocolParameterSelector)?.SetProtocol(protocolName, protocolVersion);
            selectedParametersView_.Items = new List<TreeViewItem>();
        }

        public MultiProtocolParameterSelector(string protocolName, string protocolVersion, IEngine engine) : base(new ProtocolParameterSelector(protocolName, protocolVersion, engine), engine) { }
    }
}
