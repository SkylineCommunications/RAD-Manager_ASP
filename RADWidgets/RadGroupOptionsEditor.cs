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
		private const int DefaultAnomalyThreshold_ = 3;
		private static readonly TimeSpan DefaultMinimalDuration_ = TimeSpan.FromMinutes(5);
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
				Tooltip = "Whether to continuously update the RAD model when new trend data is available. If not selected, the model will only be trained after " +
				"creation and when you manually specify a training range.",
			};

			anomalyThresholdOverrideCheckBox_ = new CheckBox("Override default anomaly threshold?")
			{
				IsChecked = options?.AnomalyThreshold != null,
				Tooltip = "Whether to override the default threshold for detecting anomalies. Anomalies are detected when the anomaly score exceeds this threshold. " +
				"With a high threshold less anomalies will be detected, with a low threshold more anomalies will be detected. If checked, the threshold can be set below.",
			};
			anomalyThresholdOverrideCheckBox_.Changed += (sender, args) => OnAnomalyThresholdOverrideCheckBoxChanged();

			string anomalyThresholdTooltip = "The threshold for detecting anomalies.";
			var anomalyThresholdLabel = new Label("Anomaly threshold")
			{
				Tooltip = anomalyThresholdTooltip,
			};
			anomalyThresholdNumeric_ = new Numeric()
			{
				Minimum = 0,
				Value = options?.AnomalyThreshold ?? DefaultAnomalyThreshold_,
				StepSize = 0.01,
				IsEnabled = options?.AnomalyThreshold != null,
				Tooltip = anomalyThresholdTooltip,
			};

			minimalDurationOverrideCheckBox_ = new CheckBox("Override default minimum anomaly duration?")
			{
				IsChecked = options?.MinimalDuration != null,
				Tooltip = "Whether to override the default duration an anomaly must last before a suggestion event is generated. Note that changing this duration will also have an " +
				"effect on the anomaly score. If checked, the duration can be set below.",
			};
			minimalDurationOverrideCheckBox_.Changed += (sender, args) => OnMinimalDurationOverrideCheckBoxChanged();

			string minimalDurationTooltip = "The minimum duration in minutes an anomaly must last before a suggestion event is generated.";
			var minimalDurationLabel = new Label("Minimum anomaly duration (in minutes)")
			{
				Tooltip = minimalDurationTooltip,
			};
			minimalDurationTime_ = new Time()
			{
				HasSeconds = false,
				Minimum = TimeSpan.FromMinutes(5),
				TimeSpan = options?.MinimalDuration != null ? TimeSpan.FromMinutes(options.MinimalDuration.Value) : DefaultMinimalDuration_,
				ClipValueToRange = true,
				IsEnabled = options?.MinimalDuration != null,
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

		private void OnAnomalyThresholdOverrideCheckBoxChanged()
		{
			anomalyThresholdNumeric_.IsEnabled = anomalyThresholdOverrideCheckBox_.IsChecked;
			if (!anomalyThresholdOverrideCheckBox_.IsChecked)
				anomalyThresholdNumeric_.Value = DefaultAnomalyThreshold_;
		}

		private void OnMinimalDurationOverrideCheckBoxChanged()
		{
			minimalDurationTime_.IsEnabled = minimalDurationOverrideCheckBox_.IsChecked;
			if (!minimalDurationOverrideCheckBox_.IsChecked)
				minimalDurationTime_.TimeSpan = DefaultMinimalDuration_;
		}
	}
}
