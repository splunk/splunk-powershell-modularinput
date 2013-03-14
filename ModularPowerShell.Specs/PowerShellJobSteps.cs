using System;
using TechTalk.SpecFlow;

namespace ModularPowerShell.Specs
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Management.Automation;

    using Splunk.ModularInputs;
    using Splunk.ModularInputs.Serialization;

    using Xunit;

    [Binding]
    public class PowerShellJobSteps : Steps
    {
        [Given(@"I add ReadOnly variables:")]
        public void GivenIAddReadOnlyVariables(Table table)
        {
            var variables = table.Rows.Select(row => new Tuple<string, string, string>(row["Variable"], row["Value"], row["Description"]));
            PowerShellJob.AddReadOnlyVariables(variables);
        }

        [Given(@"I have a PowerShell job")]
        public void GivenIHaveAPowerShellJob()
        {
            var powerShellJob = new PowerShellJob
            {
                Logger = ScenarioContext.Current.Get<ILogger>("OutputLog")
            };
            ScenarioContext.Current.Add("PowerShellJob", powerShellJob);
        }

        [Given(@"I have an Object Logger")]
        public void GivenIHaveAnObjectLogger()
        {
            var objectLogger = new ObjectLogger();
            ScenarioContext.Current.Add("OutputLog", objectLogger);
        }

        [Given(@"I have a PowerShell Job with an Object Logger")]
        public void GivenIHaveAPowerShellJobWithAnObjectLogger()
        {
            Given("I have an Object Logger");
            Given("I have a PowerShell job");
        }

        [Given(@"my script is ""(.*)""")]
        public void GivenMyScriptIs(string script)
        {
            ScenarioContext.Current.Add("PowerShellScript", script);
        }

        [Then(@"the object output should be a string ""(.*)""")]
        public void ThenTheObjectOutputShouldBeAString(string value)
        {
            var output = ScenarioContext.Current.Get<ObjectLogger>("OutputLog").Output["Test"].Select( pso => ((PSObject)pso).BaseObject ).ToArray();
            Assert.Contains(value, output);
            Assert.Equal(1, output.Length);
        }
    }
}
