using System.Collections.Generic;
using Commons;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace OpenLibraryService.Tests.Normalization
{
    public class DateNormalizerTests
    {
        public static IEnumerable<ITestCaseData> NormalizeDatesTestCases()
        {
            var y2k = new Date(2001, 01, 01);
            yield return new TestCaseData("2001-01-01", new List<Date>{y2k})
                .SetName("2001-01-01");

            yield return new TestCaseData("01-01-2001", new List<Date> {y2k})
                .SetName("01-01-2001");
            
            yield return new TestCaseData("01/01/2001", new List<Date> {y2k})
                .SetName("01/01/2001");
            
            yield return new TestCaseData("01.01.2001", new List<Date> {y2k})
                .SetName("01.01.2001");
            
            yield return new TestCaseData("2001", new List<Date> {new (2001)})
                .SetName("2001");

            yield return new TestCaseData("2001-01", new List<Date> {y2k})
                .SetName("2001-01 is actually y2k");

            yield return new TestCaseData("21.2.1944", new List<Date> {new(1944, 2, 21)})
                .SetName("21.2.1944 is 1944-02-21");
        }

        [Test, TestCaseSource(nameof(NormalizeDatesTestCases))]
        public void NormalizeTests(string input, List<Date> expected)
        {
            var result = new DateNormalizer().Normalize(input);
            CollectionAssert.AreEquivalent(result, expected);
        }
        
        public static IEnumerable<ITestCaseData> DateWithPunctuationTestCases()
        {
            var y2k = new Date(2001, 01, 01);
            yield return new TestCaseData("2001?", new List<Date>{new(2001)})
                .SetName("2001? is 2001");
        }
        
        [Test, TestCaseSource(nameof(DateWithPunctuationTestCases))]
        public void DatesWithPunctuationNormalizationTests(string input, List<Date> expected)
        {
            var result = new DateNormalizer().Normalize(input);
            CollectionAssert.AreEquivalent(result, expected);
        }
        
    }
}