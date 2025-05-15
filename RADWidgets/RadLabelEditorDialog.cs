namespace RadWidgets
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class LabelEditor : Section
	{
		private readonly TextBox _textBox;

		public LabelEditor(int index, string parameterLabel = null)
		{
			string tooltip = $"The label of the parameter {index + 1}.";
			var label = new Label($"Parameter ")
			{
				Tooltip = "The label of the parameter.",
			};

			_textBox = new TextBox
			{
				Tooltip = tooltip,
				Text = parameterLabel ?? string.Empty,
				PlaceHolder = $"Parameter {index + 1}",
			};

			AddWidget(label, 0, 0);
			AddWidget(_textBox, 0, 1);
		}

		public string Label => _textBox.Text;
	}

	public class RadLabelEditorDialog : Dialog
	{
		private readonly List<LabelEditor> _labelEditors;

		public RadLabelEditorDialog(IEngine engine, List<string> labels) : base(engine)
		{
			Title = "Edit Parameter Labels";
			_labelEditors = labels.Select((l, i) => new LabelEditor(i, l)).ToList();

			var cancelButton = new Button("Cancel")
			{
				Tooltip = "Discard the changes to the labels above.",
			};
			cancelButton.Pressed += (sender, args) => Cancelled?.Invoke(this, EventArgs.Empty);

			var okButton = new Button("OK")
			{
				Style = ButtonStyle.CallToAction,
				Tooltip = "Set the labels as the parameter labels for the current group.",
			};
			okButton.Pressed += (sender, args) => Accepted?.Invoke(this, EventArgs.Empty);

			int row = 0;
			foreach (var editor in _labelEditors)
				AddSection(editor, row++, 0);

			AddWidget(cancelButton, row, 0);
			AddWidget(okButton, row, 1);
		}

		public event EventHandler Accepted;

		public event EventHandler Cancelled;

		public List<string> Labels
		{
			get
			{
				return _labelEditors.Select(e => e.Label).ToList();
			}
		}
	}
}
