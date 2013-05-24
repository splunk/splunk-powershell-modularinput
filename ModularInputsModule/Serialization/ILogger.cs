// ***********************************************************************
// Assembly         : ModularInputsModule
// Author           : Joel Bennett
// Created          : 03-06-2013
//
// Last Modified By : Joel Bennett
// Last Modified On : 03-07-2013
// ***********************************************************************
// <copyright file="ILogger.cs" company="Splunk">
//     Copyright © 2013 by Splunk Inc., all rights reserved
// </copyright>
// <summary></summary>
// ***********************************************************************
namespace Splunk.ModularInputs.Serialization
{
    using System.Collections.Generic;
    using System.Management.Automation;

    using Common.Logging;

    /// <summary>
    /// Logging interface 
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Writes out the specified message at the specified log level
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="format">A composite format string that contains text intermixed with zero or more format items</param>
        /// <param name="args">An object array that contains 0 or more items to format</param>
        void WriteLog(LogLevel level, string format, params object[] args);

        /// <summary>
        /// Writes out a message for each object in the collection, annotating it with the source stanza
        /// </summary>
        /// <param name="outputCollection">The PowerShell output</param>
        /// <param name="stanza">The input stanza</param>
        void WriteOutput(IEnumerable<PSObject> outputCollection, string stanza);

        /// <summary>
        /// Writes out a message for a single object, annotating it with the source stanza
        /// </summary>
        /// <param name="output">A single output</param>
        /// <param name="stanza">The input stanza</param>
        void WriteOutput(PSObject output, string stanza);
    }


    /// <summary>
    /// Provides a base class for ILogger implementations
    /// </summary>
    public abstract class BaseLogger : ILogger
    {
        /// <summary>
        /// Writes out splunk xml events for the specified stanza
        /// </summary>
        /// <param name="outputCollection">The PowerShell output</param>
        /// <param name="stanza">The input stanza</param>
        public virtual void WriteOutput(IEnumerable<PSObject> outputCollection, string stanza)
        {
            foreach (var output in outputCollection)
            {
                if (output != null)
                {
                    var psOutput = output as PSObject;
                    if (psOutput != null && psOutput.BaseObject != null)
                    {
                        this.WriteOutput(psOutput, stanza);
                    }
                }
            }
        }

        /// <summary>
        /// Writes out a message for a single object, annotating it with the source stanza
        /// </summary>
        /// <param name="output">A single output</param>
        /// <param name="stanza">The input stanza</param>
        public abstract void WriteOutput(PSObject output, string stanza);

        /// <summary>
        /// Writes out the specified message at the specified log level
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="format">A composite format string that contains text intermixed with zero or more format items</param>
        /// <param name="args">An object array that contains 0 or more items to format</param>
        public abstract void WriteLog(LogLevel level, string format, params object[] args);
    }
}