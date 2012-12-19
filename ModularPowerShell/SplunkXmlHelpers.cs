// ***********************************************************************
// Assembly         : ModularPowerShell
// Author           : Joel Bennett
// Created          : 12-18-2012
//
// Last Modified By : Joel Bennett
// Last Modified On : 12-18-2012
// ***********************************************************************
// <copyright file="SplunkXmlHelpers.cs" company="Splunk">
//     Copyright (c) Splunk. All rights reserved.
// </copyright>
// <summary>
//     Extension methods for XElement, etc.
// </summary>
// ***********************************************************************
namespace Splunk.ModularInputs
{
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Extension methods for working with System.Xml.Linq classes
    /// </summary>
    public static class SplunkXmlHelpers
    {
        /// <summary>
        /// Gets the parameter value, or an empty string.
        /// </summary>
        /// <param name="stanza">The stanza.</param>
        /// <param name="name">The parameter name.</param>
        /// <returns>The parameter value, if specified. Otherwise, an empty string</returns>
        public static string GetParameterValue(this XElement stanza, string name)
        {
            try
            {
                return stanza.Descendants("param").Single(p => p.Attribute("name").Value == name).Value;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}