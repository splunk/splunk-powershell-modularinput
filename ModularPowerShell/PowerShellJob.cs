// <copyright file="PowerShellJob.cs" company="Splunk">
//   Copyright (c) Joel Bennett 2012 - 2012
// </copyright>
// <summary>
//   Defines the PowerShellJob.cs for ModularPowerShell in ModularPowerShell
// </summary>
namespace Splunk.ModularInputs
{
    using System;
    using System.IO;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Reflection;

    using Quartz;

    /// <summary>
    /// Defines a Quartz Job that executes PowerShell scripts
    /// </summary>
    [DisallowConcurrentExecution]
    public class PowerShellJob : IJob
    {
        static readonly InitialSessionState Iss = InitialSessionState.CreateDefault();

        static PowerShellJob()
        {
            Assembly mps = Assembly.GetEntryAssembly();
            string path = Path.GetDirectoryName(mps.Location);
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                path = Path.Combine(path, "Modules");
                if (Directory.Exists(path))
                {
                    Iss.ImportPSModulesFromPath(path);
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
            // Force load any Cmdlets that are in this assembly automatically.
            foreach (var t in mps.GetTypes())
            {
                var cmdlets = t.GetCustomAttributes(typeof(CmdletAttribute), false) as CmdletAttribute[];
                if (cmdlets != null)
                {
                    foreach (CmdletAttribute cmdlet in cmdlets)
                    {
                        Iss.Commands.Add(new SessionStateCmdletEntry(
                                            string.Format("{0}-{1}", cmdlet.VerbName, cmdlet.NounName), t,
                                            string.Format("{0}.xml", t.Name)));
                    }
                }
            }
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
                throw new JobExecutionException("Failed to execute PowerShell Script", ex);
            }
        }
    }
}