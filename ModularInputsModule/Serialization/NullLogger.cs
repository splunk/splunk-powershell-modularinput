// ***********************************************************************
// Assembly         : ModularInputsModule
// Author           : Joel Bennett
// Created          : 03-07-2013
//
// Last Modified By : Joel Bennett
// Last Modified On : 03-07-2013
// ***********************************************************************
// <copyright file="NullLogger.cs" company="Splunk">
//     Copyright © 2013 by Splunk Inc., all rights reserved
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace Splunk.ModularInputs.Serialization
{
    using System.Collections.Generic;
    using System.Management.Automation;

    using Common.Logging;

    using Common.Logging;

    /// <summary>
    /// Class NullLogger - A Logger that doesn't do anything
    /// </summary>
    public class NullLogger : ILogger
    {
        /// <summary>
        /// Doesn't write out the specified message at the specified log level
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="format">A composite format string that contains text intermixed with zero or more format items</param>
        /// <param name="args">An object array that contains 0 or more items to format</param>
        public void WriteLog(LogLevel level, string format, params object[] args)
        {
        }

        /// <summary>
        /// Doesn't write out a message for each object in the collection, annotating it with the source stanza
        /// </summary>
        /// <param name="outputCollection">The PowerShell output</param>
        /// <param name="stanza">The input stanza</param>
        public void WriteOutput(IEnumerable<PSObject> outputCollection, string stanza)
        {
        }

        /// <summary>
        /// Doesn't write out a message for a single object, annotating it with the source stanza
        /// </summary>
        /// <param name="output">A single output</param>
        /// <param name="stanza">The input stanza</param>
        public void WriteOutput(PSObject output, string stanza)
        {
        }
    }
}