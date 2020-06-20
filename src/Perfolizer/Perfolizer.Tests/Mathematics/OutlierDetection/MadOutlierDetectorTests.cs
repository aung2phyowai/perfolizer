using System.Collections.Generic;
using JetBrains.Annotations;
using Perfolizer.Collections;
using Perfolizer.Mathematics.OutlierDetection;
using Perfolizer.Mathematics.QuantileEstimators;
using Perfolizer.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Perfolizer.Tests.Mathematics.OutlierDetection
{
    public class MadOutlierDetectorTests : OutlierDetectorTests
    {
        public MadOutlierDetectorTests([NotNull] ITestOutputHelper output) : base(output)
        {
        }

        protected override IOutlierDetector CreateOutlierDetector(IReadOnlyList<double> values) => MadOutlierDetector.Create(values,
            quantileEstimator: SimpleQuantileEstimator.Instance);

        private static readonly IDictionary<string, TestData> TestDataMap = new Dictionary<string, TestData>
        {
            {"Yang2", new TestData(YangDataSet.Table2, new double[] { })},
            {"Yang3", new TestData(YangDataSet.Table3, new double[] {1000})},
            {"Yang4", new TestData(YangDataSet.Table4, new double[] {500, 1000})},
            {"Yang5", new TestData(YangDataSet.Table5, new double[] {1000})},
            {"Yang6", new TestData(YangDataSet.Table6, new double[] {300, 500, 1000, 1500})}
        };

        [UsedImplicitly] public static TheoryData<string> TestDataKeys = TheoryDataHelper.Create(TestDataMap.Keys);

        [Theory]
        [MemberData(nameof(TestDataKeys))]
        public void MadOutlierDetectorTest([NotNull] string testDataKey) => Check(TestDataMap[testDataKey]);

        protected override void DumpDetails(TestData testData)
        {
            var quantileEstimator = HarrellDavisQuantileEstimator.Instance;
            var values = testData.Values.ToSorted();
            double median = quantileEstimator.GetMedian(values);
            double mad = MedianAbsoluteDeviation.Calc(values, quantileEstimator);
            
            Output.WriteLine($"Median = {median.ToString(TestCultureInfo.Instance)}");
            Output.WriteLine($"MAD    = {mad.ToString(TestCultureInfo.Instance)}");
        }
    }
}