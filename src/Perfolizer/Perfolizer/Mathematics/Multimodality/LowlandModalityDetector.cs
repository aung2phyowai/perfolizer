using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Perfolizer.Collections;
using Perfolizer.Common;
using Perfolizer.Mathematics.Histograms;

namespace Perfolizer.Mathematics.Multimodality
{
    public class LowlandModalityDetector : IModalityDetector
    {
        [NotNull] public static readonly LowlandModalityDetector Instance = new LowlandModalityDetector();

        private readonly double sensitivity;
        private readonly double precision;

        public LowlandModalityDetector(double sensitivity = 0.5, double precision = 0.01)
        {
            Assertion.InRangeInclusive(nameof(sensitivity), sensitivity, 0, 1);
            Assertion.InRangeExclusive(nameof(precision), precision, 0, 1);

            this.sensitivity = sensitivity;
            this.precision = precision;
        }

        public ModalityData DetectModes(IReadOnlyList<double> values, [CanBeNull] IReadOnlyList<double> weights = null) =>
            DetectModes(values.ToSorted(), weights, EmpiricalDensityHistogramBuilder.Instance);

        public ModalityData DetectModes(ISortedReadOnlyList<double> values, [CanBeNull] IReadOnlyList<double> weights,
            [CanBeNull] IDensityHistogramBuilder densityHistogramBuilder, bool diagnostics = false)
        {
            Assertion.NotNullOrEmpty(nameof(values), values);
            Assertion.MoreThan($"{nameof(values)}.Count", values.Count, 0);
            if (values.Max() - values.Min() < 1e-9)
                throw new ArgumentException($"{nameof(values)} should contain at least two different elements", nameof(values));

            densityHistogramBuilder ??= EmpiricalDensityHistogramBuilder.Instance;
            int binCount = (int) Math.Round(1 / precision);
            var histogram = densityHistogramBuilder.Build(values, weights, binCount);
            var bins = histogram.Bins;
            double binArea = 1.0 / bins.Count;
            var binHeights = bins.Select(bin => bin.Height).ToList();

            var diagnosticsBins = diagnostics
                ? histogram.Bins.Select(b => new LowlandModalityDiagnosticsData.DiagnosticsBin(b)).ToArray()
                : Array.Empty<LowlandModalityDiagnosticsData.DiagnosticsBin>();

            var peaks = new List<int>();
            for (int i = 1; i < binCount - 1; i++)
                if (binHeights[i] > binHeights[i - 1] && binHeights[i] >= binHeights[i + 1])
                {
                    peaks.Add(i);
                    if (diagnostics)
                        diagnosticsBins[i].IsPeak = true;
                }

            RangedMode GlobalMode(double location) => new RangedMode(location, histogram.GlobalLower, histogram.GlobalUpper, values);
            RangedMode LocalMode(double location, double left, double right)
            {
                var modeValues = new List<double>();
                foreach (double value in values)
                    if (left <= value && value <= right)
                        modeValues.Add(value);
                return new RangedMode(location, left, right, SortedReadOnlyDoubleList.Create(modeValues));
            }

            ModalityData Result(IReadOnlyList<RangedMode> modes) => diagnostics
                ? new LowlandModalityDiagnosticsData(modes, histogram, diagnosticsBins)
                : new ModalityData(modes, histogram);

            switch (peaks.Count)
            {
                case 0:
                    return Result(new[] {GlobalMode(bins[binHeights.WhichMax()].Middle)});
                case 1:
                    return Result(new[] {GlobalMode(bins[peaks.First()].Middle)});
                default:
                {
                    var modeLocations = new List<double>();
                    var cutPoints = new List<double>();

                    bool TrySplit(int peak0, int peak1, int peak2)
                    {
                        int left = peak1, right = peak2;
                        double height = Math.Min(binHeights[peak1], binHeights[peak2]);
                        while (left < right && binHeights[left] > height)
                            left++;
                        while (left < right && binHeights[right] > height)
                            right--;

                        if (diagnostics)
                        {
                            for (int i = left; i <= right; i++)
                                diagnosticsBins[i].WaterLevel = height;
                        }

                        double width = bins[right].Upper - bins[left].Lower;
                        double totalArea = width * height;
                        int totalBinCount = right - left + 1;
                        double totalBinArea = totalBinCount * binArea;
                        double binProportion = totalBinArea / totalArea;
                        if (binProportion < sensitivity)
                        {
                            modeLocations.Add(bins[peak0].Middle);
                            cutPoints.Add(bins[binHeights.WhichMin(peak1, peak2 - peak1)].Middle);

                            if (diagnostics)
                            {
                                diagnosticsBins[peak0].IsMode = true;
                                for (int i = left; i <= right; i++)
                                    diagnosticsBins[i].IsLowland = true;
                            }

                            return true;
                        }

                        return false;
                    }

                    var previousPeaks = new List<int> {peaks[0]};
                    for (int i = 1; i < peaks.Count; i++)
                    {
                        int currentPeak = peaks[i];

                        while (previousPeaks.Any() && binHeights[previousPeaks.Last()] < binHeights[currentPeak])
                            if (TrySplit(previousPeaks.First(), previousPeaks.Last(), currentPeak))
                                previousPeaks.Clear();
                            else
                                previousPeaks.RemoveAt(previousPeaks.Count - 1);

                        if (previousPeaks.Any() && binHeights[previousPeaks.Last()] > binHeights[currentPeak])
                            if (TrySplit(previousPeaks.First(), previousPeaks.Last(), currentPeak))
                                previousPeaks.Clear();

                        previousPeaks.Add(currentPeak);
                    }

                    modeLocations.Add(bins[previousPeaks.First()].Middle);
                    if (diagnostics)
                        diagnosticsBins[previousPeaks.First()].IsMode = true;

                    var modes = new List<RangedMode>();
                    switch (modeLocations.Count)
                    {
                        case 0:
                            modes.Add(GlobalMode(bins[binHeights.WhichMax()].Middle));
                            break;
                        case 1:
                            modes.Add(GlobalMode(modeLocations.First()));
                            break;
                        default:
                        {
                            modes.Add(LocalMode(modeLocations.First(), histogram.GlobalLower, cutPoints.First()));
                            for (int i = 1; i < modeLocations.Count - 1; i++)
                                modes.Add(LocalMode(modeLocations[i], cutPoints[i - 1], cutPoints[i]));
                            modes.Add(LocalMode(modeLocations.Last(), cutPoints.Last(), histogram.GlobalUpper));
                        }
                            break;
                    }

                    return Result(modes);
                }
            }
        }
    }
}