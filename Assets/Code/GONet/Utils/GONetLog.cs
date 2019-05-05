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

        const string SPACE = " ";
        const char NewLine = '\n';

        private static string lastLog;
        public static string LastLog { get { return lastLog; } }
        public delegate void LogDel(string logStr);
        public static event LogDel OnLog;

        static GONetLog()
        {
            _fileLogger = LogManager.GetLogger(typeof(GONetLog));
            string configPath = Application.isEditor ? "../configs/log_config.xml" : "configs/log_config.xml";
            FileInfo info = new FileInfo(Path.Combine(Application.dataPath, configPath));
            log4net.Config.XmlConfigurator.Configure(info);
            //manual stack traces instead of this because this results in no stack trace for non debug builds!
            //Application.logMessageReceivedThreaded += Log.OnLogMessageReceived;
        }

        [Conditional("LOG_INFO")]
        public static void Info(string message)
        {
            string formattedMessage = FormatMessage(KeyInfo, message);
            UnityEngine.Debug.Log(formattedMessage);
            StackTrace trace = new StackTrace(1, true);
            OnLogMessageReceived(formattedMessage, trace.ToString(), LogType.Log);
        }

        [Conditional("LOG_DEBUG")]
        public static void Debug(string message)
        {
            string formattedMessage = FormatMessage(KeyDebug, message);
            UnityEngine.Debug.Log(formattedMessage);
            StackTrace trace = new StackTrace(1, true);
            OnLogMessageReceived(formattedMessage, trace.ToString(), LogType.Log);
        }

        [Conditional("LOG_WARNING")]
        public static void Warning(string message)
        {
            string formattedMessage = FormatMessage(KeyWarning, message);
            UnityEngine.Debug.LogWarning(formattedMessage);
            StackTrace trace = new StackTrace(1, true);
            OnLogMessageReceived(formattedMessage, trace.ToString(), LogType.Warning);
        }

        [Conditional("LOG_ERROR")]
        public static void Error(string message)
        {
            string formattedMessage = FormatMessage(KeyError, message);
            UnityEngine.Debug.LogError(formattedMessage);
            StackTrace trace = new StackTrace(1, true);
            OnLogMessageReceived(formattedMessage, trace.ToString(), LogType.Error);
        }

        [Conditional("LOG_FATAL")]
        public static void Fatal(string message)
        {
            string formattedMessage = FormatMessage(KeyFatal, message);
            UnityEngine.Debug.LogError(formattedMessage);
            StackTrace trace = new StackTrace(1, true);
            GONetLog.OnLogMessageReceived(formattedMessage, trace.ToString(), LogType.Error);
        }

        private static string FormatMessage(string level, string message)
        {
            const string FORMAT = "[{0}] (Thread:{1}) ({2:dd MMM yyyy H:mm:ss.fff}) (frame:{3}s) {4}";
            return string.Format(FORMAT, level, Thread.CurrentThread.ManagedThreadId, DateTime.Now, GONetMain.Time.ElapsedSeconds, message);
        }

        [Conditional("LOG_VERBOSE")]
        public static void Verbose(string message)
        {
            UnityEngine.Debug.Log(FormatMessage(KeyVerbose, message));
        }


        private static ILog _fileLogger;
        //enum LogType { Info, Debug, Warning, Error, Fatal, Verbose};
        public static void OnLogMessageReceived(string logString, string stackTrace, UnityEngine.LogType type)
        {           
            if(!string.IsNullOrEmpty(stackTrace))
            {
                string[] stackLines = stackTrace.Split(NewLine);
                //string logLine = stackLines[1];
                //int startIndex = stackLines[0].Length + logLine.Length + 2;
                //get all
                //string callLines = stackTrace.Substring(startIndex, stackTrace.Length - startIndex - 1);
                ////string callLine = stackLines[2]; //this will print out only the calling line

                string callLine = stackTrace;
                string logLine = logString;
                if (logLine.Contains(GONetLog.KeyInfo))
                {
                    _fileLogger.Info(string.Concat(logString, NewLine, callLine, NewLine));
                }
                else if (logLine.Contains(GONetLog.KeyDebug))
                {
                    _fileLogger.Debug(string.Concat(logString, NewLine, callLine, NewLine));
                }
                else if (logLine.Contains(GONetLog.KeyWarning))
                {
                    _fileLogger.Warn(string.Concat(logString, NewLine, callLine, NewLine));
                }
                else if (logLine.Contains(GONetLog.KeyError))
                {
                    _fileLogger.Error(string.Concat(logString, NewLine, callLine, NewLine));
                }
                else if (logLine.Contains(GONetLog.KeyFatal))
                {
                    _fileLogger.Fatal(string.Concat(logString, NewLine, callLine, NewLine));
                }
                else if (logLine.Contains(GONetLog.KeyVerbose))
                {
                    _fileLogger.Info(string.Concat(logString, NewLine, callLine, NewLine));
                }
            }
            else
            {
                switch(type)
                {
                    case LogType.Log:
                        {
                            _fileLogger.Debug(logString);
                        }
                        break;
                    case LogType.Warning:
                        {
                            _fileLogger.Warn(logString);
                        }
                        break;
                    case LogType.Error:
                        {
                            _fileLogger.Error(logString);
                        }
                        break;
                    case LogType.Exception:
                        {
                            _fileLogger.Fatal(logString);
                        }
                        break;
                    default:
                        {
                            _fileLogger.Info(logString);
                        }
                        break;
                }
            }
            lastLog = logString;

            OnLog?.Invoke(lastLog);
        }
    }
}