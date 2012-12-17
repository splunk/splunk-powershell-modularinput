namespace Splunk.ModularInputs
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Xml.Serialization;

    /// <summary>
    /// Defines a wrapper class for event serialization for splunk streaming
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
    [XmlRoot("event")]
    [Serializable]
    public class Event
    {
        /// <summary>
        /// The timestamp for this event.
        /// </summary>
        private DateTime time;

        /// <summary>
        /// Gets or sets the stanza.
        /// </summary>
        [XmlAttribute("stanza")]
        public string Stanza { get; set; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        [XmlElement("data")]
        public string Data { get; set; }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        [XmlElement("source")]
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        [XmlElement("host")]
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the source type.
        /// </summary>
        [XmlElement("sourcetype")]
        public string SourceType { get; set; }

        /// <summary>
        /// Gets or sets the time.
        /// </summary>
        [XmlElement("time")]
        public DateTime Time
        {
            get
            {
                if (this.time == default(DateTime))
                {
                    this.time = DateTime.UtcNow;
                }

                return this.time;
            }

            set
            {
                this.time = value.ToUniversalTime();
            }
        }
    }
}