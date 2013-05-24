// ***********************************************************************
// Assembly         : ModularInputsModule
// Author           : Joel Bennett
// Created          : 03-06-2013
//
// Last Modified By : Joel Bennett
// Last Modified On : 03-07-2013
// ***********************************************************************
// <copyright file="ConsoleLogger.cs" company="Splunk">
//     Copyright © 2013 by Splunk Inc., all rights reserved
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace Splunk.ModularInputs.Serialization
{
    using Common.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Management.Automation;

    /// <summary>
    /// ConsoleLogger is an implemenation of ILogger that outputs to Console.Error and Console.Out
    /// </summary>
    public class ConsoleLogger : BaseLogger
    {
        private readonly ILog debugLog = LogManager.GetLogger("debug");
        private readonly ILog outputLog = LogManager.GetLogger("output");

        /// <summary>
        /// Convenience method to write log messages to splunkd.log
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="format">A composite format string that contains text intermixed with zero or more format items</param>
        /// <param name="args">An object array that contains 0 or more items to format</param>
        public override void WriteLog(LogLevel level, string format, params object[] args)
        {
            switch (level)
            {
                case LogLevel.All:
                    outputLog.TraceFormat(format, args);
                    break;
                case LogLevel.Trace:
                    outputLog.TraceFormat(format, args);
                    break;
                case LogLevel.Debug:
                    debugLog.DebugFormat(format, args);
                    break;
                case LogLevel.Info:
                    debugLog.InfoFormat(format, args);
                    break;
                case LogLevel.Warn:
                    debugLog.WarnFormat(format, args);
                    break;
                case LogLevel.Error:
                    debugLog.ErrorFormat(format, args);
                    break;
                case LogLevel.Fatal:
                    debugLog.FatalFormat(format, args);
                    break;
                case LogLevel.Off:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("level");
            }
        }

        /// <summary>
        /// Writes out a message for a single object, annotating it with the source stanza
        /// </summary>
        /// <param name="output">A single output</param>
        /// <param name="stanza">The input stanza</param>
        public override void WriteOutput(PSObject output, string stanza)
        {
            var pOutput = output;
            if (pOutput != null && pOutput.BaseObject is string && pOutput.Properties.Match("SplunkPreFormatted").Count > 0)
            {
                try
                {
                    if ((bool)pOutput.Properties["SplunkPreFormatted"].Value)
                    {
                        outputLog.Trace((string)pOutput.BaseObject);
                        return;
                    }
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch { }
                // ReSharper restore EmptyGeneralCatchClause
            }
            outputLog.Trace(XmlFormatter.ConvertToXml(pOutput, stanza));
        }
    }
}