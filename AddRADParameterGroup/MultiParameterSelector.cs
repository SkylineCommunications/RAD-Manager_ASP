using Skyline.DataMiner.Automation;
using System.Collections.Generic;
using System.Linq;

namespace AddParameterGroup
{
    public class MultiParameterSelector : MultiParameterSelectorBase
    {
        public List<SlimParameterSelectorInfo> SelectedParameters
        {
            get
            {
                return SelectedItems.Select(s => SlimParameterSelectorInfo.Parse(s)).ToList();
            }
        }

        public MultiParameterSelector(IEngine engine) : base(new ParameterSelector(engine), engine) { }
    }
}
