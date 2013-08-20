// ***********************************************************************
// Assembly         : ModularInputsModule
// Author           : Joel Bennett
// Created          : 03-11-2013
//
// Last Modified By : Joel Bennett
// Last Modified On : 03-11-2013
// ***********************************************************************
// <copyright file="ObjectLogger.cs" company="Splunk">
//     Copyright (c) 2013 by Splunk Inc., all rights reserved.
// </copyright>
// <summary>
//   Defines the ObjectLogger.cs for ModularInputsModule in ModularPowerShell
// </summary>
namespace Splunk.ModularInputs.Serialization
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Management.Automation;
    using System.Text;

    using Common.Logging;

    public class ObjectLogger : BaseLogger
    {
        private readonly StringBuilder log = new StringBuilder();

        private readonly Dictionary<string, List<PSObject>> outputCache = new Dictionary<string, List<PSObject>>();

        /// <summary>
        /// Gets the output buffers.
        /// </summary>
        /// <value>The output.</value>
        public Dictionary<string, List<PSObject>> Output
        {
            get
            {
                return this.outputCache;
            }
        }

        /// <summary>
        /// Gets the log contents
        /// </summary>
        /// <value>The log.</value>
        public string Log
        {
            get
            {
                return this.log.ToString();
            }
        }

        /// <summary>
        /// Clears the log and output from this instance.
        /// </summary>
        public void Clear()
        {
            this.outputCache.Clear();
            this.log.Remove(0,this.log.Length);
        }

        /// <summary>
        /// Stores the specified message in the log
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="format">A composite format string that contains text intermixed with zero or more format items</param>
        /// <param name="args">An object array that contains 0 or more items to format</param>
        public override void WriteLog(LogLevel level, string format, params object[] args)
        {
            this.log.AppendFormat("{0}: ", level);
            this.log.Append(base.Trim.Replace(string.Format(CultureInfo.InvariantCulture, format, args), " "));
            this.log.AppendLine();
        }

        /// <summary>
        /// Stores the objects in the output by stanza
        /// </summary>
        /// <param name="output">The PowerShell output</param>
        /// <param name="stanza">The input stanza</param>
        public override void WriteOutput(PSObject output, string stanza)
        {
            if (!this.outputCache.ContainsKey(stanza))
            {
                this.outputCache.Add(stanza, new List<PSObject>());
            }
            this.outputCache[stanza].Add(output);
        }
    }
}