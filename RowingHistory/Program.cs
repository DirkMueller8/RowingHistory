using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using ScottPlot;
using System.Xml.Linq;
using ScottPlot.Palettes;
using SkiaSharp;
using Microsoft.VisualBasic;
using ScottPlot.Colormaps;
using ScottPlot.TickGenerators.TimeUnits;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ScottPlot.TickGenerators.Financial;
using System.Collections;

namespace Rowing
{
    // Data structure to be used for the creation of a List<T> of input data. Contains:
    // Date: date of rowing in German notation
    // Time: average rowing time in mm:ss.f/500 m acc. to value on the monitor
    // Value: either distance (in m) or time (in min), e.g. 2.500 m or 30 min
    public class RowingData
    {
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public double Value { get; set; }

        public override string ToString()
        {
            // Output format of each row after parsing
            return $"{Date.ToString("dd.MM.yyyy")}, {Time.ToString(@"m\:ss\.f")}, {Value}";
        }
    }

    // Data structure for the creation of a List<T> used for output data. Contains
    // Date: date in German notation
    // Time: rowing power in Watt, as calculated by CalculatePower()
    // Value: either distance (in m) or time (in min)
    public class PowerRowingData
    {
        public DateTime Date { get; set; }
        public double Power { get; set; }
        public double Value { get; set; }

        public override string ToString()
        {
            return $"{Date:dd.MM.yyyy}, {Power}, {Value}";
        }
    }

    public class RowingParser
    {
        // List<T> as generic collection where T is a type parameter, in this case, the custom class RowingData
        // Each element in the list will be an object of type RowingData
        // The list can dynamically grow or shrink as elements are added or removed
        // distanceList row example: 25.01.2020, 2:12.6, 2500
        private List<RowingData> distanceList = new List<RowingData>();

        private List<RowingData> durationList = new List<RowingData>();
        private List<PowerRowingData> distancePowerList = new List<PowerRowingData>();
        private List<PowerRowingData> durationPowerList = new List<PowerRowingData>();

        // (\d{2}\.\d{2}\.\d{4}) -First capture group:
        // Matches a date in format DD.MM.YYYY
        // \d{2} matches exactly two digits
        // \. matches literal dots
        // \d{4} matches exactly four digits
        // , - Matches a literal comma separator
        // (\d{1,2}:\d{2}\.\d) - Second capture group
        // Matches time in format M:SS.T or MM:SS.T
        // \d{1,2} matches one or two digits
        // : matches literal colon
        // \d{2} matches exactly two digits
        // \.\d matches decimal point and one digit
        // , - Matches another literal comma separator
        // (\d+(?:\.\d +)?) - Third capture group
        // Matches a number (integer or decimal)
        // \d+ matches one or more digits
        // (?:\.\d +)? optionally matches decimal places
        // \s+ - Matches one or more whitespace characters
        // (min|m) - Fourth capture group
        // Matches either "min" or "m" as units
        private readonly Regex regex = new Regex(@"(\d{2}\.\d{2}\.\d{4}),(\d{1,2}:\d{2}\.\d),(\d+(?:\.\d+)?)\s+(min|m)");

        // Parsing of time for subsequent transformation to power
        private static TimeSpan ParseRowingTime(string timeStr)
        {
            // string[] parts = timeStr.Split(':'); splits a time string at the colon character into an array of substrings.
            // For example:
            // For an input time string like "2:30.5":
            // The Split method creates an array parts with two elements:
            // parts = "2"(minutes)
            // parts = "30.5"(seconds and tenths)
            string[] parts = timeStr.Split(':');
            string[] secondParts = parts[1].Split('.');

            // minutes and seconds are parsed from the parts array
            int minutes = int.Parse(parts[0]);
            int seconds = int.Parse(secondParts[0]);
            // split seconds are parsed
            int tenths = int.Parse(secondParts[1]);

            // The parsed data is placed in the TimeSpan instantiation
            return new TimeSpan(0, 0, minutes, seconds, tenths * 100);
        }

        // Conversion formula applied to each row of list where time occurs
        internal double CalculatePower(TimeSpan time, double distance)
        {
            double timeInSeconds = time.TotalSeconds;
            // Pace per 500m in seconds
            double pace = timeInSeconds / 500;
            // Concept2 formula
            double power = 2.80 / Math.Pow(pace, 3);
            return Math.Round(power, 0);
        }

        // This is needed to avoid accumulation of (duplicated) data
        public void ClearLists()
        {
            distanceList.Clear();
            durationList.Clear();
            distancePowerList.Clear();
            durationPowerList.Clear();
        }

