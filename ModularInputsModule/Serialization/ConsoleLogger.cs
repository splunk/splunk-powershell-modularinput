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
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Management.Automation;

    /// <summary>
    /// ConsoleLogger is an implemenation of ILogger that outputs to Console.Error and Console.Out
    /// </summary>
    public class ConsoleLogger : BaseLogger
    {
        /// <summary>
        /// Convenience method to write log messages to splunkd.log
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="format">A composite format string that contains text intermixed with zero or more format items</param>
        /// <param name="args">An object array that contains 0 or more items to format</param>
        public override void WriteLog(LogLevel level, string format, params object[] args)
        {
            if (args.Length > 0)
            {
                format = string.Format(format, args);
            }

            Debug.WriteLine("{0} s=\"PowerShell\" {1}", level.ToString().ToUpper(), format);

            if (level > LogLevel.Output)
            {
                Console.Error.WriteLine("{0} {1}", level.ToString().ToUpper(), format);
                Console.Error.Flush();
            }
            else
            {
                Console.Out.WriteLine(format);
                Console.Out.Flush();
            }
        }

        /// <summary>
        /// Writes out a message for a single object, annotating it with the source stanza
        /// </summary>
        /// <param name="output">A single output</param>
        /// <param name="stanza">The input stanza</param>
        public override void WriteOutput(dynamic output, string stanza)
        {
            var pOutput = output as PSObject;
            if (pOutput != null && pOutput.BaseObject is string && pOutput.Properties.Match("SplunkPreFormatted").Count > 0)
            {
                try
                {
                    if (output.SplunkPreFormatted)
                    {
                        this.WriteLog(LogLevel.Output, (string)pOutput.BaseObject);
                        return;
                    }
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch { }
                // ReSharper restore EmptyGeneralCatchClause
            }

            this.WriteLog(LogLevel.Output, XmlFormatter.ConvertToString(output, stanza));
        }
    }
}