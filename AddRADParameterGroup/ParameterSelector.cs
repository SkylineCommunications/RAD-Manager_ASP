using Skyline.DataMiner.Analytics.DataTypes;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AddParameterGroup
{
    public class SlimParameterSelectorInfo : ParameterSelectorBaseInfo
    {
        public int DataMinerID { get; set; }
        public int ElementID { get; set; }
        public int ParameterID { get; set; }
        public string DisplayKeyFilter { get; set; }

        public override string ToString()
        {
            return ToParsableString();
        }

        public override string ToParsableString()
        {
            if (!string.IsNullOrEmpty(DisplayKeyFilter))
                return $"{DataMinerID}/{ElementID}/{ParameterID}/{DisplayKeyFilter}";
            else
                return $"{DataMinerID}/{ElementID}/{ParameterID}";
        }

        public static SlimParameterSelectorInfo Parse(string s)
        {
            var parts = s.Split(new char[] { '/' }, 4);
            if (parts.Length < 3)
                throw new ArgumentException($"Invalid parameter key format {s}");

            if (!int.TryParse(parts[0], out int dataMinerId))
                throw new ArgumentException($"Invalid DataMiner ID {parts[0]} in {s}");
            if (!int.TryParse(parts[1], out int elementId))
                throw new ArgumentException($"Invalid element ID {parts[1]} in {s}");
            if (!int.TryParse(parts[2], out int parameterId))
                throw new ArgumentException($"Invalid parameter ID {parts[2]} in {s}");
            string instance = parts.Length == 4 ? parts[3] : "";

            return new SlimParameterSelectorInfo()
            {
                DataMinerID = dataMinerId,
                ElementID = elementId,
                ParameterID = parameterId,
                DisplayKeyFilter = instance
            };
        }
    }

    public class ParameterSelectorInfo : SlimParameterSelectorInfo
    {
        public string ElementName { get; set; }
        public string ParameterName { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(DisplayKeyFilter))
                return $"{ElementName}/{ParameterName}/{DisplayKeyFilter}";
            else
                return $"{ElementName}/{ParameterName}";
        }
    }

    public class ParameterSelector : ParameterSelectorBase
    {
        private DropDown<Element> elementsDropDown_;

        public override ParameterSelectorBaseInfo Parameter
        {
            get
            {
                var element = elementsDropDown_.Selected;
                var parameter = parametersDropDown_.Selected;
                if (element == null || parameter == null)
                    return null;

                return new ParameterSelectorInfo
                {
                    ElementName = element.ElementName,
                    ParameterName = parameter.DisplayName,
                    DataMinerID = element.DmaId,
                    ElementID = element.ElementId,
                    ParameterID = parameter.ID,
                    DisplayKeyFilter = parameter.IsTableColumn ? instanceTextBox_.Text : ""
                };
            }
        }

        protected override bool IsValidForRAD(ParameterInfo info)
        {
            return base.IsValidForRAD(info) && (info.RealTimeTrending || info.AverageTrending) && info.IsTrendAnalyticsSupported;
        }

        private void OnSelectedElementChanged()
        {
            var element = elementsDropDown_.Selected;
            if (element == null)
            {
                ClearPossibleParameters();
                return;
            }
            var request = new GetElementProtocolMessage(element.DmaId, element.ElementId);
            var response = engine_.SendSLNetSingleResponseMessage(request) as GetElementProtocolResponseMessage;
            SetPossibleParameters(response);
        }

        public ParameterSelector(IEngine engine) : base(engine, true)
        {
            var elementsLabel = new Label("Element");
            var elements = engine.FindElements(new ElementFilter()).OrderBy(e => e.ElementName).ToList();
            elementsDropDown_ = new DropDown<Element>()
            {
                Options = elements.Select(e => new Option<Element>(e.ElementName, e)),
                IsDisplayFilterShown = true,
                IsSorted = true
            };
            elementsDropDown_.Changed += (sender, args) => OnSelectedElementChanged();
            OnSelectedElementChanged();

            AddWidget(elementsLabel, 0, 0);
            AddWidget(elementsDropDown_, 1, 0);
        }
    }
}