        private void GeneratePowerLists()
        {
            // Generate distance power list
            foreach (var entry in distanceList)
            {
                distancePowerList.Add(new PowerRowingData
                {
                    Date = entry.Date,
                    // To avoid a comma symbol in the list item, as comma is used as separator
                    Power = Convert.ToInt16(CalculatePower(entry.Time, entry.Value)),
                    Value = entry.Value
                });
            }

            // Generate duration power list
            foreach (var entry in durationList)
            {
                durationPowerList.Add(new PowerRowingData
                {
                    Date = entry.Date,
                    Power = Convert.ToInt16(CalculatePower(entry.Time, entry.Value)),
                    Value = entry.Value
                });
            }
        }

        // Processes an array of input strings and returns a tuple containing two lists of rowing data. Here's what it does:
        // Takes an array of strings(inputData) as input, where each string represents a rowing record
        // For each entry in the input array:
        // Matches the string against a regex pattern to extract date, time, value, and unit
        // Creates a new RowingData object with parsed values
        // Adds the object to either distanceList(if unit is 'm') or durationList(if unit is 'min')
        // Returns a tuple containing:
        //   First item: List of distance measurements
        //   Second item: List of duration measurements
        public (List<RowingData>, List<RowingData>) ParseRowingData(string[] inputData)
        {
            foreach (var entry in inputData)
            {
                Match match = regex.Match(entry.Trim());
                if (match.Success)
                {
                    try
                    {
                        var data = new RowingData
                        {
                            Date = DateTime.ParseExact(match.Groups[1].Value,
                                                     "dd.MM.yyyy",
                                                     CultureInfo.InvariantCulture),
                            Time = ParseRowingTime(match.Groups[2].Value),
                            Value = double.Parse(match.Groups[3].Value,
                                               CultureInfo.InvariantCulture)
                        };

                        string unit = match.Groups[4].Value;
                        if (unit == "m")
                            distanceList.Add(data);
                        else if (unit == "min")
                            durationList.Add(data);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing line '{entry}': {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"Line did not match pattern: {entry}");
                }
            }

            return (distanceList, durationList);
        }

        public void SaveAllListsToFile(string projectDirectory)
        {
            GeneratePowerLists();
            Console.WriteLine($"Project folder in SaveAllLists: {projectDirectory}");

            string basePath = Path.Combine(projectDirectory, "Data");
            Console.WriteLine($"This was determined as the data folder of the project: {basePath}");

            var filePaths = new Dictionary<string, (List<object> list, string format)>
    {
        { Path.Combine(basePath, "distanceList.txt"), (new List<object>(distanceList), "regular") },
        { Path.Combine(basePath, "durationList.txt"), (new List<object>(durationList), "regular") },
        { Path.Combine(basePath, "distancePowerList.txt"), (new List<object>(distancePowerList), "power") },
        { Path.Combine(basePath, "durationPowerList.txt"), (new List<object>(durationPowerList), "power") }
    };

            // Ensure directory exists before writing files
            Directory.CreateDirectory(basePath);
            Console.WriteLine($"Created directory: {basePath}");

            try
            {
                foreach (var file in filePaths)
                {
                    Console.WriteLine($"Writing to file: {file.Key}");
                    using (StreamWriter writer = new StreamWriter(file.Key))
                    {
                        foreach (var data in file.Value.list)
                        {
                            writer.WriteLine(data.ToString());
                        }
                    }
                }
                Console.WriteLine("All lists saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving lists to file: {ex.Message}");
            }
        }

        // Reads and processes a text file containing rowing data, returning two lists of rowing records.Specifically, it:
        //   takes a file path as input parameter
        //   reads all lines from the specified file using File.ReadAllLines()
        //   passes the lines to ParseRowingData() for processing
        //   returns a tuple containing:
        //     first list: Distance measurements(where unit is 'm')
        //     second list: Duration measurements(where unit is 'min')
        public (List<RowingData>, List<RowingData>) ParseFromFile(string filePath)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                return ParseRowingData(lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
                return (new List<RowingData>(), new List<RowingData>());
            }
        }

