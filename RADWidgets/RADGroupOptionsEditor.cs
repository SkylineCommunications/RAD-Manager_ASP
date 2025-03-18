namespace RADWidgets
{
	using System;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RADGroupOptions
	{
		public bool UpdateModel { get; set; }

		public double? AnomalyThreshold { get; set; }

		public int? MinimalDuration { get; set; }
	}

	public class RADGroupOptionsEditor : Section
	{
		private CheckBox updateModelCheckBox_;
		private CheckBox anomalyThresholdOverrideCheckBox_;
		private Numeric anomalyThresholdNumeric_;
		private CheckBox minimalDurationOverrideCheckBox_;
		private Time minimalDurationTime_;

		public RADGroupOptionsEditor()
		{
			updateModelCheckBox_ = new CheckBox("Update model on new data?");

			anomalyThresholdOverrideCheckBox_ = new CheckBox("Override default anomaly threshold?");
			anomalyThresholdOverrideCheckBox_.Changed += (sender, args) => anomalyThresholdNumeric_.IsEnabled = (sender as CheckBox).IsChecked;

			var anomalyThresholdLabel = new Label("Anomaly threshold");
			anomalyThresholdNumeric_ = new Numeric()
			{
				Minimum = 0,
				Value = 3,
				StepSize = 0.01,
				IsEnabled = false,
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
		}

		public RADGroupOptions Options
		{
			get
			{
				return new RADGroupOptions
				{
					UpdateModel = UpdateModel,
					AnomalyThreshold = AnomalyThreshold,
					MinimalDuration = MinimalDuration,
				};
			}
		}

		private bool UpdateModel => updateModelCheckBox_.IsChecked;

		private double? AnomalyThreshold
		{
			get
			{
				if (anomalyThresholdOverrideCheckBox_.IsChecked)
					return anomalyThresholdNumeric_.Value;
				else
					return null;
			}
		}

		private int? MinimalDuration
		{
			get
			{
				if (minimalDurationOverrideCheckBox_.IsChecked)
					return (int)minimalDurationTime_.TimeSpan.TotalMinutes;
				else
					return null;
			}
		}
	}
}
