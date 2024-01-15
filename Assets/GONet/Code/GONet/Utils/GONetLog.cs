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

using System.Diagnostics;
using System.IO;
using UnityEngine;
using log4net;
using System;
using System.Threading;
using System.Collections.Concurrent;
using System.Text;
using GONet.Utils;

namespace GONet
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

        private static string lastLog;
        public static string LastLog { get { return lastLog; } }
        public delegate void LogDel(string logStr);
        public static event LogDel OnLog;

        private static ILog _fileLogger;

        private static readonly string configXml = @" <log4net>
 
   <appender name=""FileAppender"" type=""log4net.Appender.RollingFileAppender"">
     <file value=""logs\gonet.log"" />
     <appendToFile value=""true"" />
     <rollingStyle value=""Once"" />
     <maxSizeRollBackups value=""10"" />
     <maximumFileSize value=""10MB"" />
     <staticLogFileName value=""true"" />
     <layout type=""log4net.Layout.PatternLayout"">
       <!-- <conversionPattern value=""%date %-5level in [%thread] %logger%newline%message%newline"" /> -->
	   <conversionPattern value=""[%-5level] (Thread:%t) %date{yyyy-MM-dd HH:mm:ss.fff} %message%newline"" />
     </layout>
   </appender>
   
	<appender name=""ConsoleAppender"" type=""log4net.Appender.ConsoleAppender"">
		<layout type=""log4net.Layout.PatternLayout"">
			   <conversionPattern value=""[%-5level] (Thread:%t) %date{yyyy-MM-dd HH:mm:ss.fff} %message%newline"" />
		 </layout>
	</appender>
 
   <root>
     <level value=""ALL"" />
     <appender-ref ref=""FileAppender"" />
     <appender-ref ref=""ConsoleAppender"" />
   </root>
   
 </log4net>
";

        static GONetLog()
        {
            _fileLogger = LogManager.GetLogger(typeof(GONetLog));
            using Stream configXmlStream = StringUtils.GenerateStreamFromString(configXml);
            log4net.Config.XmlConfigurator.Configure(configXmlStream);
        }

        #region Append methods

        static readonly ConcurrentDictionary<Thread, StringBuilder> appendStringBuilderByThreadMap = new ConcurrentDictionary<Thread, StringBuilder>();

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
            StringBuilder stringBuilder;
            if (!appendStringBuilderByThreadMap.TryGetValue(Thread.CurrentThread, out stringBuilder))
            {
                appendStringBuilderByThreadMap[Thread.CurrentThread] = stringBuilder = new StringBuilder(5000);
            }

            stringBuilder.Append(message);
            if (doesIncludeEOL)
            {
                stringBuilder.Append(Environment.NewLine);
            }
        }

        public static bool Append_FlushVerbose(string message = null)
        {
            return Append_Flush(KeyVerbose, message);
        }

        public static bool Append_FlushDebug(string message = null)
        {
            return Append_Flush(KeyDebug, message);
        }

        public static bool Append_FlushInfo(string message = null)
        {
            return Append_Flush(KeyInfo, message);
        }

        public static bool Append_FlushWarning(string message = null)
        {
            return Append_Flush(KeyWarning, message);
        }

        public static bool Append_FlushError(string message = null)
        {
            return Append_Flush(KeyError, message);
        }

        public static bool Append_FlushFatal(string message = null)
        {
            return Append_Flush(KeyFatal, message);
        }

        private static bool Append_Flush(string logLevelKey, string message)
        {
            if (message != null)
            {
                Append(message, false);
            }

            StringBuilder stringBuilder;
            if (appendStringBuilderByThreadMap.TryGetValue(Thread.CurrentThread, out stringBuilder))
            {
                if (stringBuilder.Length > 0)
                {
                    switch (logLevelKey)
                    {
                        case KeyVerbose:
                            Verbose(stringBuilder.ToString());
                            break;
                        case KeyDebug:
                            Debug(stringBuilder.ToString());
                            break;
                        case KeyInfo:
                            Info(stringBuilder.ToString());
                            break;
                        case KeyWarning:
                            Warning(stringBuilder.ToString());
                            break;
                        case KeyError:
                            Error(stringBuilder.ToString());
                            break;
                        case KeyFatal:
                            Fatal(stringBuilder.ToString());
                            break;
                    }
                    stringBuilder.Clear();
                    return true;
                }
            }

            return false;
        }

        #endregion

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
                const string FRAME_PRE = "(frame:";
                const string FRAME_POST = "s) ";
                ProcessMessageViaLogger(string.Concat(FRAME_PRE, GONetMain.Time.ElapsedSeconds, FRAME_POST, keyXxx, SPACE, message), trace.ToString(), logType);
            }
        }

        [Conditional("LOG_VERBOSE")]
        public static void Verbose(string message)
        {
            UnityEngine.Debug.Log(FormatMessage(KeyVerbose, message));
        }

        private static string FormatMessage(string level, string message)
        {
            const string FORMAT = "[{0}]{5}{6} (Thread:{1}) ({2:dd MMM yyyy H:mm:ss.fff}) (frame:{3}s) {4}";
            const string CLIENT = "[Client]";
            const string SERVER = "[Server]";
            return string.Format(FORMAT, level, Thread.CurrentThread.ManagedThreadId, DateTime.Now, GONetMain.Time.ElapsedSeconds, message,
                GONetMain.IsServer ? SERVER : string.Empty,
                GONetMain.IsClient ? CLIENT : string.Empty
                );
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