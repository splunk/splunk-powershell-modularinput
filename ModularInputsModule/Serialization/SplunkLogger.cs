// ***********************************************************************
// Assembly         : ModularInputsModule
// Author           : Joel Bennett
// Created          : 03-06-2013
//
// Last Modified By : Joel Bennett
// Last Modified On : 03-07-2013
// ***********************************************************************
// <copyright file="SplunkLogger.cs" company="Splunk">
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
    /// SplunkLogger is an implemenation of ILogger that outputs to SplunkHome\var\log\splunk\powershell.log and Console.Out
    /// It should have a rotating file handler with maxBytes=25000000 and backupCount=5
    /// And it should format the log messages like '%(asctime)s %(levelname)s %(message)s'
    /// </summary>
    public class SplunkLogger : BaseLogger
    {
        public SplunkLogger()
        {
            // # Setup logger
            // logger = logging.getLogger('eventgen')
            // logger.propagate = False # Prevent the log messages from being duplicated in the python.log file
            // logger.setLevel(logging.INFO)
            // formatter = logging.Formatter('%(asctime)s %(levelname)s %(message)s')
            // streamHandler = logging.StreamHandler(sys.stdout)
            // streamHandler.setFormatter(formatter)
            // logger.addHandler(streamHandler)
            // fileHandler = logging.handlers.RotatingFileHandler(os.environ['SPLUNK_HOME'] + '/var/log/splunk/eventgen.log', maxBytes=25000000, backupCount=5)
            // formatter = logging.Formatter('%(asctime)s %(levelname)s %(message)s')
            // fileHandler.setFormatter(formatter)
        }

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


            if (level > LogLevel.Output)
            {
                Console.Error.WriteLine("{0} {1}", level.ToString().ToUpper(), format);
                Console.Error.Flush();
            }
            else
            {
                Debug.WriteLine("{0} s=\"PowerShell\" {1}", level.ToString().ToUpper(), format);
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
                        Console.Out.WriteLine((string)pOutput.BaseObject);
                        return;
                    }
                }
                // ReSharper disable EmptyGeneralCatchClause
                catch { }
                // ReSharper restore EmptyGeneralCatchClause
            }

            Console.Out.WriteLine(XmlFormatter.ConvertToXml(output, stanza));
        }
    }
}