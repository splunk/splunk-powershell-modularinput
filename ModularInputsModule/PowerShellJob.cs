// ***********************************************************************
// Assembly         : ModularInputsModule
// Author           : Joel Bennett
// Created          : 03-06-2013
//
// Last Modified By : Joel Bennett
// Last Modified On : 03-07-2013
// ***********************************************************************
// <copyright file="PowerShellJob.cs" company="Splunk">
//     Copyright © 2013 by Splunk Inc., all rights reserved
// </copyright>
// <summary>
//   Defines the PowerShellJob.cs for ModularPowerShell in ModularPowerShell
// </summary>
// ***********************************************************************
namespace Splunk.ModularInputs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Reflection;

    using Microsoft.Practices.Unity;

    using Common.Logging;

    using Quartz;

    using Splunk.ModularInputs.Serialization;

    /// <summary>
    /// Defines a Quartz Job that executes PowerShell scripts
    /// </summary>
    [DisallowConcurrentExecution]
    public class PowerShellJob : IJob
    {
        /// <summary>
        /// Initializes static members of the <see cref="PowerShellJob"/> class.
        /// </summary>
        static PowerShellJob()
        {
            Iss = ConfigureSessionState();
        }

        /// <summary>
        /// Gets or sets the logger used for job output
        /// </summary>
        /// <value>The logger.</value>
        [Dependency]
        public ILogger Logger { get; set; }

        /// <summary>
        /// The shared initial session state (we'll preload modules, etc).
        /// </summary>
        public static InitialSessionState Iss { get; private set; }

        /// <summary>
        /// Configures the initial state of the sessions.
        /// </summary>
        /// <returns>A configured InitialSessionState with modules and cmdlets loaded.</returns>
        private static InitialSessionState ConfigureSessionState()
        {
            var iss = InitialSessionState.CreateDefault();

            Assembly mps = Assembly.GetExecutingAssembly(); // .GetEntryAssembly()
            string path = Path.GetDirectoryName(mps.Location);
            if (!string.IsNullOrWhiteSpace(path))
            {
                iss.ImportPSModulesFromPath(Path.Combine(path, "Modules"));
            }
            return iss.LoadCmdlets(mps);
        }

        /// <summary>
        /// Adds read only variables to the shared initial session state.
        /// </summary>
        /// <param name="values">A collection of string tuples containing the name, value, and description of the variables to be added.</param>
        public static void AddReadOnlyVariables(IEnumerable<Tuple<string, string, string>> values)
        {
            foreach (var variable in values)
            {
                Iss.Variables.Add(
                    new SessionStateVariableEntry(
                        variable.Item1,
                        variable.Item2,
                        variable.Item3,
                        ScopedItemOptions.Constant & ScopedItemOptions.ReadOnly));
            }
        }

        /// <summary>
        /// Gets a concrete instance of an ILogger.
        /// </summary>
        /// <param name="typeName">The AssemblyQualifiedName of a type that implements ILogger.</param>
        public void SetLogger(string typeName)
        {
            if (typeName != null)
            {
                var type = Type.GetType(typeName);
                if (type != null)
                {
                    // ReSharper disable EmptyGeneralCatchClause
                    try
                    {
                        var container = new UnityContainer().RegisterType(typeof(ILogger), type);
                        Logger = container.Resolve<ILogger>();
                    }
                    catch { }
                    // ReSharper restore EmptyGeneralCatchClause
                }
            }
            Logger = new ConsoleLogger();
        }

        /// <summary>
        /// Executes the job
        /// </summary>
        /// <param name="context">
        /// The execution context, containing the work to be done (including the PowerShell script)
        /// </param>
        public void Execute(IJobExecutionContext context)
        {
            var data = context.JobDetail.JobDataMap;
            string command = data.GetString("script");
            string name = context.JobDetail.Key.Name;
            // just to be safe, make a copy each time
            var iss = Iss.Clone();
            try
            {
                // since it's a copy, we can just add this without removing it each time
                iss.Variables.Add(
                    new SessionStateVariableEntry(
                        "SplunkStanzaName",
                        data.GetString("SplunkStanzaName"),
                        "The name of the inputs.conf stanza that defined this script",
                        ScopedItemOptions.Constant & ScopedItemOptions.ReadOnly));


                // We're trying to inherit the dependency injection from the scheduler
                SetLogger(data.GetString("ILogger"));

                if (command == null)
                {
                    this.Logger.WriteLog(LogLevel.Error, "Missing 'script' parameter.");
                    return;
                }

                // Logging FYI:
                this.Logger.WriteLog(LogLevel.Info, string.Format("--- Stanza: {0} ---", name));

                // Environment.SetEnvironmentVariable("SPLUNKPS_INPUT_NAME", name);

                // Workaround a bug in PowerShell which voids the PSModulePath
                var path = data.GetString("PSModulePath");
                if (path != null)
                {
                    Environment.SetEnvironmentVariable("PSModulePath", path);
                }

            }
            catch (Exception ex)
            {
                this.Logger.WriteLog(LogLevel.Error, "PowerShell Exception:\r\n" + ex.Message);
                throw new JobExecutionException("Failed to execute PowerShell Script", ex);
            }

            this.Execute(iss, command, name);
        }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="iss">The initial session state</param>
        /// <param name="command">The command.</param>
        /// <param name="stanzaName">The stanza name.</param>
        /// <exception cref="Quartz.JobExecutionException">Failed to execute PowerShell Script</exception>
        public void Execute(InitialSessionState iss, string command, string stanzaName)
        {
            try
            {
                // We may want to use a runspace pool? ps.RunspacePool = rsp;
                var ps = PowerShell.Create(iss);

                ps = ps.AddScript(command);

                // Write the command output to the configured logger
                this.Logger.WriteOutput(ps.Invoke(), stanzaName);

                // Write out any errors from invoking the script
                if (!ps.HadErrors)
                {
                    return;
                }

                foreach (var error in ps.Streams.Error)
                {
                    var details = error.ErrorDetails != null ? error.ErrorDetails.Message : error.Exception.Message;

                    this.Logger.WriteLog(
                        LogLevel.Error,
                        "Stanza=\"{0}\"\nSCRIPT=\"{1}\"\nCATEGORY=\"{2}\"\nTargetName=\"{3}\"\nTargetType=\"{4}\"\nActivity=\"{5}\"\nReason=\"{6}\"\nDetails=\"{7}\"\n",
                        stanzaName,
                        command,
                        error.CategoryInfo.Category,
                        error.CategoryInfo.TargetName,
                        error.CategoryInfo.TargetType,
                        error.CategoryInfo.Activity,
                        error.CategoryInfo.Reason,
                        details);
                }
            }
            catch (Exception ex)
            {
                this.Logger.WriteLog(LogLevel.Error, "PowerShell Exception:\r\n" + ex.Message);
                throw new JobExecutionException("Failed to execute PowerShell Script", ex);
            }
        }
    }
}