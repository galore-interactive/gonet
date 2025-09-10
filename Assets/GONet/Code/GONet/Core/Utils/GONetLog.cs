/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 * 
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using GONet.Utils;

namespace GONet
{
    /// <summary>
    /// A high-performance logging utility that can/should replace <see cref="UnityEngine.Debug"/> LogXxx methods usage.
    /// Advantages:
    ///     1. Log statements will all have a nice/complete date/time stamp (that includes millisecond and uses GONet's own <see cref="HighResolutionTimeUtils"/>)
    ///     2. Log statements will go to the console as usual as well as a gonet.log file in /logs folder
    ///     3. gonet.log file history will be maintained with automatic file rotation and cleanup
    ///     4. Thread-safe operation for multi-threaded environments
    ///     5. You can subscribe to logging events
    ///     6. You can conditionally exclude calls to any/all logging level methods using #DEFINE (i.e., remove everything from a production release/build if you like)
    ///     7. Memory-efficient with minimal allocations and background async file writing
    /// </summary>
    public static class GONetLog
    {
        #region Constants and Enums

        // These keys are used to figure out which Log method was used when logging
        // because the observer of unity's logging reports unity log levels...not necessarily the 
        // log levels we will print out
        private const string KeyInfo = "Log:Info";
        private const string KeyDebug = "Log:Debug";
        private const string KeyWarning = "Log:Warning";
        private const string KeyError = "Log:Error";
        private const string KeyFatal = "Log:Fatal";
        private const string KeyVerbose = "Log:Verbose";

        private const char NewLine = '\n';
        private const string SPACE = " ";

        private enum LogLevel
        {
            Verbose,
            Debug,
            Info,
            Warning,
            Error,
            Fatal
        }

        #endregion

        #region Configuration

        // Log configuration (can be exposed as public properties if you want to make them configurable)
        private static readonly string LogDirectory;
        private static readonly int MaxLogFileDays = 5;
        private static readonly string LogFilePrefix = "gonet";
        private static readonly string LogFileExtension = ".log";
        private static readonly int MaxQueueSize = 10000;
        private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(1);
        private static readonly System.Threading.ThreadPriority BackgroundThreadPriority = System.Threading.ThreadPriority.BelowNormal;

        #endregion

        #region Fields

        private static string _currentLogFile;
        private static string _lastLog;
        private static readonly Thread _loggerThread;
        private static readonly ConcurrentQueue<LogMessage> _logQueue = new ConcurrentQueue<LogMessage>();
        private static readonly AutoResetEvent _queueEvent = new AutoResetEvent(false);
        private static volatile bool _shutdownRequested = false;
        private static int _isShuttingDown = 0; // For thread-safe shutdown detection
        private static int _queuedItemsCount = 0;
        private static DateTime _lastFlushTime = DateTime.Now;
        private static readonly object _flushLock = new object();
        private static readonly ConcurrentDictionary<Thread, StringBuilder> _appendStringBuilderByThreadMap = new ConcurrentDictionary<Thread, StringBuilder>();
        private static bool _initialized;

        // Adding FileStreamWriter for more efficient file writes
        private static FileStream _fileStream;
        private static StreamWriter _streamWriter;

        // Platform detection
        private static readonly bool IsWebGL =
#if UNITY_WEBGL
            true;
#else
            false;
#endif

        #endregion

        #region Events and Properties

        public static string LastLog => _lastLog;

        public delegate void LogDelegate(string logStr);
        public static event LogDelegate OnLog;

        #endregion

        #region Constructor/Initialization

        static GONetLog()
        {
            if (_initialized) return; // domain reload safety

            try
            {
                // Set up log directory
                string basePath;

                // Choose appropriate base path based on platform
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    // WebGL has limited file system access
                    basePath = Application.temporaryCachePath;
                }
                else if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    // iOS has strict sandbox rules
                    basePath = Application.persistentDataPath;
                }
                else
                {
                    // Default for most platforms
                    basePath = Application.persistentDataPath;
                }

                LogDirectory = Path.Combine(basePath, "logs");

                try
                {
                    Directory.CreateDirectory(LogDirectory);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"GONetLog: Failed to create log directory: {ex.Message}");
                    // Fall back to Application.temporaryCachePath if first attempt fails
                    LogDirectory = Path.Combine(Application.temporaryCachePath, "logs");
                    Directory.CreateDirectory(LogDirectory);
                }

