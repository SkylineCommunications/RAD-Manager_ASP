namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class GroupNameSection : VisibilitySection
	{
		private readonly TextBox _groupNameTextBox;
		private readonly List<string> _existingGroupNames;

		public GroupNameSection(string initialName, List<string> existingGroupNames, int textBoxColumnSpan)
		{
			_existingGroupNames = existingGroupNames;
			if (!string.IsNullOrEmpty(initialName)) // The current group name should be accepted as valid
				_existingGroupNames.Remove(initialName);

			var groupNameTooltip = "Provide the name of the group. This name will be used when creating suggestion events for anomalies detected on this group.";
			var groupNameLabel = new Label("Group name")
			{
				Tooltip = groupNameTooltip,
			};
			_groupNameTextBox = new TextBox()
			{
				Text = initialName,
				MinWidth = 600,
				Tooltip = groupNameTooltip,
			};
			_groupNameTextBox.Changed += (sender, args) => OnGroupNameTextBoxChanged();

			AddWidget(groupNameLabel, 0, 0);
			AddWidget(_groupNameTextBox, 0, 1, 1, textBoxColumnSpan);
		}

		public event EventHandler<EventArgs> ValidationChanged;

		public string GroupName => _groupNameTextBox.Text;

		public bool IsValid => _groupNameTextBox.ValidationState == UIValidationState.Valid;

		public string ValidationText => _groupNameTextBox.ValidationText;

		private void OnGroupNameTextBoxChanged()
		{
			if (string.IsNullOrEmpty(_groupNameTextBox.Text))
			{
				_groupNameTextBox.ValidationState = UIValidationState.Invalid;
				_groupNameTextBox.ValidationText = "Provide a group name";
			}
			else if (_existingGroupNames.Contains(_groupNameTextBox.Text))
			{
				_groupNameTextBox.ValidationState = UIValidationState.Invalid;
				_groupNameTextBox.ValidationText = "Group name already exists";
			}
			else
			{
				_groupNameTextBox.ValidationState = UIValidationState.Valid;
				_groupNameTextBox.ValidationText = string.Empty;
			}
		}
	}
}
