using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChatLib
{
    public class Log
    {
        // Error, Warning, Info, Debug(All messages)
        // Error = 0
        // Warning = 5,
        // Info = 10,
        // Debug = 100
        public const int LevelInfo = 10;
        public const int LevelWarning = 5;
        public const int LevelError = 0;
        public const int LevelDebug = 100;

        private static Log _instance;
        private List<TextWriter> _outputs;
        private readonly object _outputsLock = new object();
        private int _logLevel = LevelInfo;


        public static Log Current { get { return _instance ?? (_instance = new Log()); } }

        /// <summary>
        /// Gets the current logging level
        /// </summary>
        public int CurrentLoggingLevel { get { return _logLevel; } }

        private Log()
        {
            _outputs = new List<TextWriter>();
        }

        /// <summary>
        /// Writes a log entry with the Info level. This is the third logging level.
        /// </summary>
        /// <param name="source">The source of the entry</param>
        /// <param name="message">The message to write</param>
        /// <param name="args">Any parameters to format into the message</param>
        public static void Info(string source, string message, params object[] args)
        {
            Current.Write(LevelInfo, source, message, args);
        }


        /// <summary>
        /// Writes a log entry with the Warning level. This is the second logging level.
        /// </summary>
        /// <param name="source">The source of the entry</param>
        /// <param name="message">The message to write</param>
        /// <param name="args">Any parameters to format into the message</param>
        public static void Warning(string source, string message, params object[] args)
        {
            Current.Write(LevelWarning, source, message, args);
        }

        /// <summary>
        /// Writes a log entry with the Error level. This is the lowest logging level.
        /// </summary>
        /// <param name="source">The source of the entry</param>
        /// <param name="message">The message to write</param>
        /// <param name="args">Any parameters to format into the message</param>
        public static void Error(string source, string message, params object[] args)
        {
            Current.Write(LevelError, source, message, args);
        }

        /// <summary>
        /// Writes a log entry with the Debug level. This is the highest logging level.
        /// </summary>
        /// <param name="source">The source of the entry</param>
        /// <param name="message">The message to write</param>
        /// <param name="args">Any parameters to format into the message</param>
        public static void Debug(string source, string message, params object[] args)
        {
            Current.Write(LevelDebug, source, message, args);
        }


        /// <summary>
        /// Writes an entry into the log with the specified verbosity level, source, and message
        /// </summary>
        /// <param name="level">The verbosity level of the message</param>
        /// <param name="source">The source of the message</param>
        /// <param name="message">A composite string containing the message body</param>
        /// <param name="args">Any parameters to format in the message body</param>
        public void Write(int level, string source, string message, params object[] args)
        {
            if (level > _logLevel)
                return; // Above our paygrade, don't log.
            
            string levelStr;
            if(level == LevelError)
                levelStr = "ERROR";
            else if(level <= LevelWarning)
                levelStr = "WARNING";
            else if(level <= LevelInfo)
                levelStr = "INFO";
            else
                levelStr = "DEBUG";

            // Output format is <DateTime> [<Source>/<Level>] <Message>
            string logLine = string.Format("{0} [{4}/{1}/{2}]: {3}", DateTime.Now.ToString(), source, levelStr, string.Format(message, args), System.Threading.Thread.CurrentThread.ManagedThreadId);

            // Write to all outputs
            lock (_outputsLock)
            {
                for (int i = 0; i < _outputs.Count; i++)
                {
                    _outputs[i].WriteLine(logLine);
                }
            }
        }

        /// <summary>
        /// Adds a logging output
        /// </summary>
        /// <param name="writer">A TextWriter to write log output to</param>
        public void AddOutput(TextWriter writer)
        {
            lock (_outputsLock)
                _outputs.Add(writer);
        }

        /// <summary>
        /// Removes a previously added logging output
        /// </summary>
        /// <param name="writer">A previously added TextWriter to remove</param>
        public void RemoveOutput(TextWriter writer)
        {
            lock (_outputsLock)
                _outputs.Remove(writer);
        }

        /// <summary>
        /// Sets the logging threshold. Any messages with a level above this number will be ignored
        /// </summary>
        /// <param name="level">The logging level</param>
        public void SetLogLevel(int level)
        {
            _logLevel = level;
        }
    }
}
