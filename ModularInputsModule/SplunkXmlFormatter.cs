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
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Management.Automation;
    using System.Text;


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
    [Cmdlet(VerbsData.ConvertTo, "SplunkEventXml")]
    public class SplunkXmlFormatter : Cmdlet
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
        /// Gets or sets the stanza name for the &lt;event&gt; output.
        /// </summary>
        /// <returns>The stanza name</returns>
        [Parameter]
        public string Stanza { get; set; }

        /// <summary>
        /// Gets or sets the InputObject to be output
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }


        /// <summary>
        /// Gets or sets the list of properties that we want to output
        /// </summary>
        /// <value>The property names.</value>
        [Parameter]
        public HashSet<string> Property { get; set; }


        /// <summary>
        /// Gets or sets the value of AsXml to control whether the output is wrapped in event tags.
        /// </summary>
        [Parameter]
        public SwitchParameter AsXml { get; set; }
        
        protected override void ProcessRecord()
        {
            var output = GetData(this.InputObject, this.Stanza, this.Property);
            base.WriteObject(output);
            base.ProcessRecord();
        }

        /// <summary>
        /// Convenience method to write log messages to splunkd.log
        /// </summary>
        /// <param name="msg">The message</param>
        public static void WriteLog(string msg)
        {
            WriteLog(LogLevel.Info, msg);
        }

        /// <summary>
        /// Convenience method to write log messages to splunkd.log
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="msg">The message</param>
        public static void WriteLog(LogLevel level, string msg)
        {
            Console.Error.WriteLine("{0} {1}", level.ToString().ToUpper(), msg);
            Console.Error.Flush();
        }


        /// <summary>
        /// Writes out splunk xml events
        /// </summary>
        /// <param name="outputCollection">The PowerShell output</param>
        public static void WriteOutput(IEnumerable<PSObject> outputCollection)
        {
            foreach (var output in outputCollection)
            {
                Console.Out.WriteLine(GetData(output));
            }
        }

        /// <summary>
        /// Writes out splunk xml events for the specified stanza
        /// </summary>
        /// <param name="outputCollection">The PowerShell output</param>
        /// <param name="stanza">The input stanza</param>
        public static void WriteOutput(IEnumerable<PSObject> outputCollection, string stanza)
        {
            foreach (var output in outputCollection)
            {
                Console.Out.WriteLine(GetData(output, stanza));
            }
        }

        /// <summary>
        /// Gets a Name="Value"; representation of the data.
        /// </summary>
        /// <param name="output">The object being output.</param>
        /// <param name="stanza">A name to use for the stanza attribute</param>
        /// <param name="properties">The names of the properties to output</param>
        /// <returns>A string representation of the object.</returns>
        private static string GetData(PSObject output, string stanza = null, HashSet<string> properties = null)
        {

            IEnumerable<PSPropertyInfo> psPropertyInfos;
            if (properties == null || properties.Count == 0)
            {
                psPropertyInfos = output.Properties.Where(p => p.MemberType != PSMemberTypes.ScriptProperty && p.IsGettable);
            }
            else
            {
                psPropertyInfos =
                    output.Properties.Where(p =>
                        properties.Contains(p.Name, StringComparer.InvariantCultureIgnoreCase) &&
                        p.MemberType != PSMemberTypes.ScriptProperty &&
                        p.IsGettable);
            }

            var sb = KeyValuePairs(output, psPropertyInfos);

            // wrap the whole thing inside an event tag
            sb.Insert(0, string.Format("<event {0}>", string.IsNullOrEmpty(stanza) ? "" : "stanza=\"" + stanza + "\""));

            return sb.Append("</data></event>\n").ToString();
        }

        private static StringBuilder KeyValuePairs(PSObject output, IEnumerable<PSPropertyInfo> psPropertyInfos)
        {
            bool hasTime = false;
            var sb = new StringBuilder("<data>");

            // We still process the properties, because in PowerShell Strings can have ETS properties
            // Specifically, we might be adding Splunk* data
            // TODO: if we're in use as a cmdlet, we have a runspace, and can process Script Properties
            
            foreach (PSPropertyInfo property in psPropertyInfos)
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
                    if (LogOutputErrors)
                    {
                        // TODO: Should we log these to a per-script location?
                        WriteLog(
                            LogLevel.Error,
                            ex.Message + " Encountered while reading '" + name + "'.\nSTACK TRACE:\n" + ex.StackTrace);
                    }
                }

                if (!hasError || OutputBlanksOnError)
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

            // If they output a string, it had better already be in a splunk-compatible format
            if (output.BaseObject is string || output.GetType().IsPrimitive)
            {
                sb.Append(output + "\n");
            }

            // make sure we *always* define the time
            if (!hasTime)
            {
                // convert the time to unix epoch time
                var value = (DateTimeOffset.UtcNow - Epoch).TotalSeconds.ToString(CultureInfo.InvariantCulture);
                sb.Insert(0, "<time>" + value + "</time>\n");
            }
            return sb;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to output blank values when there's an error
        /// </summary>
        /// <value><c>true</c> to output even on error; otherwise, <c>false</c>.</value>
        public static bool OutputBlanksOnError { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to log output errors.
        /// </summary>
        /// <value><c>true</c> to log errors; otherwise, <c>false</c>.</value>
        public static bool LogOutputErrors { get; set; }
    }
}
