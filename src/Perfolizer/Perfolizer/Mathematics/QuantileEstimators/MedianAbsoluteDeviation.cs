using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Perfolizer.Collections;

namespace Perfolizer.Mathematics.QuantileEstimators
{
    /// <summary>
    /// The median absolute deviation (MAD).
    /// MAD = median(abs(x[i] - median(x)))
    /// </summary>
    public static class MedianAbsoluteDeviation
    {
        /// <summary>
        /// Ratio between the standard deviation and the median absolute deviation for the normal distribution.
        /// It equals ≈1.482602218505602.
        /// The formula: (StandardDeviation) = 1.482602218505602 * (MedianAbsoluteDeviation).
        /// </summary>
        public const double DefaultConsistencyConstant = 1.482602218505602;

        public static double CalcMad([NotNull] ISortedReadOnlyList<double> values, double consistencyConstant = DefaultConsistencyConstant,
            [CanBeNull] IQuantileEstimator quantileEstimator = null)
        {
            quantileEstimator ??= SimpleQuantileEstimator.Instance;

            double median = quantileEstimator.GetMedian(values);
            var deviations = new double[values.Count];
            for (int i = 0; i < values.Count; i++)
                deviations[i] = Math.Abs(values[i] - median);
            return consistencyConstant * quantileEstimator.GetMedian(deviations);
        }

        public static double CalcMad([NotNull] IReadOnlyList<double> values, double consistencyConstant = DefaultConsistencyConstant,
            [CanBeNull] IQuantileEstimator quantileEstimator = null) =>
            CalcMad(values.ToSorted(), consistencyConstant, quantileEstimator);

        public static double CalcLowerMad([NotNull] ISortedReadOnlyList<double> values,
            double consistencyConstant = DefaultConsistencyConstant,
            [CanBeNull] IQuantileEstimator quantileEstimator = null)
        {
            quantileEstimator ??= SimpleQuantileEstimator.Instance;

            double median = quantileEstimator.GetMedian(values);
            var deviations = new List<double>(values.Count);
            for (int i = 0; i < values.Count; i++)
                if (values[i] <= median)
                    deviations.Add(Math.Abs(values[i] - median));
            return consistencyConstant * quantileEstimator.GetMedian(deviations);
        }

        public static double CalcLowerMad([NotNull] IReadOnlyList<double> values, double consistencyConstant = DefaultConsistencyConstant,
            [CanBeNull] IQuantileEstimator quantileEstimator = null) =>
            CalcLowerMad(values.ToSorted(), consistencyConstant, quantileEstimator);
        
        public static double CalcUpperMad([NotNull] ISortedReadOnlyList<double> values,
            double consistencyConstant = DefaultConsistencyConstant,
            [CanBeNull] IQuantileEstimator quantileEstimator = null)
        {
            quantileEstimator ??= SimpleQuantileEstimator.Instance;

            double median = quantileEstimator.GetMedian(values);
            var deviations = new List<double>(values.Count);
            for (int i = 0; i < values.Count; i++)
                if (values[i] >= median)
                    deviations.Add(Math.Abs(values[i] - median));
            return consistencyConstant * quantileEstimator.GetMedian(deviations);
        }

        public static double CalcUpperMad([NotNull] IReadOnlyList<double> values, double consistencyConstant = DefaultConsistencyConstant,
            [CanBeNull] IQuantileEstimator quantileEstimator = null) =>
            CalcUpperMad(values.ToSorted(), consistencyConstant, quantileEstimator);
    }
}