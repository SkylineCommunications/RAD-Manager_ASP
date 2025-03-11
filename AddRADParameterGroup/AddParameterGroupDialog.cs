using Skyline.DataMiner.Analytics.DataTypes;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Utils.InteractiveAutomationScript;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Remoting.Channels;

namespace AddParameterGroup
{
    public enum AddGroupType
    {
        [Description("Add single group")]
        Single,
        [Description("Add group for each element with given connector")]
        MultipleOnProtocol
    }

    public class AddParameterGroupDialog : Dialog
    {
        private EnumDropDown<AddGroupType> addTypeDropDown_;
        private Label groupNameLabel_;
        private TextBox groupNameTextBox_;
        private MultiParameterSelector parameterSelector_;
        private MultiParameterPerProtocolSelector parameterPerProtocolSelector_;
        private CheckBox updateModelCheckBox_;
        private CheckBox anomalyThresholdOverrideCheckBox_;
        private Numeric anomalyThresholdNumeric_;
        private CheckBox minimalDurationOverrideCheckBox_;
        private Time minimalDurationTime_;
        private Button okButton_;

        public AddGroupType AddType => addTypeDropDown_.Selected;
        public string GroupName => groupNameTextBox_.Text;
        public List<ParameterSelectorInfo> Parameters => parameterSelector_.SelectedItems;
        public string ProtocolName => parameterPerProtocolSelector_.ProtocolName;
        public string ProtocolVersion => parameterPerProtocolSelector_.ProtocolVersion;
        public List<ProtocolParameterSelectorInfo> ProtolParameters => parameterPerProtocolSelector_.SelectedParameters;
        public bool UpdateModel => updateModelCheckBox_.IsChecked;
        public double? AnomalyThreshold
        {
            get
            {
                if (anomalyThresholdOverrideCheckBox_.IsChecked)
                    return anomalyThresholdNumeric_.Value;
                else
                    return null;
            }
        }
        public int? MinimalDuration
        {
            get
            {
                if (minimalDurationOverrideCheckBox_.IsChecked)
                    return (int)minimalDurationTime_.TimeSpan.TotalMinutes;
                else
                    return null;
            }
        }
        public event EventHandler Accepted;
        public event EventHandler Cancelled;

        private void UpdateAddGroupIsEnabled()
        {
            bool parametersSelected;
            if (addTypeDropDown_.Selected == AddGroupType.Single)
                parametersSelected = parameterSelector_.SelectedItems.Count > 0;
            else
                parametersSelected = parameterPerProtocolSelector_.SelectedParameters.Count > 0;
            okButton_.IsEnabled = !string.IsNullOrEmpty(GroupName) && parametersSelected;
        }

        private void OnGroupNameTextBoxChanged()
        {
            groupNameTextBox_.ValidationState = string.IsNullOrEmpty(groupNameTextBox_.Text) ? UIValidationState.Invalid : UIValidationState.Valid;
            UpdateAddGroupIsEnabled();
        }

        public AddParameterGroupDialog(IEngine engine) : base(engine)
        {
            Title = "Add Parameter Group";

            var addTypeLabel = new Label("What to add?");
            addTypeDropDown_ = new EnumDropDown<AddGroupType>()
            {
                Selected = AddGroupType.Single
            };
            addTypeDropDown_.Changed += (sender, args) =>
            {
                if (args.Selected == AddGroupType.Single)
                {
                    parameterSelector_.IsVisible = true;
                    parameterPerProtocolSelector_.IsVisible = false;
                    groupNameLabel_.Text = "Group name";
                }
                else
                {
                    parameterSelector_.IsVisible = false;
                    parameterPerProtocolSelector_.IsVisible = true;
                    groupNameLabel_.Text = "Group name prefix";
                }
            };

            groupNameLabel_ = new Label("Group name");
            groupNameTextBox_ = new TextBox()
            {
                MinWidth = 600
            };
            groupNameTextBox_.ValidationState = UIValidationState.Invalid;
            groupNameTextBox_.Changed += (sender, args) => OnGroupNameTextBoxChanged();

            parameterSelector_ = new MultiParameterSelector(engine);
            parameterSelector_.Changed += (sender, args) => UpdateAddGroupIsEnabled();

            parameterPerProtocolSelector_ = new MultiParameterPerProtocolSelector(engine)
            {
                IsVisible = false
            };
            parameterPerProtocolSelector_.Changed += (sender, args) => UpdateAddGroupIsEnabled();

            updateModelCheckBox_ = new CheckBox("Update model on new data?");

            anomalyThresholdOverrideCheckBox_ = new CheckBox("Override default anomaly threshold?");
            anomalyThresholdOverrideCheckBox_.Changed += (sender, args) => anomalyThresholdNumeric_.IsEnabled = (sender as CheckBox).IsChecked;

            var anomalyThresholdLabel = new Label("Anomaly threshold");
            anomalyThresholdNumeric_ = new Numeric()
            {
                Minimum = 0,
                Value = 3,
                StepSize = 0.01,
                IsEnabled = false
            };

            minimalDurationOverrideCheckBox_ = new CheckBox("Override default minimal anomaly duration?");
            minimalDurationOverrideCheckBox_.Changed += (sender, args) => minimalDurationTime_.IsEnabled = (sender as CheckBox).IsChecked;

            var minimalDurationLabel = new Label("Minimal anomaly duration");
            minimalDurationTime_ = new Time()
            {
                HasSeconds = false,
                Minimum = TimeSpan.FromMinutes(5),
                TimeSpan = TimeSpan.FromMinutes(5),
                ClipValueToRange = true,
                IsEnabled = false,
            };

            okButton_ = new Button("Add group");
            okButton_.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

            var cancelButton = new Button("Cancel");
            cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

            OnGroupNameTextBoxChanged();

            int row = 0;
            AddWidget(addTypeLabel, row, 0);
            AddWidget(addTypeDropDown_, row, 1, 1, parameterSelector_.ColumnCount - 1);
            ++row;

            AddWidget(groupNameLabel_, row, 0);
            AddWidget(groupNameTextBox_, row, 1, 1, parameterSelector_.ColumnCount - 1);
            ++row;

            AddSection(parameterSelector_, row, 0);
            row += parameterSelector_.RowCount;

            AddSection(parameterPerProtocolSelector_, row, 0);
            row += parameterPerProtocolSelector_.RowCount;

            AddWidget(updateModelCheckBox_, row, 0, 1, parameterSelector_.ColumnCount);
            ++row;

            AddWidget(anomalyThresholdOverrideCheckBox_, row, 0, 1, parameterSelector_.ColumnCount);
            ++row;

            AddWidget(anomalyThresholdLabel, row, 0);
            AddWidget(anomalyThresholdNumeric_, row, 1, 1, parameterSelector_.ColumnCount - 1);
            ++row;

            AddWidget(minimalDurationOverrideCheckBox_, row, 0, 1, parameterSelector_.ColumnCount);
            ++row;

            AddWidget(minimalDurationLabel, row, 0);
            AddWidget(minimalDurationTime_, row, 1, 1, parameterSelector_.ColumnCount - 1);
            ++row;

            AddWidget(cancelButton, row, 0, 1, 1);
            AddWidget(okButton_, row, 1, 1, 3);
        }
    }
}
