using Xunit;
using System;
using Rowing;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TestProject
{
    public class RowingParserTests
    {
        private readonly RowingParser _parser;

        public RowingParserTests()
        {
            _parser = new RowingParser();
        }

        [Theory]
        [InlineData("2:02.4", 500, 191)]
        [InlineData("2:22.4", 500, 121)]
        public void CalculatePower_ShouldMatchConceptIIValues(string timeStr, double distance, double expectedPower)
        {
            // Arrange
            var methodInfo = typeof(RowingParser).GetMethod("CalculatePower",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var time = TimeSpan.ParseExact(timeStr, @"m\:ss\.f", null);

            // Act
            var power = (double)methodInfo.Invoke(_parser, new object[] { time, distance });

            // Assert
            Assert.Equal(expectedPower, power, 1);
        }
    }
}