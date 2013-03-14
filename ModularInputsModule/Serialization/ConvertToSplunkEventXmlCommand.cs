// ***********************************************************************
// Assembly         : ModularPowerShell
// Author           : Joel Bennett
// Created          : 12-07-2012
//
// Last Modified By : Joel Bennett
// Last Modified On : 12-18-2012
// ***********************************************************************
// <copyright file="ConvertToSplunkEventXmlCommand.cs" company="Splunk">
//     Copyright © 2012, 2013 by Splunk Inc., all rights reserved
// </copyright>
// <summary>
//    Defines the ConvertToSplunkEventXmlCommand class which handles formatting for Splunk XML output streaming
// </summary>
// ***********************************************************************
namespace Splunk.ModularInputs.Serialization
{
    using System.Collections.Generic;
    using System.Management.Automation;

    /// <summary>
    /// Handles formatting for Splunk XML streaming
    /// </summary>
    [Cmdlet(VerbsData.ConvertTo, "SplunkEventXml")]
    public class ConvertToSplunkEventXmlCommand : PSCmdlet
    {
        /// <summary>
        /// Gets or sets the stanza name for the &lt;event&gt; output.
        /// </summary>
        /// <returns>The stanza name</returns>
        [Parameter]
        public string Stanza { get; set; }

        /// <summary>
        /// Gets or sets the InputObject to be output
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        /// <summary>
        /// Gets or sets the list of properties that we want to output
        /// </summary>
        /// <value>The property names.</value>
        [Parameter(Position = 1)]
        public HashSet<string> Property { get; set; }

        /// <summary>
        /// Gets or sets the value of AsXml to control whether the output is wrapped in event tags.
        /// </summary>
        [Parameter]
        public SwitchParameter AsXml { get; set; }

        protected override void BeginProcessing()
        {
            if (string.IsNullOrEmpty(Stanza))
            {
                Stanza = (string)GetVariableValue("SplunkStanzaName");
            }
            base.BeginProcessing();
        }

        /// <summary>
        /// Processes the record (get's called once for each object in the pipeline output).
        /// </summary>
        protected override void ProcessRecord()
        {
            var output = this.AsXml ? 
                XmlFormatter.ConvertToXml(this.InputObject, this.Stanza, this.Property) : 
                XmlFormatter.ConvertToString(this.InputObject, this.Property, false);

            if (AsXml)
            {
                var psOutput = new PSObject(output);
                psOutput.Properties.Add(new PSNoteProperty("SplunkPreFormatted", true));
                this.WriteObject(psOutput);
            }
            else this.WriteObject(output);


            base.ProcessRecord();
        }
    }
}
