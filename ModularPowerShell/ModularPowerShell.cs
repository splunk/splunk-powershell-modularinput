// ***********************************************************************
// Assembly         : ModularPowerShell
// Author           : Joel Bennett
// Created          : 12-06-2012
//
// Last Modified By : Joel Bennett
// Last Modified On : 12-18-2012
// ***********************************************************************
// <copyright file="ModularPowerShell.cs" company="Splunk">
//     Copyright © 2013 by Splunk Inc., all rights reserved
// </copyright>
// <summary>Defines the ModularPowerShell program</summary>
// ***********************************************************************

namespace Splunk.ModularInputs
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;

    using Quartz;
    using Quartz.Impl;

    using Splunk.ModularInputs.Serialization;

    public class ModularPowerShell
    {
        public ModularPowerShell(XElement input, ILogger logger)
        {
            this.Logger = logger;

            // Initialize output
            this.Logger.WriteLog(LogLevel.Output, "<stream>");

            this.Jobs = this.ParseJobs(input);

            // TODO: Finalize output
            // Console.Out.WriteLine("</stream>");
            this.Logger.WriteLog(LogLevel.Info, "Finished InputDefinition");            
        }

        /// <summary>
        /// Gets or Sets the logger used for writing any output
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the jobs to be executed
        /// </summary>
        /// <value>The jobs and their triggers.</value>
        public Dictionary<IJobDetail, Quartz.Collection.ISet<ITrigger>> Jobs { get; set; }

        /// <summary>
        /// Starts the scheduler with the configured jobs.
        /// </summary>
        public void StartScheduler()
        {
            try
            {
                var scheduler = StdSchedulerFactory.GetDefaultScheduler();
                scheduler.Start();
                scheduler.ScheduleJobs(this.Jobs, true);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(LogLevel.Fatal, "Failed to start Quartz job scheduler");
                Logger.WriteLog(LogLevel.Error, ex.Message);
                if (ex.InnerException != null)
                {
                    Logger.WriteLog(LogLevel.Error, ex.InnerException.Message);
                }
            }
        }

        /// <summary>
        /// Parses the job stanzas from the input xml.
        /// </summary>
        /// <param name="inputXml">The input XML.</param>
        /// <returns>The dictionary of jobs with their parsed schedules.</returns>
        public Dictionary<IJobDetail, Quartz.Collection.ISet<ITrigger>> ParseJobs(XElement inputXml)
        {
            var parsedJobs = new Dictionary<IJobDetail, Quartz.Collection.ISet<ITrigger>>();

            // Workaround a bug in PowerShell which voids the PSModulePath
            var psModulePath = Environment.GetEnvironmentVariable("PSModulePath");

            AddGlobalVariables(inputXml);

            foreach (XElement stanza in inputXml.Descendants("stanza"))
            {
                try
                {
                    // get the hostname part of the powershell://stanza
                    var name = stanza.Attribute("name").Value;
                    var uri = new Uri(name);
                    name = uri.Host;

                    var job = JobBuilder.Create<PowerShellJob>()
                                        .UsingJobData("script", stanza.GetParameterValue("script"))
                                        .UsingJobData("ILogger", typeof(ConsoleLogger).AssemblyQualifiedName)
                                        .UsingJobData("PSModulePath", psModulePath)
                                        .UsingJobData("SplunkStanzaName", name)
                                        .WithIdentity( name );

                    var trigger = TriggerBuilder.Create()
                                                .WithSchedule(CronScheduleBuilder.CronSchedule(stanza.GetParameterValue("schedule")))
                                                .StartNow()
                                                .Build();
                    parsedJobs.Add(job.Build(), new Quartz.Collection.HashSet<ITrigger>(new[] { trigger }));
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(
                        LogLevel.Fatal, "Failed to parse stanza {0}\n{1}", stanza.Attribute("name").Value, ex.Message);

                    if (ex.InnerException != null)
                    {
                        Logger.WriteLog(LogLevel.Error, "Inner Exception: {0}", ex.InnerException.Message);
                    }
                }
            }

            return parsedJobs;
        }

        /// <summary>
        /// Loads the global settings from the input xml.
        /// </summary>
        /// <param name="inputXml">The input XML.</param>
        /// <returns>StringDictionary.</returns>
        private static void AddGlobalVariables(XContainer inputXml)
        {
            var splunkSettings = new List<Tuple<string,string,string>>();
            XElement setting;

            if ((setting = inputXml.Element("server_host")) != null)
            {
                splunkSettings.Add(new Tuple<string, string, string>("SplunkServerHost",setting.Value, "The Splunk server hostname"));
            }

            if ((setting = inputXml.Element("server_uri")) != null)
            {
                splunkSettings.Add(new Tuple<string, string, string>("SplunkServerUri",setting.Value, "The Splunk server REST uri"));
            }

            if ((setting = inputXml.Element("session_key")) != null)
            {
                splunkSettings.Add(new Tuple<string, string, string>("SplunkSessionKey",setting.Value, "The Splunk REST API key"));
            }

            if ((setting = inputXml.Element("checkpoint_dir")) != null)
            {
                splunkSettings.Add(new Tuple<string, string, string>("SplunkCheckpointPath",setting.Value, "The path for storing persistent state"));
            }

            splunkSettings.Add(new Tuple<string, string, string>("SplunkHome", Environment.GetEnvironmentVariable("SPLUNK_HOME"), "The Splunk install root"));
            splunkSettings.Add(new Tuple<string, string, string>("SplunkServerName", Environment.GetEnvironmentVariable("SPLUNK_SERVER_NAME"), "The registered name of this Splunk server"));
            PowerShellJob.AddReadOnlyVariables(splunkSettings);
        }
    }
}