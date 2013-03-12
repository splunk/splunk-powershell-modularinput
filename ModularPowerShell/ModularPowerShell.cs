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

            // Set environment variables:
            XElement setting;
            if ((setting = input.Element("server_host")) != null)
            {
                Environment.SetEnvironmentVariable("SPLUNKPS_SERVER_HOST", setting.Value);
            }

            if ((setting = input.Element("server_uri")) != null)
            {
                Environment.SetEnvironmentVariable("SPLUNKPS_SERVER_URI", setting.Value);
            }

            if ((setting = input.Element("session_key")) != null)
            {
                Environment.SetEnvironmentVariable("SPLUNKPS_SESSION_KEY", setting.Value);
            }

            if ((setting = input.Element("checkpoint_dir")) != null)
            {
                Environment.SetEnvironmentVariable("SPLUNKPS_CHECKPOINT_DIR", setting.Value);
            }

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

            foreach (XElement stanza in inputXml.Descendants("stanza"))
            {
                try
                {
                    var job = JobBuilder.Create<PowerShellJob>()
                                        .UsingJobData("script", stanza.GetParameterValue("script"))
                                        .UsingJobData("ILogger", typeof(ConsoleLogger).AssemblyQualifiedName)
                                        .UsingJobData("PSModulePath", psModulePath)
                                        .WithIdentity(stanza.Attribute("name").Value)
                                        .Build();
                    var trigger = TriggerBuilder.Create()
                                                .WithSchedule(CronScheduleBuilder.CronSchedule(stanza.GetParameterValue("schedule")))
                                                .StartNow()
                                                .Build();
                    parsedJobs.Add(job, new Quartz.Collection.HashSet<ITrigger>(new[] { trigger }));
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
    }
}