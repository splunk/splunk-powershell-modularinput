
namespace ModularPowerShell
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using Splunk.ModularInputs;

    /// <summary>
    /// The PowerShell Modular Input Program.
    /// </summary>
    public class Program
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
        /// <param name="args">
        /// The arguments
        /// </param>
        public static void Main(string[] args)
        {
            SplunkXmlFormatter.Write(LogLevel.Info, string.Format("powershell.exe {0}", string.Join(" ",args)));

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
                    SplunkXmlFormatter.Write(LogLevel.Error, "--validate_arguments not implemented yet");
                    Environment.Exit(1);
                }
                else if (args[0].ToLowerInvariant().Equals("--input") && args.Length == 2)
                {
                    input = XDocument.Load(args[1]);
                }
                else
                {
                    SplunkXmlFormatter.Write(LogLevel.Error, Usage);
                    Environment.Exit(2);
                }
            }
            else
            {
                input = XDocument.Parse(Console.In.ReadToEnd());
            }

                SplunkXmlFormatter.Write("Reading InputDefinition");

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
                XElement id = null;
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

                SplunkXmlFormatter.Write(LogLevel.Info, id.ToString());

                // Set environment variables:
                Environment.SetEnvironmentVariable("SPLUNKPS_SERVER_HOST", id.Element("server_host").Value);
                Environment.SetEnvironmentVariable("SPLUNKPS_SERVER_URI", id.Element("server_uri").Value);
                Environment.SetEnvironmentVariable("SPLUNKPS_SESSION_KEY", id.Element("session_key").Value);
                Environment.SetEnvironmentVariable("SPLUNKPS_CHECKPOINT_DIR", id.Element("checkpoint_dir").Value);


                // Logging FYI
                SplunkXmlFormatter.Write(string.Format("Stanzas: {0}", id.Descendants("stanza").Count()));
                SplunkXmlFormatter.Write(string.Format("CWD: {0}", Environment.CurrentDirectory));
                SplunkXmlFormatter.Write(string.Format("command: {0}", Environment.CommandLine));

                // Create a pool of runspaces.
                ////using (RunspacePool rsp = RunspaceFactory.CreateRunspacePool(Settings.Default.MinRunspaces, Settings.Default.MaxRunspaces))
                ////{
                ////    rsp.Open();

                // Initialize output
                Console.Out.WriteLine("<stream>");

                foreach (var stanza in id.Descendants("stanza"))
                {
                    InvokeStanza(stanza);
                }

                // Finalize output
                Console.Out.WriteLine("</stream>");
                ////   rsp.Close();
                ////}

                SplunkXmlFormatter.Write("Finished InputDefinition");
                Environment.Exit(0);
        }

        /// <summary>
        /// Invokes the PowerShell script in a given stanza
        /// </summary>
        /// <param name="stanza">
        /// The stanza.
        /// </param>
        private static void InvokeStanza(XElement stanza)
        {
            // Logging FYI:
            SplunkXmlFormatter.Write(string.Format("--- Stanza: {0} ---", stanza.Attribute("name").Value));

            var ps = PowerShell.Create();
            ////ps.RunspacePool = rsp;
            var command = stanza.Descendants("param").FirstOrDefault(p => p.Attribute("name").Value == "script");
            if (command != null)
            {
                ps = ps.AddScript(command.Value);

                // TODO: handle scheduling instead of just executing everything
                ////from p in stanza.Descendants("param") select new KeyValuePair<string,string>( p.Attributes("name"), p.Value );
                // Write the command output to splunk xml
                SplunkXmlFormatter.WriteOutput(ps.Invoke(), stanza.Attribute("name").Value);

                // Write out any errorrs from invoking the script
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

                        string msg = command.Value + " " + (error.ErrorDetails != null ? error.ErrorDetails.Message : error.Exception.Message);

                        SplunkXmlFormatter.Write(LogLevel.Error, msg);
                        SplunkXmlFormatter.Write(LogLevel.Error, format);
                    }
                }
            }
            else
            {
                SplunkXmlFormatter.Write(LogLevel.Error, "Missing 'script' parameter.");
            }
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
                                    new XAttribute("name", "run"),
                                    new XElement("title", "Start Schedule"),
                                    new XElement("description", "The time when the script should execute first.")))))));

            Environment.Exit(0);
        }
    }
}