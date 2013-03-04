// <copyright file="PowerShellJob.cs" company="Splunk">
//   Copyright (c) Joel Bennett 2012 - 2012
// </copyright>
// <summary>
//   Defines the PowerShellJob.cs for ModularPowerShell in ModularPowerShell
// </summary>
namespace Splunk.ModularInputs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Reflection;

    using Quartz;

    public static class SessionStateHelper
    {
        /// <summary>
        /// Loads any cmdlets defined in the specified types
        /// </summary>
        /// <param name="iss">The InitialSessionState.</param>
        /// <param name="types">The types which might be cmdlets.</param>
        /// <returns>The InitialSessionState with the commands added.</returns>
        public static InitialSessionState LoadCmdlets(this InitialSessionState iss, IEnumerable<Type> types)
        {
            foreach (var t in types)
            {
                var cmdlets = t.GetCustomAttributes(typeof(CmdletAttribute), false) as CmdletAttribute[];
                if (cmdlets != null)
                {
                    foreach (CmdletAttribute cmdlet in cmdlets)
                    {
                        iss.Commands.Add(
                            new SessionStateCmdletEntry(
                                string.Format("{0}-{1}", cmdlet.VerbName, cmdlet.NounName), t, string.Format("{0}.xml", t.Name)));
                    }
                }
            }
            return iss;
        }

        /// <summary>
        /// Loads any cmdlets defined in the specified assembly
        /// </summary>
        /// <param name="iss">The InitialSessionState.</param>
        /// <param name="assembly">The assembly which contains cmdlets.</param>
        /// <returns>The InitialSessionState with the commands added.</returns>
        public static InitialSessionState LoadCmdlets(this InitialSessionState iss, Assembly assembly)
        {
            return LoadCmdlets(iss, assembly.GetTypes());
        }

        /// <summary>
        /// Loads any modules defined in the specified path
        /// </summary>
        /// <param name="iss">The InitialSessionState.</param>
        /// <param name="path">The path.</param>
        /// <returns>The InitialSessionState with the modules added.</returns>
        public static InitialSessionState LoadModules(this InitialSessionState iss, string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                path = Path.Combine(path, "Modules");
                if (Directory.Exists(path))
                {
                    iss.ImportPSModulesFromPath(path);
                }
                else
                {
                    SplunkXmlFormatter.WriteLog(LogLevel.Error, "Module path does not exist: '" + path + "'");
                }
            }
            else
            {
                SplunkXmlFormatter.WriteLog(LogLevel.Error, "Module path does not exist: '" + path + "'");
            }

            return iss;
        }

    }

    /// <summary>
    /// Defines a Quartz Job that executes PowerShell scripts
    /// </summary>
    [DisallowConcurrentExecution]
    public class PowerShellJob : IJob
    {
        private static readonly InitialSessionState Iss;

        /// <summary>
        /// Initializes static members of the <see cref="PowerShellJob"/> class.
        /// </summary>
        static PowerShellJob()
        {
            Iss = ConfigureSessionState();
        }

        /// <summary>
        /// Configures the initial state of the sessions.
        /// </summary>
        /// <returns>InitialSessionState.</returns>
        private static InitialSessionState ConfigureSessionState()
        {
            Assembly mps = Assembly.GetEntryAssembly();

            string path = Path.GetDirectoryName(mps.Location);

            return InitialSessionState.CreateDefault().LoadModules(path).LoadCmdlets(mps);
        }

        /// <summary>
        /// Executes the job
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        public void Execute(IJobExecutionContext context)
        {
            var data = context.JobDetail.JobDataMap;

            try
            {
                // Logging FYI:
                SplunkXmlFormatter.WriteLog(string.Format("--- Stanza: {0} ---", context.JobDetail.Key.Name));
                Environment.SetEnvironmentVariable("SPLUNKPS_INPUT_NAME", context.JobDetail.Key.Name);
                var ps = PowerShell.Create(Iss);
                ////ps.RunspacePool = rsp;
                var command = data.GetString("script");
                if (command != null)
                {
                    ps = ps.AddScript(command);

                    // TODO: handle scheduling instead of just executing everything
                    ////from p in stanza.Descendants("param") select new KeyValuePair<string,string>( p.Attributes("name"), p.Value );
                    // Write the command output to splunk xml
                    SplunkXmlFormatter.WriteOutput(ps.Invoke(), context.JobDetail.Key.Name);

                    // Write out any errors from invoking the script
                    if (ps.HadErrors)
                    {
                        foreach (var error in ps.Streams.Error)
                        {
                            string format = string.Format(
                                "{0}: ({1}:{2}):[{3}], {4}",
                                error.CategoryInfo.Category,
                                error.CategoryInfo.TargetName,
                                error.CategoryInfo.TargetType,
                                error.CategoryInfo.Activity,
                                error.CategoryInfo.Reason);

                            string msg = command + " "
                                         + (error.ErrorDetails != null
                                                ? error.ErrorDetails.Message
                                                : error.Exception.Message);

                            SplunkXmlFormatter.WriteLog(LogLevel.Error, msg);
                            SplunkXmlFormatter.WriteLog(LogLevel.Error, format);
                        }
                    }
                }
                else
                {
                    SplunkXmlFormatter.WriteLog(LogLevel.Error, "Missing 'script' parameter.");
                }
            }
            catch (Exception ex)
            {
                SplunkXmlFormatter.WriteLog(LogLevel.Error, "PowerShell Error:\r\n" + ex.Message);
                throw new JobExecutionException("Failed to execute PowerShell Script", ex);
            }
        }
    }
}