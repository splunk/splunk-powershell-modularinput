// ***********************************************************************
// Assembly         : ModularPowerShell
// Author           : Joel Bennett
// Created          : 12-07-2012
//
// Last Modified By : Joel Bennett
// Last Modified On : 12-18-2012
// ***********************************************************************
// <copyright file="SplunkXmlFormatter.cs" company="Splunk">
//     Copyright (c) 2012. All rights reserved.
// </copyright>
// <summary>
//    Defines the SplunkXmlFormatter class which handles formatting for Splunk XML output streaming
// </summary>
// ***********************************************************************
namespace Splunk.ModularInputs
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Management.Automation;
    using System.Text;

    using Splunk.ModularInputs.Properties;

    /// <summary>
    /// List of appropriate log levels for logging functions
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Debug Messages
        /// </summary>
        Debug,

        /// <summary>
        /// Informational Messages
        /// </summary>
        Info,

        /// <summary>
        /// Warning Messages
        /// </summary>
        Warn,

        /// <summary>
        /// Error Messages
        /// </summary>
        Error,

        /// <summary>
        /// Fatal Error Messages
        /// </summary>
        Fatal
    }

    /// <summary>
    /// Handles formatting for Splunk XML streaming
    /// </summary>
    public static class SplunkXmlFormatter
    {
        /// <summary>
        /// The Unix Epoch time
        /// </summary>
        private static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan(0));

        /// <summary>
        /// The names of the special properties which are recognized on objects
        /// </summary>
        private static readonly string[] ReservedProperties = new[] { "SplunkIndex", "SplunkSource", "SplunkHost", "SplunkSourceType", "SplunkTime" };

        /// <summary>
        /// Convenience method to write log messages to splunkd.log
        /// </summary>
        /// <param name="msg">The message</param>
        public static void Write(string msg)
        {
            Write(LogLevel.Info, msg);
        }

        /// <summary>
        /// Convenience method to write log messages to splunkd.log
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="msg">The message</param>
        public static void Write(LogLevel level, string msg)
        {
            Console.Error.WriteLine("{0} {1}", level.ToString().ToUpper(), msg);
            Console.Error.Flush();
        }

        /// <summary>
        /// Writes out splunk xml events
        /// </summary>
        /// <param name="outputCollection">The PowerShell output</param>
        public static void WriteOutput(Collection<PSObject> outputCollection)
        {
            foreach (var output in outputCollection)
            {
                Console.Out.WriteLine("<event>\n\t{0}</event>", GetData(output));
            }
        }

        /// <summary>
        /// Writes out splunk xml events for the specified stanza
        /// </summary>
        /// <param name="outputCollection">The PowerShell output</param>
        /// <param name="stanza">The input stanza</param>
        public static void WriteOutput(Collection<PSObject> outputCollection, string stanza)
        {
            foreach (var output in outputCollection)
            {
                Console.Out.WriteLine("<event stanza=\"{1}\">\n\t{0}</event>", GetData(output), stanza);
            }
        }

        /// <summary>
        /// Gets a Name="Value"; representation of the data.
        /// </summary>
        /// <param name="output">The object being output.</param>
        /// <returns>A string representation of the object.</returns>
        public static string GetData(PSObject output)
        {
            var sb = new StringBuilder("<data>");

            // NOTE: we have to ignore Script Properties, because they require a runspace
            bool hasTime = false;
            foreach (var property in output.Properties.Where(p => p.MemberType != PSMemberTypes.ScriptProperty && p.IsGettable))
            {
                var value = string.Empty;
                var name = property.Name;
                var hasError = false;

                try
                {
                    value = property.Value.ToString();
                }
                catch (Exception ex)
                {
                    hasError = true;
                    if (Settings.Default.LogOutputErrors)
                    {
                        // TODO: Should we log these to a per-script location?
                        Write(LogLevel.Error, ex.Message + "\nSTACK TRACE:\n" + ex.StackTrace);
                    }
                }

                if (!hasError || Settings.Default.OutputBlanksOnError)
                {
                    // Handle special property names
                    if (!hasError && Array.IndexOf(ReservedProperties, name) > 0)
                    {
                        name = name.Remove(0, 6).ToLowerInvariant();
                        if (name.Equals("time"))
                        {
                            DateTimeOffset time;
                            try
                            {
                                time = (DateTimeOffset)property.Value;
                            }
                            catch
                            {
                                if (!DateTimeOffset.TryParse(value, out time))
                                {
                                    time = DateTimeOffset.UtcNow;
                                }
                            }

                            // convert the time to unix epoch time
                            value = (time - Epoch).TotalSeconds.ToString(CultureInfo.InvariantCulture);
                            hasTime = true;
                        }

                        sb.Insert(0, string.Format("<{0}>{1}</{0}>\n", name, value));
                    }
                    else
                    {
                        sb.AppendFormat("{0}=\"{1}\"\n", name, value);
                    }
                }
            }

            // make sure we always define the time
            if (!hasTime)
            {
                // convert the time to unix epoch time
                var value = (DateTimeOffset.UtcNow - Epoch).TotalSeconds.ToString(CultureInfo.InvariantCulture);
                sb.Insert(0, "<time>" + value + "</time>\n");
            }

            return sb.Append("</data>\n").ToString();
        }
    }
}