                // Clean up old log files
                CleanupOldLogFiles();

                // Create today's log file path
                _currentLogFile = GetLogFilePath(DateTime.Now);

                // Initialize the file stream
                InitializeFileStream();

                // Start the background thread for log processing
                // Skip for WebGL which doesn't support threading
                if (!IsWebGL)
                {
                    try
                    {
                        _loggerThread = new Thread(ProcessLogQueue)
                        {
                            IsBackground = true,
                            Name = "GONet Logger Thread"
                        };

                        // Set thread priority in a try/catch (some platforms may restrict this)
                        try
                        {
                            _loggerThread.Priority = BackgroundThreadPriority;
                        }
                        catch (Exception)
                        {
                            // Ignore priority setting failures - not critical
                        }

                        _loggerThread.Start();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"GONetLog: Failed to start logger thread: {ex.Message}");

                        // For platforms that might have threading issues, continue without the thread
                        // This will make logging synchronous but still functional
                        UnityEngine.Debug.LogWarning("GONetLog: Continuing without background processing thread. Logging will be synchronous.");
                    }
                }
                else
                {
                    // WebGL special case - logging will be synchronous
                    UnityEngine.Debug.Log("GONetLog: Running on WebGL, using synchronous logging (no background thread)");
                }

                // Register for application quit to ensure logs are flushed
                Application.quitting += OnApplicationQuitting;

                _initialized = true; // Mark as initialized only if we get here successfully
                Info("GONetLog initialized");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"GONetLog: Initialization failed: {ex.Message}\n{ex.StackTrace}");
                _initialized = false; // Ensure we're marked as not initialized if anything fails
            }
        }

        private static void InitializeFileStream()
        {
            try
            {
                // Close existing stream if needed
                CloseFileStream();

                // Create directory if it doesn't exist (important for all platforms)
                string directory = Path.GetDirectoryName(_currentLogFile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Open with FileShare.ReadWrite to allow other processes to read the log while we write
                _fileStream = new FileStream(_currentLogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                _streamWriter = new StreamWriter(_fileStream, Encoding.UTF8)
                {
                    AutoFlush = false // We'll manage flushing for better performance
                };
            }
            catch (Exception ex)
            {
                // Fall back to Unity logging if file access fails
                UnityEngine.Debug.LogError($"GONetLog: Failed to initialize log file: {ex.Message}");
                // Try to log more details about the exception for troubleshooting
                if (ex.InnerException != null)
                {
                    UnityEngine.Debug.LogError($"GONetLog: Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        private static void CloseFileStream()
        {
            if (_streamWriter != null)
            {
                try
                {
                    _streamWriter.Flush();
                    _streamWriter.Close();
                    _streamWriter = null;
                }
                catch (Exception) { /* Ignore exceptions during cleanup */ }
            }

            if (_fileStream != null)
            {
                try
                {
                    _fileStream.Close();
                    _fileStream = null;
                }
                catch (Exception) { /* Ignore exceptions during cleanup */ }
            }
        }

        private static void OnApplicationQuitting()
        {
            try
            {
                // Prevent multiple shutdown attempts
                if (Interlocked.Exchange(ref _isShuttingDown, 1) != 0)
                {
                    return; // Already shutting down
                }

                UnityEngine.Debug.Log("GONetLog: Beginning shutdown sequence");

                // Signal the background thread to finish processing
                _shutdownRequested = true;

                try
                {
                    // Signal event might throw on some platforms during shutdown
                    _queueEvent.Set();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"GONetLog: Error signaling queue event during shutdown: {ex.Message}");
                }

                // Give the thread a chance to complete - try a more gentle approach
                if (_loggerThread != null && _loggerThread.IsAlive)
                {
                    try
                    {
                        // Wait without Join first - can be safer on some platforms
                        for (int i = 0; i < 10; i++) // Try for up to 1 second
                        {
                            if (!_loggerThread.IsAlive)
                            {
                                break;
                            }

                            Thread.Sleep(100);
                        }

                        // If thread is still alive, try a formal Join
                        if (_loggerThread.IsAlive)
                        {
                            bool joined = _loggerThread.Join(1000); // Wait up to 1 second more
                            if (!joined)
                            {
                                UnityEngine.Debug.LogWarning("GONetLog: Logger thread did not complete in time during shutdown");

                                // Don't abort the thread - that's what's causing the error
                                // Just continue with shutdown and let OS cleanup the thread
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"GONetLog: Error joining logger thread during shutdown: {ex.Message}");
                    }
                }

                // Process any remaining messages directly
                try
                {
                    DequeueAndWriteMessages();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"GONetLog: Error processing remaining messages during shutdown: {ex.Message}");
                }

                // Ensure everything is written to disk
                CloseFileStream();

                UnityEngine.Debug.Log("GONetLog: Shutdown complete");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"GONetLog: Error during shutdown: {ex.Message}");
            }
        }

        #endregion

        #region Append Methods

        public static void Append(string message)
        {
            Append(message, false);
        }

        public static void AppendLine(string message)
        {
            Append(message, true);
        }

        private static void Append(string message, bool doesIncludeEOL)
        {
            if (!_appendStringBuilderByThreadMap.TryGetValue(Thread.CurrentThread, out StringBuilder stringBuilder))
            {
                _appendStringBuilderByThreadMap[Thread.CurrentThread] = stringBuilder = new StringBuilder(5000);
            }

            stringBuilder.Append(message);
            if (doesIncludeEOL)
            {
                stringBuilder.Append(Environment.NewLine);
            }
        }

        public static bool Append_FlushVerbose(string message = null)
        {
            return Append_Flush(LogLevel.Verbose, message);
        }

        public static bool Append_FlushDebug(string message = null)
        {
            return Append_Flush(LogLevel.Debug, message);
        }

        public static bool Append_FlushInfo(string message = null)
        {
            return Append_Flush(LogLevel.Info, message);
        }

        public static bool Append_FlushWarning(string message = null)
        {
            return Append_Flush(LogLevel.Warning, message);
        }

        public static bool Append_FlushError(string message = null)
        {
            return Append_Flush(LogLevel.Error, message);
        }

        public static bool Append_FlushFatal(string message = null)
        {
            return Append_Flush(LogLevel.Fatal, message);
        }

        private static bool Append_Flush(LogLevel logLevel, string message)
        {
            if (message != null)
            {
                Append(message, false);
            }

            if (_appendStringBuilderByThreadMap.TryGetValue(Thread.CurrentThread, out StringBuilder stringBuilder))
            {
                if (stringBuilder.Length > 0)
                {
                    string text = stringBuilder.ToString();
                    switch (logLevel)
                    {
                        case LogLevel.Verbose:
                            Verbose(text);
                            break;
                        case LogLevel.Debug:
                            Debug(text);
                            break;
                        case LogLevel.Info:
                            Info(text);
                            break;
                        case LogLevel.Warning:
                            Warning(text);
                            break;
                        case LogLevel.Error:
                            Error(text);
                            break;
                        case LogLevel.Fatal:
                            Fatal(text);
                            break;
                    }
                    stringBuilder.Clear();
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Log Methods

        [Conditional("LOG_INFO")]
        public static void Info(string message)
        {
            LogInternal(message, KeyInfo, LogType.Log, LogLevel.Info);
        }

        [Conditional("LOG_DEBUG")]
        public static void Debug(string message)
        {
            LogInternal(message, KeyDebug, LogType.Log, LogLevel.Debug);
        }

        [Conditional("LOG_WARNING")]
        public static void Warning(string message)
        {
            LogInternal(message, KeyWarning, LogType.Warning, LogLevel.Warning);
        }

        [Conditional("LOG_ERROR")]
        public static void Error(string message)
        {
            LogInternal(message, KeyError, LogType.Error, LogLevel.Error);
        }

        [Conditional("LOG_FATAL")]
        public static void Fatal(string message)
        {
            LogInternal(message, KeyFatal, LogType.Error, LogLevel.Fatal);
        }

        [Conditional("LOG_VERBOSE")]
        public static void Verbose(string message)
        {
            LogInternal(message, KeyVerbose, LogType.Log, LogLevel.Verbose);
        }

        #endregion

        #region Internal Implementation

        private static void LogInternal(string message, string keyXxx, LogType logType, LogLevel logLevel)
        {
            if (GONetMain.IsUnityApplicationEditor)
            {
                string formattedMessage = FormatMessage(keyXxx, message);
                switch (logType)
                {
                    case LogType.Assert:
                    case LogType.Log:
                        UnityEngine.Debug.Log(formattedMessage);
                        break;

                    case LogType.Warning:
                        UnityEngine.Debug.LogWarning(formattedMessage);
                        break;

                    case LogType.Exception:
                    case LogType.Error:
                        UnityEngine.Debug.LogError(formattedMessage);
                        break;
                }
            }
            else
            {
                StackTrace trace = new StackTrace(1, true);
                const string FRAME_PRE = "(frame:";
                const string FRAME_POST = "s) ";
                string logString = string.Concat(FRAME_PRE, GONetMain.Time.FrameCount, '/', GONetMain.Time.ElapsedSeconds, FRAME_POST, keyXxx, SPACE, message);
                ProcessMessageViaLogger(logString, trace.ToString(), logType, logLevel);
            }
        }

        private static string FormatMessage(string level, string message)
        {
            const string FORMAT = "[{0}]{5}{6} (Thread:{1}) ({2:dd MMM yyyy H:mm:ss.fff}) (frame:{7}/{3}s) {4}";
            const string CLIENT = "[Client]";
            const string SERVER = "[Server]";
            return string.Format(FORMAT, level, Thread.CurrentThread.ManagedThreadId, DateTime.Now, GONetMain.Time?.ElapsedSeconds, message,
                GONetMain.IsServer ? SERVER : string.Empty,
                GONetMain.IsClient ? CLIENT : string.Empty,
                GONetMain.Time?.FrameCount);
        }

        private static void ProcessMessageViaLogger(string logString, string stackTrace, LogType type, LogLevel logLevel)
        {
            // Create the full log message with or without stack trace
            string fullMessage;
            if (string.IsNullOrEmpty(stackTrace))
            {
                fullMessage = logString;
            }
            else
            {
                fullMessage = string.Concat(logString, NewLine, stackTrace, NewLine);
            }

            // Update LastLog for subscribers
            _lastLog = logString;

            // Notify subscribers synchronously (on this thread)
            OnLog?.Invoke(_lastLog);

            // Create log message
            var logMessage = new LogMessage
            {
                Timestamp = DateTime.Now,
                Message = fullMessage,
                Level = logLevel,
                ThreadId = Thread.CurrentThread.ManagedThreadId
            };

            // For WebGL or if thread not running, process synchronously
            if (IsWebGL || _loggerThread == null || !_loggerThread.IsAlive)
            {
                // Handle WebGL or thread-less case with direct synchronous logging
                try
                {
                    string expectedLogFile = GetLogFilePath(DateTime.Now);
                    if (_currentLogFile != expectedLogFile)
                    {
                        // Today changed, rotate log file
                        _currentLogFile = expectedLogFile;
                        InitializeFileStream();
                        CleanupOldLogFiles();
                    }

                    // Format and write the log directly
                    string formattedMessage = string.Format("[{0}] (Thread:{1}) {2:yyyy-MM-dd HH:mm:ss.fff} {3}{4}",
                        logMessage.Level.ToString().ToUpper(),
                        logMessage.ThreadId,
                        logMessage.Timestamp,
                        logMessage.Message,
                        Environment.NewLine);

                    WriteToFile(formattedMessage);
                    ForceFlush(false);
                }
                catch (Exception ex)
                {
                    // If direct writing fails, fall back to Unity console
                    UnityEngine.Debug.LogError($"GONetLog: Failed to write log synchronously: {ex.Message}");
                }
            }
            else
            {
                // Normal async case - queue message for background writing
                if (Interlocked.Increment(ref _queuedItemsCount) < MaxQueueSize) // Check queue size
                {
                    _logQueue.Enqueue(logMessage);

                    try
                    {
                        // Signal the background thread that a message is available
                        _queueEvent.Set();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"GONetLog: Failed to signal background thread: {ex.Message}");
                    }
                }
                else
                {
                    // Queue full, write to Unity console as a fallback
                    UnityEngine.Debug.LogWarning($"GONetLog: Log queue full, message dropped: {logString}");
                    Interlocked.Decrement(ref _queuedItemsCount);
                }
            }
        }

        private static void ProcessLogQueue()
        {
#if UNITY_WEBGL
            // WebGL doesn't support threading - this method won't be called
            // But we need to keep it for cross-platform compilation
            return;
#else
            try
            {
                int errorCount = 0;
                const int maxErrors = 5; // Limit consecutive errors before backing off

                while (!_shutdownRequested)
                {
                    try
                    {
                        // Wait for a message or timeout (to check for rotation)
                        _queueEvent.WaitOne(1000);

                        // Check if we've been asked to shut down during the wait
                        if (_shutdownRequested)
                        {
                            break;
                        }

                        // Check for log file rotation
                        string expectedLogFile = GetLogFilePath(DateTime.Now);
                        if (_currentLogFile != expectedLogFile)
                        {
                            // Today changed, rotate log file
                            _currentLogFile = expectedLogFile;
                            InitializeFileStream();
                            CleanupOldLogFiles();
                        }

                        // Process all pending log messages
                        if (DequeueAndWriteMessages())
                        {
                            // If dequeued any messages, force a flush
                            ForceFlush(true);
                        }
                        else if ((DateTime.Now - _lastFlushTime) > FlushInterval)
                        {
                            // Periodic flush even if no new messages
                            ForceFlush(false);
                        }

                        // Reset error count on successful loop
                        errorCount = 0;
                    }
                    catch (ThreadAbortException)
                    {
                        // Thread is being aborted (application quitting or domain unloading)
                        // Just break out of the loop, no need to log an error
                        break;
                    }
                    catch (Exception loopEx)
                    {
                        // Skip logging if we're shutting down
                        if (_shutdownRequested)
                        {
                            break;
                        }

                        // Count consecutive errors
                        errorCount++;

                        // Log to Unity console as fallback, but only if not a thread abort
                        if (!(loopEx is ThreadAbortException))
                        {
                            UnityEngine.Debug.LogError($"GONetLog: Error in log processing loop: {loopEx.Message}");
                        }

                        // If too many consecutive errors, back off
                        if (errorCount >= maxErrors)
                        {
                            UnityEngine.Debug.LogError($"GONetLog: Too many consecutive errors, backing off for 10 seconds");
                            try
                            {
                                Thread.Sleep(10000); // Back off for 10 seconds
                                InitializeFileStream(); // Try to reinitialize after the wait
                            }
                            catch
                            {
                                // Ignore sleep errors
                            }
                        }
                    }
                }

                // Handle final cleanup when exiting the loop normally
                try
                {
                    DequeueAndWriteMessages();
                    ForceFlush(true);
                    UnityEngine.Debug.Log("GONetLog: Logger thread shutting down normally");
                }
                catch (Exception)
                {
                    // Ignore errors during final cleanup
                }
            }
            catch (ThreadAbortException)
            {
                // Main try/catch - special handler for thread abort
                try
                {
                    // Try to do final processing but don't log errors
                    DequeueAndWriteMessages();
                    ForceFlush(true);
                }
                catch
                {
                    // Truly ignore everything during thread abort
                }
            }
            catch (Exception ex)
            {
                // Only log if not a thread abort
                if (!(ex is ThreadAbortException))
                {
                    // Log to Unity console as fallback
                    UnityEngine.Debug.LogError($"GONetLog: Fatal error in log processing thread: {ex.Message}\n{ex.StackTrace}");
                }
            }
#endif
        }

        private static bool DequeueAndWriteMessages()
        {
            bool dequeuedAny = false;
            StringBuilder batchBuilder = new StringBuilder(8192);
            LogMessage message;

            // Process messages in batches for better performance
            while (_logQueue.TryDequeue(out message))
            {
                dequeuedAny = true;
                Interlocked.Decrement(ref _queuedItemsCount);

                // Format the log entry
                batchBuilder.AppendFormat("[{0}] (Thread:{1}) {2:yyyy-MM-dd HH:mm:ss.fff} {3}{4}",
                    message.Level.ToString().ToUpper(),
                    message.ThreadId,
                    message.Timestamp,
                    message.Message,
                    Environment.NewLine);

                // Write to file in batches to reduce I/O
                if (batchBuilder.Length > 4096)
                {
                    WriteToFile(batchBuilder.ToString());
                    batchBuilder.Clear();
                }
            }

            // Write any remaining messages
            if (batchBuilder.Length > 0)
            {
                WriteToFile(batchBuilder.ToString());
            }

            return dequeuedAny;
        }

        private static void ForceFlush(bool forceReopen)
        {
            lock (_flushLock)
            {
                try
                {
                    if (_streamWriter != null)
                    {
                        _streamWriter.Flush();
                        _lastFlushTime = DateTime.Now;

                        // Optionally reopen the file to ensure file handle is released
                        if (forceReopen)
                        {
                            InitializeFileStream();
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"GONetLog: Error flushing log: {ex.Message}");
                }
            }
        }

        private static void WriteToFile(string message)
        {
            try
            {
                lock (_flushLock)
                {
                    if (_streamWriter != null)
                    {
                        _streamWriter.Write(message);
                    }
                    else if (!_shutdownRequested)
                    {
                        // If writer is null but we're not shutting down, try to reinitialize
                        UnityEngine.Debug.LogWarning("GONetLog: StreamWriter was null, attempting to reinitialize...");
                        InitializeFileStream();

                        // Try writing again if reinitialization succeeded
                        if (_streamWriter != null)
                        {
                            _streamWriter.Write(message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"GONetLog: Error writing to log file: {ex.Message}");

                // Handle IO errors by attempting to recreate the file
                if (ex is IOException || ex is ObjectDisposedException)
                {
                    try
                    {
                        // Wait a moment in case of temporary file system issues
                        Thread.Sleep(100);
                        InitializeFileStream();
                    }
                    catch
                    {
                        // Ignore errors in recovery attempt
                    }
                }
            }
        }

        private static string GetLogFilePath(DateTime date)
        {
            return Path.Combine(LogDirectory, $"{LogFilePrefix}-{date:yyyy-MM-dd}{LogFileExtension}");
        }

        private static void CleanupOldLogFiles()
        {
            try
            {
                // Verify directory exists before attempting to enumerate files
                if (!Directory.Exists(LogDirectory))
                {
                    return;
                }

                DirectoryInfo dirInfo = new DirectoryInfo(LogDirectory);
                FileInfo[] files;

                try
                {
                    files = dirInfo.GetFiles($"{LogFilePrefix}-*{LogFileExtension}");
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"GONetLog: Failed to get log files for cleanup: {ex.Message}");
                    return;
                }

                DateTime cutoffDate = DateTime.Now.AddDays(-MaxLogFileDays);

                foreach (FileInfo file in files)
                {
                    try
                    {
                        // Extract date from filename
                        if (file.Name.Length >= LogFilePrefix.Length + 11) // Make sure filename is long enough
                        {
                            string dateText = file.Name.Substring(LogFilePrefix.Length + 1, 10); // Extract "yyyy-MM-dd"
                            DateTime fileDate;

                            if (DateTime.TryParseExact(dateText, "yyyy-MM-dd",
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out fileDate))
                            {
                                if (fileDate < cutoffDate)
                                {
                                    try
                                    {
                                        file.Delete();
                                    }
                                    catch (IOException)
                                    {
                                        // File might be locked by another process
                                        UnityEngine.Debug.LogWarning($"GONetLog: Could not delete log file {file.Name} (file may be in use)");
                                    }
                                }
                                continue; // Skip the fallback if we successfully parsed the date
                            }
                        }

                        // If any parse errors, use last write time as fallback
                        if (file.LastWriteTime < cutoffDate)
                        {
                            try
                            {
                                file.Delete();
                            }
                            catch (IOException)
                            {
                                // File might be locked by another process
                                UnityEngine.Debug.LogWarning($"GONetLog: Could not delete log file {file.Name} (file may be in use)");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Catch exceptions per file to continue processing other files
                        UnityEngine.Debug.LogWarning($"GONetLog: Error processing log file {file.Name} for cleanup: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"GONetLog: Failed to cleanup old log files: {ex.Message}");
            }
        }

        #endregion

        #region Supporting Types

#if UNITY_WEBGL
    // Use class instead of struct for WebGL since it doesn't support multithreading
    private class LogMessage
    {
        public DateTime Timestamp;
        public string Message;
        public LogLevel Level;
        public int ThreadId;
    }
#else
        private struct LogMessage
        {
            public DateTime Timestamp;
            public string Message;
            public LogLevel Level;
            public int ThreadId;
        }
#endif

        #endregion
    }
}