        // Uses the Nuget library ScottPlot to create plots of training data
        public void CreatePowerPlot(string whichtype, string projectDirectory)
        {
            string plotsFolder = Path.Combine(projectDirectory, "Data\\plots");
            Console.WriteLine("Project directory path in SaveAllLists: " + projectDirectory);

            // Create the plot with default constructor
            var plt = new ScottPlot.Plot();

            // Create regression line only for these data clouds":
            if (whichtype == "4" || whichtype == "5")
            {
                // Convert data for plotting
                double[] dates = distancePowerList.Select(x => x.Date.ToOADate()).ToArray();
                double[] powers = distancePowerList.Select(x => x.Power).ToArray();

                // Add scatter plot with connected lines
                var scatter = plt.Add.Scatter(dates, powers);
                scatter.LineWidth = 0;
                scatter.MarkerSize = 10;

                // calculate the regression line
                ScottPlot.Statistics.LinearRegression reg = new(dates, powers);

                // plot the regression line
                Coordinates pt1 = new(dates.First(), reg.GetValue(dates.First()));
                Coordinates pt2 = new(dates.Last(), reg.GetValue(dates.Last()));
                var line = plt.Add.Line(pt1, pt2);
                line.MarkerSize = 0;
                line.LineWidth = 2;
                line.LinePattern = LinePattern.Dashed;
                // Configure datetime axis
                plt.Axes.DateTimeTicksBottom();

                // note the formula at the top of the plot
                plt.Title(reg.FormulaWithRSquared);
            }
            else
            {
                // Convert data for plotting
                double[] dates = distancePowerList.Select(x => x.Date.ToOADate()).ToArray();
                double[] powers = distancePowerList.Select(x => x.Power).ToArray();

                // Add scatter plot with connected lines
                var scatter = plt.Add.Scatter(dates, powers);
                scatter.LineWidth = 2;
                scatter.MarkerSize = 8;

                // Configure datetime axis
                plt.Axes.DateTimeTicksBottom();

                // Set labels
                plt.Title("Rowing Power Over Time for 2500 m Distance on Concept II");
                plt.XLabel("Date");
                plt.YLabel("Power (Watt)");
                plt.Axes.AutoScale();
            }
            // Save the plot
            string filename = whichtype switch
            {
                "1" => "power",
                "2" => "power_no_less_than_2500m",
                "3" => "power_no_less_than_2500m_no_1993",
                "4" => "power_no_less_than_2500m_regression_early",
                _ => "power_no_less_than_2500m_regression_late"
            };
            plt.SavePng(Path.Combine(plotsFolder, $"{filename}.png"), 744, 400);
        }
    }

    internal class Program
    {
        private static void Main()
        {
            bool continueInput = true;
            var parser = new RowingParser();
            string filePath;

            // To avoid absolute paths and preserve file structure independence on local machine:
            string projectDirectory = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)
                ?.Replace("bin\\Debug\\net8.0", "")
                ?? Directory.GetCurrentDirectory();
            Console.WriteLine("AppDomain: " + projectDirectory);
            // Define Data folder path
            string basePath = Path.Combine(projectDirectory, "Data");
            Console.WriteLine("Base path in Main(): " + basePath);

            while (continueInput)
            {
                Console.WriteLine("There are various input files, from which you select one: ");
                Console.WriteLine("1: original complete input");
                Console.WriteLine("2: input but omit all lines with less than 2500 m");
                Console.WriteLine("3: input but omit all lines with less than 2500 m, and omit data from 1993");
                Console.WriteLine("4: input but omit all lines with less than 2500 m, and omit data from 1993, regression early");
                Console.WriteLine("5: input but omit all lines with less than 2500 m, and omit data from 1993, regression late");
                Console.WriteLine("x: exit");
                string? selectFileSource = Console.ReadLine();

                if (selectFileSource?.ToLower() == "x")
                {
                    continueInput = false;
                    continue;
                }

                // User selects the source text file with the List content
                switch (selectFileSource)
                {
                    case "1":
                        filePath = basePath + "\\input_test.txt";
                        Console.WriteLine(filePath);
                        break;

                    case "2":
                        filePath = basePath + "\\input_no_less_than_2500m.txt";
                        Console.WriteLine(filePath);

                        break;

                    case "3":
                        filePath = basePath + "\\input_no_less_than_2500m_wo_1993.txt";
                        break;

                    case "4":
                        filePath = basePath + "\\input_no_less_than_2500m_regression_early.txt";
                        Console.WriteLine(filePath);

                        break;

                    case "5":
                        filePath = basePath + "\\input_no_less_than_2500m_regression_late.txt";
                        Console.WriteLine(filePath);

                        break;

                    default:
                        Console.WriteLine("Your selection was incorrect, try again");
                        continue;
                }

                // To avoi accumulation of more than one run
                parser.ClearLists();

                // Create a tuple of two lists
                (List<RowingData> distances, List<RowingData> durations) = parser.ParseFromFile(filePath);
                parser.SaveAllListsToFile(projectDirectory);

                Console.WriteLine("Distances (meters):");
                foreach (var d in distances)
                    Console.WriteLine($"{d}, m");

                Console.WriteLine("\nDurations (minutes):");
                foreach (var d in durations)
                    Console.WriteLine($"{d}, min");

                parser.CreatePowerPlot(selectFileSource, projectDirectory);

                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }
        }
    }
}