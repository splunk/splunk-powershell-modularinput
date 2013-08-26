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
    using System.Threading;

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
        private readonly Command defaultOutputCommand = new Command("Out-Default");

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
            // We need STA so we behave more like PS3
            iss.ApartmentState = ApartmentState.STA;

            // We need ReuseThread so that we behave, well, the way that PowerShell.exe and ISE do.
            iss.ThreadOptions = PSThreadOptions.ReuseThread;

            // We moved ModularInputsModule.dll into a subdirectory
            // So we need to use GetEntryAssembly to use the path of PowerShell.exe
            Assembly mps = Assembly.GetEntryAssembly(); // .GetExecutingAssembly();
            string path = Path.GetDirectoryName(mps.Location);
            if (!string.IsNullOrEmpty(path))
            {
                // because this must work with PowerShell2, we can't use ImportPSModulesFromPath
                path = Path.Combine(path, "Modules");
                if (!Directory.Exists(path))
                {
                    var logger = new ConsoleLogger();
                    logger.WriteLog(LogLevel.Warn, "The Modules Path '{0}' could not be found", path);
                }
                else
                {
                    iss.ImportPSModule(Directory.GetDirectories(path));
                }
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

            if (Logger == null)
            {
                Logger = new ConsoleLogger();
            }
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
                this.Logger.WriteLog(LogLevel.Debug, string.Format("Execute Stanza: {0}", name));

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
            Runspace runspace = null;
            try
            {
                // We may want to use a runspace pool? ps.RunspacePool = rsp;
                runspace = RunspaceFactory.CreateRunspace(iss);
                runspace.Open();
                var pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript(command);


                //// ToDo: a light-weight host API?
                //var runSpace = RunspaceFactory.CreateRunspace(iss);
                //Runspace.DefaultRunspace = runSpace;
                //runSpace.Open();
                //var ps = runSpace.CreatePipeline(command, false);
                //ps.Commands.Add(this.defaultOutputCommand);

                // ps = ps.AddScript(command);

                // Write the command output to the configured logger
                this.Logger.WriteOutput(pipeline.Invoke(), stanzaName);

                // Write out any errors from invoking the script

                if (pipeline.Error.Count == 0)
                {
                    return;
                }

                foreach (ErrorRecord error in pipeline.Error.ReadToEnd())
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
                runspace.Close();
            }
            catch (Exception ex)
            {
                this.Logger.WriteLog(LogLevel.Error, "PowerShell Exception:\r\n" + ex.Message);
                throw new JobExecutionException("Failed to execute PowerShell Script", ex);
            }
            finally
            {
                if (runspace != null)
                {
                    runspace.Dispose();
                }
            }
        }
    }

    public class Tuple<T1, T2, T3>
    {
        public Tuple(T1 item1, T2 item2, T3 item3)
        {
            this.Item1 = item1;
            this.Item2 = item2;
            this.Item3 = item3;
        }

        /// <summary>
        /// Gets or sets the item1.
        /// </summary>
        /// <value>The item1.</value>
        public T1 Item1 { get; set; }
        
        /// <summary>
        /// Gets or sets the item2.
        /// </summary>
        /// <value>The item2.</value>
        public T2 Item2 { get; set; }

        /// <summary>
        /// Gets or sets the item3.
        /// </summary>
        /// <value>The item3.</value>
        public T3 Item3 { get; set; }
    }
}