using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.AutomationUI.Objects;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AddParameterGroup
{
    public abstract class MultiParameterSelectorBase : Section
    {
        protected ParameterSelectorBase addSelector_;
        protected TreeView selectedParametersView_;

        public event EventHandler Changed;

        protected List<string> SelectedItems
        {
            get
            {
                return selectedParametersView_.Items.Select(i => i.KeyValue).ToList();
            }
        }

        private void AddButton_Pressed(object sender, EventArgs e)
        {
            var parameter = addSelector_.Parameter;
            if (parameter == null)
                return;
            if (selectedParametersView_.Items.Any(i => i.KeyValue == parameter.ToString()))
                return;

            var item = new TreeViewItem(parameter.ToString(), parameter.ToParsableString());
            selectedParametersView_.Items = selectedParametersView_.Items.Concat(new List<TreeViewItem>() { item }).ToList();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void RemoveButton_Pressed(object sender, EventArgs e)
        {
            var newItems = selectedParametersView_.Items.Where(i => !i.IsChecked).ToList();
            if (selectedParametersView_.Items.Count() != newItems.Count())
            {
                selectedParametersView_.Items = newItems;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        protected MultiParameterSelectorBase(ParameterSelectorBase addSelector, IEngine engine) : base()
        {
            addSelector_ = addSelector;

            selectedParametersView_ = new TreeView(new List<TreeViewItem>())
            {
                IsReadOnly = false,
                MinHeight = 100
            };

            var addButton = new Button("Add parameter");
            addButton.Pressed += AddButton_Pressed;

            var removeButton = new Button("Remove parameter");
            removeButton.Pressed += RemoveButton_Pressed;

            int row = 0;
            AddSection(addSelector_, row, 0);
            AddWidget(addButton, row + addSelector_.RowCount - 1, addSelector_.ColumnCount);
            row += addSelector_.RowCount;

            AddWidget(selectedParametersView_, row, 0, 2, addSelector_.ColumnCount);
            AddWidget(removeButton, row, addSelector_.ColumnCount, verticalAlignment: VerticalAlignment.Top);
        }
    }
}
