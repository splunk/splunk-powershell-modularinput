using System;
using TechTalk.SpecFlow;

namespace ModularPowerShell.Specs
{
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using Splunk.ModularInputs;
    using Splunk.ModularInputs.Serialization;

    using Xunit;

    [Binding]
    public class BugFixesSteps
    {
        [Given(@"I have a PowerShell job")]
        public void GivenIHaveAPowerShellJob()
        {
            var objectLogger = new ObjectLogger();
            ScenarioContext.Current.Add("OutputLog", objectLogger);
            var powerShellJob = new PowerShellJob { Logger = objectLogger };
            ScenarioContext.Current.Add("PowerShellJob", powerShellJob);
        }

        [Given(@"the script outputs nulls \(intermixed with real data\)")]
        public void GivenTheScriptOutputsNullsIntermixedWithRealData()
        {
            ScenarioContext.Current.Add("PowerShellScript","Write-Output @($null,'Test Data', $null, $(Get-Item .))");
        }
        
        [Given(@"the script outputs a string")]
        public void GivenTheScriptOutputsAString()
        {
            ScenarioContext.Current.Add("PowerShellScript", "Write-Output \"Hello ${Env:UserName}\"");
        }

        [Given(@"the script calls ConvertTo-Splunk")]
        public void GivenTheScriptCallsConvertToSplunk()
        {
            ScenarioContext.Current.Add("PowerShellScript", "\"Hello ${Env:UserName}\" | ConvertTo-SplunkEventXml");
        }
        
        [When(@"I execute the job")]
        public void WhenIExecuteTheJob()
        {
            var script = ScenarioContext.Current.Get<string>("PowerShellScript");
            ScenarioContext.Current.Get<PowerShellJob>("PowerShellJob").Execute(script, "Test");
        }

        [Then(@"the job should succeed and produce real data")]
        public void ThenTheJobShouldSucceedAndProduceRealData()
        {
            var output = ScenarioContext.Current.Get<ObjectLogger>("OutputLog").Output["Test"];
            Assert.NotEmpty(output);
        }
        
        [Then(@"the output should have no empty events")]
        public void ThenTheOutputShouldHaveNoEmptyEvents()
        {
            var output = ScenarioContext.Current.Get<ObjectLogger>("OutputLog").Output["Test"];
            Assert.DoesNotContain(null,output);
        }

        [Given(@"I have an (.*) string")]
        public void GivenIHaveAString(string name)
        {
            ScenarioContext.Current.Add(name,"Hello World");
        }

        [When(@"I call ConvertToString with the (.*) string")]
        public void WhenICallXmlFormatterConvertToString(string name)
        {
            var output = new PSObject(ScenarioContext.Current[name]);
            var xml = XmlFormatter.ConvertToString(output);
            ScenarioContext.Current.Add("XmlEventString", xml);
        }

        [Then(@"the xml event output should not include the (.*) property")]
        public void ThenTheXmlEventOutputShouldNotIncludeTheProperty(string name)
        {
            var output = ScenarioContext.Current.Get<string>("XmlEventString");
            var propSearch = new Regex("^(?:" + name + ")\\s*=");

            Assert.False(propSearch.IsMatch(output));
        }

        [Then(@"the xml event output should be valid XML")]
        public void ThenTheXmlEventOutputShouldBeValidXml()
        {
            var output = ScenarioContext.Current.Get<string>("XmlEventString");
            var document = XDocument.Load(new StringReader(output));
            ScenarioContext.Current.Add("XmlEventDocument", document);
            Assert.NotEmpty(document.Elements("event"));
            Assert.NotEmpty(document.Elements("event").Elements("data"));
        }


        [Then(@"the xml event output should not have nested (.*) tags")]
        public void ThenTheXmlEventOutputShouldNotHaveNestedTags(string name)
        {
            if (!ScenarioContext.Current.ContainsKey("XmlEventDocument"))
            {
                this.ThenTheXmlEventOutputShouldBeValidXml();
            }
            var document = ScenarioContext.Current.Get<XDocument>("XmlEventDocument");

            var nested = document.Elements(name).Elements(name);
            Assert.Empty(nested);
        }

    }
}
