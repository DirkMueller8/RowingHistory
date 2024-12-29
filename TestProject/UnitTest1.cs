using Xunit;
using System;
using System.IO;
using System.Collections.Generic;
using Rowing;

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

        [Fact]
        public void SaveAllListsToFile_ShouldCreateDistanceListCorrectly()
        {
            // Arrange
            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            string inputFilePath = Path.Combine(tempDirectory, "input_test.txt");
            string[] inputLines = new[]
            {
                "25.01.2020,2:12.6,2500 m",
                "26.01.2020,2:15.0,2500 m"
            };
            File.WriteAllLines(inputFilePath, inputLines);

            // Act
            _parser.ClearLists();
            _parser.ParseFromFile(inputFilePath);
            _parser.SaveAllListsToFile(tempDirectory);

            // Assert
            string distanceListPath = Path.Combine(tempDirectory, "Data", "distanceList.txt");
            Assert.True(File.Exists(distanceListPath), "distanceList.txt should be created.");

            string[] expectedLines = new[]
            {
                "25.01.2020, 2:12.6, 2500",
                "26.01.2020, 2:15.0, 2500"
            };
            string[] actualLines = File.ReadAllLines(distanceListPath);
            Assert.Equal(expectedLines, actualLines);

            // Cleanup
            Directory.Delete(tempDirectory, true);
        }
    }
}