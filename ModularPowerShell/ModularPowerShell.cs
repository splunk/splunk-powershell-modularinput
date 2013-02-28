// ***********************************************************************
// Assembly         : ModularPowerShell
// Author           : Joel Bennett
// Created          : 12-06-2012
//
// Last Modified By : Joel Bennett
// Last Modified On : 12-18-2012
// ***********************************************************************
// <copyright file="ModularPowerShell.cs" company="Splunk">
//     Copyright (c) Splunk. All rights reserved.
// </copyright>
// <summary>Defines the ModularPowerShell program</summary>
// ***********************************************************************

namespace Splunk.ModularInputs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using Quartz;
    using Quartz.Impl;

    using Splunk.ModularInputs.Properties;

    /// <summary>
    /// The PowerShell Modular Input Program.
    /// </summary>
    public class ModularPowerShell
    {
        /// <summary>
        /// The usage string for errors
        /// </summary>
        private const string Usage = "Invalid Arguments. Valid Invocation are:\n"
                                     + "  ps.exe --validate_arguments\n"
                                     + "  ps.exe --scheme\n"
                                     + "  ps.exe\n";

        /// <summary>
        /// The main entry point for the PowerShell Modular Input
        /// </summary>
        /// <param name="args">The arguments</param>
        public static void Main(string[] args)
        {
            // configure the logger
            SplunkXmlFormatter.LogOutputErrors = Settings.Default.LogOutputErrors;
            SplunkXmlFormatter.OutputBlanksOnError = Settings.Default.OutputBlanksOnError;
            // log our command line
            SplunkXmlFormatter.WriteLog(LogLevel.Info, string.Format("PowerShell.exe {0}", string.Join(" ", args)));

            XElement input = null;
            try
            {
                input = ReadInput(args);
            }
            catch (Exception ex)
            {
                SplunkXmlFormatter.WriteLog(LogLevel.Error, "Failed to parse inputs");
                SplunkXmlFormatter.WriteLog(LogLevel.Error, ex.Message);
                if (ex.InnerException != null)
                {
                    SplunkXmlFormatter.WriteLog(LogLevel.Error, ex.InnerException.Message);                   
                }

                Environment.Exit(4);
            }

            // Set environment variables:
// ReSharper disable PossibleNullReferenceException
            Environment.SetEnvironmentVariable("SPLUNKPS_SERVER_HOST", input.Element("server_host").Value);
            Environment.SetEnvironmentVariable("SPLUNKPS_SERVER_URI", input.Element("server_uri").Value);
            Environment.SetEnvironmentVariable("SPLUNKPS_SESSION_KEY", input.Element("session_key").Value);
            Environment.SetEnvironmentVariable("SPLUNKPS_CHECKPOINT_DIR", input.Element("checkpoint_dir").Value);
// ReSharper restore PossibleNullReferenceException

            // Initialize output
            Console.Out.WriteLine("<stream>");

            var scheduler = StdSchedulerFactory.GetDefaultScheduler();
            scheduler.Start();
            try {
                var jobs = (from stanza in input.Descendants("stanza")
                            let job =
                                JobBuilder.Create<PowerShellJob>()
                                          .UsingJobData("script", stanza.GetParameterValue("script"))
                                          .WithIdentity(stanza.Attribute("name").Value)
                                          .Build()
                            let trigger =
                                TriggerBuilder.Create()
                                              .WithSchedule(
                                                  CronScheduleBuilder.CronSchedule(stanza.GetParameterValue("schedule")))
                                              .StartNow()
                                              .Build()
                            select new { job, trigger }).ToDictionary(k => k.job, v => (IList<ITrigger>)new[] { v.trigger });

                scheduler.ScheduleJobs(jobs, true);
            }
            catch (Exception ex)
            {
                SplunkXmlFormatter.WriteLog(LogLevel.Error, "Failed to schedule jobs in Quartz");
                SplunkXmlFormatter.WriteLog(LogLevel.Error, ex.Message);
                if (ex.InnerException != null)
                {
                    SplunkXmlFormatter.WriteLog(LogLevel.Error, ex.InnerException.Message);
                }

                Environment.Exit(5);
            }

            // TODO: Finalize output
            // Console.Out.WriteLine("</stream>");
            SplunkXmlFormatter.WriteLog("Finished InputDefinition");
        }

        /// <summary>
        /// Handle arguments and read input stream.
        /// </summary>
        /// <param name="args">The arguments passed to the program.</param>
        /// <returns>The input document</returns>
        /// <exception cref="System.IO.InvalidDataException">input is not valid input xml</exception>
        private static XElement ReadInput(string[] args)
        {
            // <input>
            //  <server_host>myHost</server_host>
            //  <server_uri>https://127.0.0.1:8089</server_uri>
            //  <session_key>123102983109283019283</session_key>
            //  <checkpoint_dir>/opt/splunk/var/lib/splunk/modinputs</checkpoint_dir>
            //  <configuration>
            //    <stanza name="myScheme://aaa">
            //        <param name="param1">value1</param>
            //        <param name="param2">value2</param>
            //        <param name="disabled">0</param>
            //        <param name="index">default</param>
            //    </stanza>
            //    <stanza name="myScheme://bbb">
            //        <param name="param1">value1</param>
            //        <param name="param2">value2</param>
            //        <param name="disabled">0</param>
            //        <param name="index">default</param>
            //    </stanza>
            //  </configuration>
            // </input>
            XDocument input = null;

            if (args.Length > 0)
            {
                if (args[0].ToLower().Equals("--scheme"))
                {
                    WriteScheme();
                    Environment.Exit(0);
                }
                else if (args[0].ToLowerInvariant().Equals("--validate_arguments"))
                {
                    SplunkXmlFormatter.WriteLog(LogLevel.Error, "--validate_arguments not implemented yet");
                    Environment.Exit(1);
                }
                else if (args[0].ToLowerInvariant().Equals("--input") && args.Length == 2)
                {
                    SplunkXmlFormatter.WriteLog("Reading InputDefinition from parameter for testing");
                    input = XDocument.Load(args[1]);
                }
                else
                {
                    SplunkXmlFormatter.WriteLog(LogLevel.Error, Usage);
                    Environment.Exit(2);
                }
            }
            else
            {
                SplunkXmlFormatter.WriteLog("Reading InputDefinition");
                input = XDocument.Parse(Console.In.ReadToEnd());
            }

            XElement id; 

            try
            {
                id = input.Element("input");
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("input is not valid input xml", ex);
            }

            if (id == null)
            {
                throw new InvalidDataException("input is not valid input xml");
            }

            SplunkXmlFormatter.WriteLog(LogLevel.Info, id.ToString());
            return id;
        }

        /// <summary>
        /// Writes the endpoint scheme xml for splunk.
        /// </summary>
        private static void WriteScheme()
        {
            // SplunkXmlFormatter.Write(LogLevel.INFO, "Dumping Scheme to STDOUT");

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