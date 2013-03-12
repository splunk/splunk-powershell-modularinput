// ***********************************************************************
// Assembly         : ModularPowerShell
// Author           : Joel Bennett
// Created          : 03-11-2013
//
// Last Modified By : Joel Bennett
// Last Modified On : 03-11-2013
// ***********************************************************************
// <copyright file="PowerShell.cs" company="Splunk">
//     Copyright © 2013 by Splunk Inc., all rights reserved
// </copyright>
// <summary>
//     The entry point for PowerShell.exe
// </summary>
// ***********************************************************************
namespace Splunk.ModularInputs
{
    using System;
    using System.Xml.Linq;

    using Splunk.ModularInputs.Properties;
    using Splunk.ModularInputs.Serialization;

    /// <summary>
    /// The PowerShell Modular Input Program.
    /// </summary>
    public class PowerShell
    {
        /// <summary>
        /// The usage string for errors
        /// </summary>
        private const string Usage = "Invalid Arguments. Valid Invocation are:\n"
                                     + "  PowerShell.exe --validate_arguments\n"
                                     + "  PowerShell.exe --scheme\n"
                                     + "  PowerShell.exe\n";

        /// <summary>
        /// The main entry point for the PowerShell Modular Input
        /// </summary>
        /// <param name="args">The arguments</param>
        public static void Main(string[] args)
        {
            // log our command line
            Console.Error.WriteLine("INFO: PowerShell.exe " + string.Join(" ", args));

            // configure the logger
            XmlFormatter.LogOutputErrors = Settings.Default.LogOutputErrors;
            XmlFormatter.OutputBlanksOnError = Settings.Default.OutputBlanksOnError;
            XmlFormatter.Logger = new ConsoleLogger();

            XDocument document = null;
            XElement input = null;
            if (args.Length > 0)
            {
                if (args[0].ToLower().Equals("--scheme"))
                {
                    WriteScheme();
                    Environment.Exit(0);
                }
                else if (args[0].ToLowerInvariant().Equals("--validate_arguments"))
                {
                    Console.Error.Write( "ERROR: --validate_arguments not implemented yet");
                    Environment.Exit(1);
                }
                else if (args[0].ToLowerInvariant().Equals("--input") && args.Length == 2)
                {
                    // Logger.WriteLog(LogLevel.Info, "Reading InputDefinition from parameter for testing");
                    document = XDocument.Load(args[1]);
                }
                else
                {
                    Console.Error.Write("ERROR: " + Usage);
                    Environment.Exit(2);
                }
            }
            else
            {
                document = XDocument.Parse(Console.In.ReadToEnd());
            }


            try
            {
                Console.Error.Write("DEBUG: " + document);
                input = document.Element("input");
                if (input == null)
                {
                    Console.Error.Write("ERROR: input is not valid input xml");
                    Environment.Exit(6);
                }
            }
            catch (Exception ex)
            {
                Console.Error.Write("ERROR: input is not valid input xml: " + ex.Message);
                Environment.Exit(6);
            }

            var self = new ModularPowerShell(input, XmlFormatter.Logger);

            self.StartScheduler();
        }

        /// <summary>
        /// Writes the endpoint scheme xml for splunk.
        /// </summary>
        private static void WriteScheme()
        {
            // ConvertToSplunkEventXmlCommand.Write(LogLevel.INFO, "Dumping Scheme to STDOUT");

            // Write out the XML
            Console.WriteLine(
                new XDocument(
                    new XElement(
                        "scheme",
                        new XElement("title", "PowerShell Scripts"),
                        new XElement("description", "Handles executing PowerShell scripts with parameters as inputs"),
                        new XElement("streaming_mode", "xml"),
                        new XElement("use_single_instance", "true"),
                        new XElement(
                            "endpoint",
                            new XElement(
                                "args",
                                new XElement(
                                    "arg",
                                    new XAttribute("name", "name"),
                                    new XElement("title", "Input Name"),
                                    new XElement("description", "A unique name for this PowerShell input.")),
                                new XElement(
                                    "arg",
                                    new XAttribute("name", "script"),
                                    new XElement("title", "Command or Script Path"),
                                    new XElement("description", "A powershell command-line, script, or the full path to a script.")),
                                new XElement(
                                    "arg",
                                    new XAttribute("name", "schedule"),
                                    new XElement("title", "Cron Schedule"),
                                    new XElement("description", "A cron string specifying the schedule for execution.")))))));

            Environment.Exit(0);
        }
    }
}