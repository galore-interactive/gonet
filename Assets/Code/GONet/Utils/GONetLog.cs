using System.Diagnostics;
using System.IO;
using UnityEngine;
using log4net;
using System;
using System.Threading;

namespace GONet.Utils
{
    /// <summary>
    /// A nice log4net based logging utility that can/should replace <see cref="UnityEngine.Debug"/> LogXxx methods usage.
    /// Advantages:
    ///     1. log statements will all have a nice/complete date/time stamp (that includes millisecond and uses GONet's own <see cref="HighResolutionTimeUtils"/>).  This alone might change your life!
    ///     2. log statements will go to the console as usual as well as a gonet.log file in /logs folder
    ///     3. gonet.log file history will be maintained for up to 10 files
    ///     4. you can reconfigure the settings of how the log functions using log4net config stuff (initially configured to be located in configs/log_config.xml)
    ///     5. you can subscribe
    ///     6. you can conditionally exclude calls to any/all logging level methods using #DEFINE (i.e., remove everything from a production release/build if you like)
    /// </summary>
    public static class GONetLog
    {
        //these keys are used to figure out which Log. Method was used when logging
        //because the observer of unity's logging reports unity log levels...not necessarily the 
        //log levels we will print out to log4net
        const string KeyInfo = "Log:Info";
        const string KeyDebug = "Log:Debug";
        const string KeyWarning = "Log:Warning";
        const string KeyError = "Log:Error";
        const string KeyFatal = "Log:Fatal";
        const string KeyVerbose = "Log:Verbose"; //this isn't really implemented the way we want yet

        const char NewLine = '\n';
        const string SPACE = " ";
        private const string FORMAT = "[{0}] ({1:dd MMM yyyy H:mm:ss.fff}) {2}";

        private static string lastLog;
        public static string LastLog { get { return lastLog; } }
        public delegate void LogDel(string logStr);
        public static event LogDel OnLog;

        private static ILog _fileLogger;

        static GONetLog()
        {
            _fileLogger = LogManager.GetLogger(typeof(GONetLog));
            string configPath = Application.isEditor ? "../configs/log_config.xml" : "configs/log_config.xml";
            FileInfo info = new FileInfo(Path.Combine(Application.dataPath, configPath));
            log4net.Config.XmlConfigurator.Configure(info);
        }

        [Conditional("LOG_INFO")]
        public static void Info(string message)
        {
            LogInternal(message, KeyInfo, LogType.Log);
        }

        [Conditional("LOG_DEBUG")]
        public static void Debug(string message)
        {
            LogInternal(message, KeyDebug, LogType.Log);
        }

        [Conditional("LOG_WARNING")]
        public static void Warning(string message)
        {
            LogInternal(message, KeyWarning, LogType.Warning);
        }

        [Conditional("LOG_ERROR")]
        public static void Error(string message)
        {
            LogInternal(message, KeyError, LogType.Error);
        }

        [Conditional("LOG_FATAL")]
        public static void Fatal(string message)
        {
            LogInternal(message, KeyFatal, LogType.Error);
        }

        private static void LogInternal(string message, string keyXxx, LogType logType)
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
                ProcessMessageViaLogger(string.Concat(keyXxx, SPACE, message), trace.ToString(), logType);
            }
        }

        [Conditional("LOG_VERBOSE")]
        public static void Verbose(string message)
        {
            UnityEngine.Debug.Log(FormatMessage(KeyVerbose, message));
        }

        private static string FormatMessage(string level, string message)
        {
            const string FORMAT = "[{0}] (Thread:{1}) ({2:dd MMM yyyy H:mm:ss.fff}) (frame:{3}s) {4}";
            return string.Format(FORMAT, level, Thread.CurrentThread.ManagedThreadId, DateTime.Now, GONetMain.Time.ElapsedSeconds, message);
        }

        private static void ProcessMessageViaLogger(string logString, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(stackTrace))
            {
                switch (type)
                {
                    case LogType.Log:
                        _fileLogger.Debug(logString);
                        break;

                    case LogType.Warning:
                        _fileLogger.Warn(logString);
                        break;

                    case LogType.Error:
                        _fileLogger.Error(logString);
                        break;

                    case LogType.Exception:
                        _fileLogger.Fatal(logString);
                        break;

                    default:
                        _fileLogger.Info(logString);
                        break;
                }
            }
            else
            {
                string callLine = stackTrace;
                string logLine = logString;
                string concattedMessage = string.Concat(logString, NewLine, callLine, NewLine);
                if (logLine.Contains(KeyInfo))
                {
                    _fileLogger.Info(concattedMessage);
                }
                else if (logLine.Contains(KeyDebug))
                {
                    _fileLogger.Debug(concattedMessage);
                }
                else if (logLine.Contains(KeyWarning))
                {
                    _fileLogger.Warn(concattedMessage);
                }
                else if (logLine.Contains(KeyError))
                {
                    _fileLogger.Error(concattedMessage);
                }
                else if (logLine.Contains(KeyFatal))
                {
                    _fileLogger.Fatal(concattedMessage);
                }
                else if (logLine.Contains(KeyVerbose))
                {
                    _fileLogger.Info(concattedMessage);
                }
            }
            lastLog = logString;

            OnLog?.Invoke(lastLog);
        }
    }
}