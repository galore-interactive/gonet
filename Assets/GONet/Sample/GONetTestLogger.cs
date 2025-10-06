using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace GONet.Sample
{
    /// <summary>
    /// Custom logger for test execution results.
    /// Writes test logs to Resources/Tests/Results/{testname}_{timestamp}.log
    /// </summary>
    public class GONetTestLogger : IDisposable
    {
        private StreamWriter writer;
        private string logFilePath;
        private StringBuilder buffer = new StringBuilder();

        public GONetTestLogger(string testName)
        {
            // Create log file in Resources/Tests/Results (outside of Resources processing)
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string safeTestName = SanitizeFileName(testName);
            string fileName = $"{safeTestName}_{timestamp}.log";

            // Get the Assets folder path (works in both Editor and builds)
            string projectPath = Application.dataPath;
            string resultsFolder = Path.Combine(projectPath, "GONet", "Sample", "Resources", "Tests", "Results");

            // Create directory if it doesn't exist
            Directory.CreateDirectory(resultsFolder);

            logFilePath = Path.Combine(resultsFolder, fileName);

            try
            {
                writer = new StreamWriter(logFilePath, false, Encoding.UTF8);
                writer.AutoFlush = true;

                // Write header
                WriteHeader(testName);

                GONetLog.Info($"[TestLogger] Created log file: {logFilePath}");
            }
            catch (Exception ex)
            {
                GONetLog.Error($"[TestLogger] Failed to create log file: {ex.Message}");
                writer = null;
            }
        }

        private void WriteHeader(string testName)
        {
            if (writer == null)
                return;

            writer.WriteLine("================================================================================");
            writer.WriteLine($"GONet Test Execution Log");
            writer.WriteLine($"Test: {testName}");
            writer.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Unity Version: {Application.unityVersion}");
            writer.WriteLine($"Platform: {Application.platform}");
            writer.WriteLine("================================================================================");
            writer.WriteLine();
        }

        public void Log(string message)
        {
            if (writer == null)
                return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logLine = $"[{timestamp}] {message}";

            writer.WriteLine(logLine);

            // Also log to GONet's log system
            GONetLog.Info($"[TEST] {message}");
        }

        public void LogStep(int stepIndex, int totalSteps, string stepType, string details = "")
        {
            string stepInfo = $"Step {stepIndex + 1}/{totalSteps}: {stepType}";
            if (!string.IsNullOrEmpty(details))
                stepInfo += $" - {details}";

            Log(stepInfo);
        }

        public void LogPass(string testName)
        {
            Log($"✓ PASS | {testName}");
        }

        public void LogFail(string testName, string reason)
        {
            Log($"❌ FAIL | {testName}");
            if (!string.IsNullOrEmpty(reason))
                Log($"  Reason: {reason}");
        }

        public void LogSummary(int passed, int failed)
        {
            writer.WriteLine();
            writer.WriteLine("================================================================================");
            writer.WriteLine("TEST SUMMARY");
            writer.WriteLine("================================================================================");
            writer.WriteLine($"Total Passed:  {passed}");
            writer.WriteLine($"Total Failed:  {failed}");
            writer.WriteLine($"Total Tests:   {passed + failed}");
            writer.WriteLine($"Success Rate:  {(passed + failed > 0 ? (passed * 100f / (passed + failed)) : 0f):F1}%");
            writer.WriteLine("================================================================================");
        }

        private string SanitizeFileName(string fileName)
        {
            // Remove invalid file name characters
            char[] invalids = Path.GetInvalidFileNameChars();
            string safe = fileName;

            foreach (char c in invalids)
            {
                safe = safe.Replace(c, '_');
            }

            // Replace spaces with underscores
            safe = safe.Replace(' ', '_');

            return safe;
        }

        public void Dispose()
        {
            if (writer != null)
            {
                writer.Flush();
                writer.Close();
                writer.Dispose();
                writer = null;

                GONetLog.Info($"[TestLogger] Closed log file: {logFilePath}");
            }
        }

        public string GetLogFilePath()
        {
            return logFilePath;
        }
    }
}
