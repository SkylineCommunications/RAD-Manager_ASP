namespace RadWidgets
{
	using System.Collections.Generic;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class ParameterLabelEditor : VisibilitySection
	{
		private readonly Numeric _parametersCountNumeric;
		private readonly List<TextBox> _parameterTextBoxes = new List<TextBox>();

		public ParameterLabelEditor(int minParameters, int maxParameters, int textBoxesColumnSpan = 1)
		{
			// Numeric input for number of parameters per subgroup
			const string parametersPerSubgroupTooltip = "For each subgroup you will be able to add this many subgroups";
			var parametersPerSubgroupLabel = new Label("Number of parameters per subgroup")
			{
				Tooltip = parametersPerSubgroupTooltip,
			};
			_parametersCountNumeric = new Numeric
			{
				Tooltip = parametersPerSubgroupTooltip,
				Minimum = minParameters,
				Maximum = maxParameters,
				Value = minParameters,
				StepSize = 1,

			};
			_parametersCountNumeric.Changed += (sender, args) => OnParametersCountNumericChanged();

			int row = 0;
			AddWidget(parametersPerSubgroupLabel, row, 0);
			AddWidget(_parametersCountNumeric, row, 1);
			row++;

			// Initialize with the current number of text boxes
			OnParametersCountNumericChanged();
		}

		public int MinParameters => (int)_parametersCountNumeric.Minimum;

		public int MaxParameters => (int)_parametersCountNumeric.Maximum;

		public int ParametersCount => (int)_parametersCountNumeric.Value;

		private void OnParametersCountNumericChanged()
		{
			int newCount = (int)_parametersCountNumeric.Value;
			if (newCount < _parameterTextBoxes.Count)
			{
				for (int i = newCount; i < _parameterTextBoxes.Count; ++i)
					RemoveWidget(_parameterTextBoxes[i]);
				_parameterTextBoxes.RemoveRange(newCount, _parameterTextBoxes.Count - newCount);
			}
			else if (newCount > _parameterTextBoxes.Count)
			{
				for (int i = _parameterTextBoxes.Count; i < newCount; ++i)
				{
					var textBox = new TextBox
					{
						Tooltip = $"The label of parameter {i + 1}. This label will be used when creating suggestion events for anomalies detected on this group.",
						PlaceHolder = $"Parameter {i + 1}",
					};
					AddWidget(textBox, i + 1, 1);
					_parameterTextBoxes.Add(textBox);
				}
			}
		}
	}
}
