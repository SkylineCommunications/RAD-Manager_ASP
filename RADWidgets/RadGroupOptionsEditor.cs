namespace RadWidgets
{
	using System;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class RadGroupOptions
	{
		public bool UpdateModel { get; set; }

		public double? AnomalyThreshold { get; set; }

		public int? MinimalDuration { get; set; }
	}

	/// <summary>
	/// Editor for RAD group options.
	/// </summary>
	public class RadGroupOptionsEditor : Section
	{
		private readonly CheckBox updateModelCheckBox_;
		private readonly CheckBox anomalyThresholdOverrideCheckBox_;
		private readonly Numeric anomalyThresholdNumeric_;
		private readonly CheckBox minimalDurationOverrideCheckBox_;
		private readonly Time minimalDurationTime_;

		/// <summary>
		/// Initializes a new instance of the <see cref="RadGroupOptionsEditor"/> class.
		/// </summary>
		/// <param name="columnCount">The number of columns the section should take (should be 2 or greater).</param>
		/// <param name="options">The initial settings to display (if any).</param>
		public RadGroupOptionsEditor(int columnCount, RadGroupOptions options = null)
		{
			updateModelCheckBox_ = new CheckBox("Update model on new data?")
			{
				IsChecked = options?.UpdateModel ?? false,
			};

			anomalyThresholdOverrideCheckBox_ = new CheckBox("Override default anomaly threshold?")
			{
				IsChecked = options?.AnomalyThreshold != null,
			};
			anomalyThresholdOverrideCheckBox_.Changed += (sender, args) => anomalyThresholdNumeric_.IsEnabled = (sender as CheckBox).IsChecked;

			var anomalyThresholdLabel = new Label("Anomaly threshold");
			anomalyThresholdNumeric_ = new Numeric()
			{
				Minimum = 0,
				Value = options?.AnomalyThreshold ?? 3,
				StepSize = 0.01,
				IsEnabled = false,
			};

			minimalDurationOverrideCheckBox_ = new CheckBox("Override default minimal anomaly duration?")
			{
				IsChecked = options?.MinimalDuration != null,
			};
			minimalDurationOverrideCheckBox_.Changed += (sender, args) => minimalDurationTime_.IsEnabled = (sender as CheckBox).IsChecked;

			var minimalDurationLabel = new Label("Minimal anomaly duration");
			minimalDurationTime_ = new Time()
			{
				HasSeconds = false,
				Minimum = TimeSpan.FromMinutes(5),
				TimeSpan = TimeSpan.FromMinutes(options?.MinimalDuration ?? 5),
				ClipValueToRange = true,
				IsEnabled = false,
			};

			int row = 0;
			AddWidget(updateModelCheckBox_, row, 0, 1, columnCount);
			++row;

			AddWidget(anomalyThresholdOverrideCheckBox_, row, 0, 1, columnCount);
			++row;

			AddWidget(anomalyThresholdLabel, row, 0);
			AddWidget(anomalyThresholdNumeric_, row, 1, 1, columnCount - 1);
			++row;

			AddWidget(minimalDurationOverrideCheckBox_, row, 0, 1, columnCount);
			++row;

			AddWidget(minimalDurationLabel, row, 0);
			AddWidget(minimalDurationTime_, row, 1, 1, columnCount - 1);
		}

		public RadGroupOptions Options
		{
			get
			{
				return new RadGroupOptions
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
