// ***********************************************************************
// Assembly         : ModularInputsModule
// Author           : Joel Bennett
// Created          : 03-06-2013
//
// Last Modified By : Joel Bennett
// Last Modified On : 03-07-2013
// ***********************************************************************
// <copyright file="XmlFormatter.cs" company="Splunk">
//     Copyright © 2013 by Splunk Inc., all rights reserved
// </copyright>
// <summary>
//   Splunk formatter for XML output from Modular Inputs
// </summary>
// ***********************************************************************
namespace Splunk.ModularInputs.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Management.Automation;
    using System.Security;
    using System.Text;

    using Microsoft.Practices.Unity;

    /// <summary>
    /// Splunk formatter for XML output
    /// </summary>
    public class XmlFormatter
    {
        /// <summary>
        /// The Unix Epoch time
        /// </summary>
        private static readonly DateTimeOffset Epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, new TimeSpan(0));

        /// <summary>
        /// The names of the special properties which are recognized on objects
        /// </summary>
        private static readonly HashSet<string> ReservedProperties = new HashSet<string>( new[]{ "SplunkIndex", "SplunkSource", "SplunkHost", "SplunkSourceType", "SplunkTime" });

        static XmlFormatter()
        {
            Logger = new NullLogger();
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

        /// <summary>
        /// Gets or sets the logger used for writing output.
        /// </summary>
        /// <value>The logger.</value>
        [Dependency]
        public static ILogger Logger { get; set; }

        /// <summary>
        /// Gets a Name="Value"; representation of the data.
        /// </summary>
        /// <param name="output">The object being output.</param>
        /// <param name="stanza">A name to use for the stanza attribute</param>
        /// <param name="properties">The names of the properties to output</param>
        /// <returns>A string representation of the object.</returns>
        public static string ConvertToString(PSObject output, string stanza = null, HashSet<string> properties = null)
        {
            // If they pass in a property list, make sure it has all the ReservedProperties in it:
            if (properties != null)
            {
                properties.UnionWith(ReservedProperties);
            }
            else if (output.BaseObject is string)
            {
                // Unless otherwise specified, only output the contents and the Splunk* properties
                properties = new HashSet<string>(ReservedProperties);
            }

            IEnumerable<dynamic> values;
            if (output.BaseObject is IEnumerable<KeyValuePair<string, object>>)
            {
                values = ConvertToNameValueObjects(output.BaseObject as IEnumerable<KeyValuePair<string, object>>, properties);
            }
            else if (output.BaseObject is IDictionary)
            {
                values = ConvertToNameValueObjects(output.BaseObject as IDictionary, properties);
            }
            else
            {
                values = FilterNameValueObjects(output.Properties, properties);
            }

            string data = KeyValuePairs(values, output.BaseObject as string);

            // wrap the whole thing inside an event tag
            return "<event" + (string.IsNullOrEmpty(stanza) ? ">" : " stanza=\"" + stanza + "\">") +
                         data +
                   "</event>\n";
        }

        /// <summary>
        /// Filters a list of PSPropertyInfo objects to only the ones which are gettable and in the list of names
        /// </summary>
        /// <param name="output">The properties to be selected.</param>
        /// <param name="properties">An optional list of keys that we care about.</param>
        /// <returns>An enumerable collection of PSPropertyInfo objects which have Name and Value properties</returns>
        private static IEnumerable<dynamic> FilterNameValueObjects(IEnumerable<PSPropertyInfo> output, IEnumerable<string> properties)
        {
            Debug.Assert(output != null, "output != null");

            if (properties == null)
            {
                return output.Where(p => p.MemberType != PSMemberTypes.ScriptProperty && p.IsGettable);
            }

            return output.Where(
                p =>
                properties.Contains(p.Name, StringComparer.InvariantCultureIgnoreCase)
                && p.MemberType != PSMemberTypes.ScriptProperty && p.IsGettable);
        }

        /// <summary>
        /// Converts to a filtered list of Name-Value pairs, representing some or all of the entries in output
        /// </summary>
        /// <param name="output">The objects to be converted.</param>
        /// <param name="keys">An optional list of keys that we care about.</param>
        /// <returns>An enumerable collection of dynamic objects with Name and Value properties</returns>
        private static IEnumerable<dynamic> ConvertToNameValueObjects(IEnumerable<KeyValuePair<string, object>> output, ICollection<string> keys)
        {
            return keys == null ?
                output.Select(kv => new { Name = kv.Key, kv.Value }) :
                output.Where(kv => keys.Contains(kv.Key)).Select(kv => new { Name = kv.Key, kv.Value });
        }

        /// <summary>
        /// Converts a Dictionary to a filtered list of Name-Value pairs representing some or all of the entries in the dictionary
        /// </summary>
        /// <param name="output">The objects to be converted.</param>
        /// <param name="keys">An optional list of keys that we care about.</param>
        /// <returns>An enumerable collection of dynamic objects with Name and Value properties</returns>
        private static IEnumerable<dynamic> ConvertToNameValueObjects(IDictionary output, IEnumerable<string> keys)
        {
            if (keys == null)
            {
                foreach (DictionaryEntry kv in output)
                {
                    yield return new { Name = kv.Key.ToString(), kv.Value };
                }
            }
            else
            {
                foreach (var name in keys.Where(output.Contains))
                {
                    yield return new { Name = name, Value = output[name] };
                }
            }
        }

        /// <summary>
        /// Converts objects with Names/Keys and Values into Name="Value" strings
        /// </summary>
        /// <param name="objects">The key/value pairs.</param>
        /// <param name="content">Extra content to be embedded below the object output</param>
        /// <returns>The string representation of the objects</returns>
        private static string KeyValuePairs(IEnumerable<dynamic> objects, string content = "")
        {
            bool hasTime = false;
            var meta = new StringBuilder();
            var data = new StringBuilder();

            // We still process the properties, because in PowerShell Strings can have ETS properties
            // Specifically, we might be adding Splunk* data
            // TODO: if we're in use as a cmdlet, we have a runspace, and can process Script Properties            
            foreach (dynamic property in objects)
            {
                string name, value = string.Empty;

                try
                {
                    name = property.Name;
                }
                catch
                {
                    name = property.Key;
                }

                try
                {
                    if (property.Value != null)
                    {
                        value = property.Value.ToString();
                    }
                }
                catch
                {
                    value = string.Empty;
                }

                // Handle special property names
                if (ReservedProperties.Contains(name))
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

                    meta.Insert(0, string.Format("<{0}>{1}</{0}>\n", name, value));
                }
                else
                {
                    data.AppendFormat("{0}=\"{1}\"\n", name, value);
                }
            }

            // make sure we *always* define the time
            if (!hasTime)
            {
                // convert the time to unix epoch time
                var value = (DateTimeOffset.UtcNow - Epoch).TotalSeconds.ToString(CultureInfo.InvariantCulture);
                meta.Insert(0, "<time>" + value + "</time>\n");
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                data.Append(content);
            }



            return meta.ToString() + "<data>" + SecurityElement.Escape(data.ToString()) + "</data>";
        }
    }
}