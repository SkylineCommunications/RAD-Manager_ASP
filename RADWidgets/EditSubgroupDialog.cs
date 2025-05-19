namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using RadUtils;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class EditSubgroupDialog : Dialog
	{
		private readonly RadSubgroupEditor _subgroupEditor;
		private readonly Button _okButton;
		private readonly Button _cancelButton;

		public EditSubgroupDialog(IEngine engine, List<string> existingSubgroups, RadSubgroupSettings settings, RadGroupOptions parentOptions) : base(engine)
		{
			Title = string.IsNullOrEmpty(settings?.Name) ? "Edit subgroup" : $"Edit subgroup '{settings.Name}'";
			_subgroupEditor = new RadSubgroupEditor(engine, existingSubgroups, settings, parentOptions);

			_okButton = new Button("Apply")
			{
				Style = ButtonStyle.CallToAction,
				Tooltip = "Apply the specified settings to the subgroup.",
			};
			_okButton.Pressed += (s, e) => Accepted?.Invoke(this, EventArgs.Empty);

			_cancelButton = new Button("Cancel")
			{
				Tooltip = "Do not add the subgroup specified above.",
			};
			_cancelButton.Pressed += (s, e) => Cancelled?.Invoke(this, EventArgs.Empty);

			int row = 0;
			AddSection(_subgroupEditor, 0, 0);
			row += _subgroupEditor.RowCount;

			AddWidget(_cancelButton, row, 0);
			AddWidget(_okButton, row, 1, 1, _subgroupEditor.ColumnCount - 1);
			//TODO: check for duplicate parameters, duplicate names, that all parameters are filled in and so on
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public RadSubgroupSettings Settings
		{
			get
			{
				return _subgroupEditor.Settings;
			}
		}
	}
}
