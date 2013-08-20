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
    using System.Linq;

    using Common.Logging;
    using System;
    using System.Globalization;
    using System.Management.Automation;
    using System.Text.RegularExpressions;

    /// <summary>
    /// ConsoleLogger is an implemenation of ILogger that outputs to Console.Error and Console.Out
    /// </summary>
    public class ConsoleLogger : BaseLogger
    {
        private readonly ILog debugLog = LogManager.GetLogger("debug");
        private readonly ILog outputLog = LogManager.GetLogger("output");

        /// <summary>
        /// Convenience method to write output for splunkd.
        /// LogLevel.All is output, everything else goes to the powershell.log
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="format">A composite format string that contains text intermixed with zero or more format items</param>
        /// <param name="args">An object array that contains 0 or more items to format</param>
        public override void WriteLog(LogLevel level, string format, params object[] args)
        {
            // remove newlines from output messages (this is one of the reasons everything goes through here.
            var logMessage = base.Trim.Replace(string.Format(CultureInfo.InvariantCulture, format, args), " ");

            switch (level)
            {
                case LogLevel.All:
                    outputLog.Trace(logMessage);
                    break;
                case LogLevel.Trace:
                    debugLog.Trace(logMessage);
                    break;
                case LogLevel.Debug:
                    debugLog.Debug(logMessage);
                    break;
                case LogLevel.Info:
                    debugLog.Info(logMessage);
                    break;
                case LogLevel.Warn:
                    debugLog.Warn(logMessage);
                    break;
                case LogLevel.Error:
                    debugLog.Error(logMessage);
                    break;
                case LogLevel.Fatal:
                    debugLog.Fatal(logMessage);
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
                    var preformatted = pOutput.Properties.Match("SplunkPreFormatted").FirstOrDefault();
                    if ((preformatted != null) && preformatted.Value is bool)
                    {
                        if ((bool)preformatted.Value)
                        {
                            outputLog.Trace(pOutput.BaseObject);
                            return;
                        }
                    }
                    else
                    {
                        this.WriteLog(LogLevel.Error, "Error: Invalid 'SplunkPreFormatted' property: {0}", preformatted);
                    }
                }
                catch (Exception ex)
                {
                    this.WriteLog(LogLevel.Error, "Exception while logging 'SplunkPreFormatted' output: {0}", ex.Message);
                }
            }
            this.WriteLog(LogLevel.All, XmlFormatter.ConvertToXml(pOutput, stanza));
        }
    }
}