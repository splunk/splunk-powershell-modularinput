using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModularPowerShell
{
    using System.Collections.ObjectModel;
    using System.Management.Automation;

    /// <summary>
    /// The PowerShell invoker.
    /// </summary>
    public class PowerShellInvoker
    {
        /// <summary>
        /// The invoke script.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        public void InvokeScript(string path)
        {
            var ps = PowerShell.Create(RunspaceMode.NewRunspace);
            ps.AddScript(path);            
            this.WriteOutput(ps.Invoke());
        }

        /// <summary>
        /// The write output.
        /// </summary>
        /// <param name="outputCollection">
        /// The invoke.
        /// </param>
        private void WriteOutput(Collection<PSObject> outputCollection)
        {
            foreach (var output in outputCollection)
            {
                foreach (var prop in output.Properties)
                {
                    string.Format("{0}={1}", prop.Name, prop.Value);
                }
            }
        }

    }
}
