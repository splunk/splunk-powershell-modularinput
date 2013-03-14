namespace ModularPowerShell.Specs
{
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using Splunk.ModularInputs;
    using Splunk.ModularInputs.Serialization;

    using TechTalk.SpecFlow;
    using Xunit;

    [Binding]
    public class BugFixesSteps : Steps
    {
        [Given(@"the script outputs nulls \(intermixed with real data\)")]
        public void GivenTheScriptOutputsNullsIntermixedWithRealData()
        {
            ScenarioContext.Current.Add("PowerShellScript","Write-Output @($null,'Test Data', $null, $(Get-Item .))");
        }
        
        [When(@"I execute the job")]
        public void WhenIExecuteTheJob()
        {
            var script = ScenarioContext.Current.Get<string>("PowerShellScript");
            ScenarioContext.Current.Get<PowerShellJob>("PowerShellJob").Execute(PowerShellJob.Iss, script, "Test");
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

        [Given(@"I have an output string")]
        public void GivenIHaveAnOutputString()
        {
            Given("I have an Object Logger");
            ScenarioContext.Current.Get<ObjectLogger>("OutputLog").WriteOutput(new PSObject("Hello World"), "Test");
        }

        [When(@"I call ConvertToString with the output string")]
        public void WhenICallConvertToString()
        {
            var output = ScenarioContext.Current.Get<ObjectLogger>("OutputLog").Output["Test"];
            var xml = XmlFormatter.ConvertToString(output.First() as PSObject);
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
                Then("the xml event output should be valid XML");
            }
            var document = ScenarioContext.Current.Get<XDocument>("XmlEventDocument");

            var nested = document.Descendants(name).Descendants(name);
            Assert.Empty(nested);
        }

        [Then(@"the xml event output should not have ""(.*)"" in the data")]
        public void ThenTheXmlEventOutputShouldNotHaveInTheData(string text)
        {
            if (!ScenarioContext.Current.ContainsKey("XmlEventDocument"))
            {
                Then("the xml event output should be valid XML");
            }
            var document = ScenarioContext.Current.Get<XDocument>("XmlEventDocument");

            var nested = document.Descendants("data").Where(e => e.Value.Contains(text)).ToList();
            Assert.Empty(nested);
        }

        [Then(@"the xml event output should have ""(.*)"" in the data")]
        public void ThenTheXmlEventOutputShouldHaveInTheData(string text)
        {
            if (!ScenarioContext.Current.ContainsKey("XmlEventDocument"))
            {
                Then("the xml event output should be valid XML");
            }
            var document = ScenarioContext.Current.Get<XDocument>("XmlEventDocument");

            var nested = document.Descendants("data").Where(e => e.Value.Contains(text)).ToList();
            Assert.NotEmpty(nested);
        }

        [Then(@"the output should have a SplunkPreFormatted property on it")]
        public void ThenTheOutputShouldHaveASplunkPreFormattedPropertyOnIt()
        {
            var output = ScenarioContext.Current.Get<ObjectLogger>("OutputLog").Output["Test"];
            Assert.True(output.First().SplunkPreFormatted);
        }

    }
}
