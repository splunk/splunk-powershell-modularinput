// ***********************************************************************
// Assembly         : ModularPowerShell.Specs
// Author           : Joel Bennett
// Created          : 03-06-2013
//
// Last Modified By : Joel Bennett
// Last Modified On : 03-08-2013
// ***********************************************************************
// <copyright file="CronParsingSteps.cs" company="Splunk">
//     Copyright (c) 2013 by Splunk Inc., all rights reserved.
// </copyright>
// <summary>
//     Step definitions for the Cron Parsing Feature tests
// </summary>
// ***********************************************************************
namespace ModularPowerShell.Specs
{
    using System;

    using Quartz;
    using Quartz.Impl;
    using Quartz.Impl.Calendar;

    using Splunk.ModularInputs;
    using System.Collections.Generic;
    using TechTalk.SpecFlow;

    using Xunit;

    [Binding]
    public class CronParsingSteps
    {
        [Given(@"I have entered ""(.*)"" as my schedule")]
        public void GivenIHaveEnteredMySchedule(string cron)
        {
            ScenarioContext.Current.Add("cronString", cron);
        }

        [Given(@"I have specified ""(.*)"" as the script")]
        public void GivenIHaveSpecifiedTheScript(string script)
        {
            ScenarioContext.Current.Add("script", script);
        }


        //[Then(@"the result should have Seconds divisible by (.*)")]
        //public void ThenTheResultShouldHaveSecondsDivisibleBy(int div)
        //{
        //    var job = new PowerShellJob();
        //    job.Logger = new ObjectLogger();
        //    job.Execute();
        //    ScenarioContext.Current.Pending();
        //}

        [When(@"I parse the schedule")]
        public void WhenIParseTheSchedule()
        {
            var cron = new CronExpression(ScenarioContext.Current.Get<string>("cronString"));
            ScenarioContext.Current.Add("cronExpression", cron);
        }

        [Then(@"the schedule should have (.*) invocations in seconds divisible by (.*)")]
        public void ThenTheScheduleShouldHaveInvocationsInSecondsDivisibleBy(int count, int divisor)
        {
            var rounded = System.DateTimeOffset.Now;
            rounded = rounded.AddSeconds(-(rounded.Second+1));
            var minute = rounded.Minute + 1;

            var cronExpression = ScenarioContext.Current.Get<CronExpression>("cronExpression");
            while (minute == (rounded = cronExpression.GetNextValidTimeAfter(rounded) ?? rounded.AddMinutes(1)).Minute)
            {
                Assert.Equal(0, rounded.Second % divisor);
                count--;
            }

            Assert.Equal(0, count);
        }

        [Then(@"the schedule should be invoked only on (.*) at (.*)")]
        public void ThenTheScheduleShouldBeInvokedOnlyOnAt(string date, string time)
        {
            var cronExpression = ScenarioContext.Current.Get<CronExpression>("cronExpression");

            var target = System.DateTimeOffset.Parse(time + " " + date);
           
            var dt = new System.DateTimeOffset(2000, 1, 1, 0, 0, 0, 0, new TimeSpan(0));
            var next = cronExpression.GetNextValidTimeAfter(dt);

            // ReSharper disable PossibleInvalidOperationException
            Assert.True(next.HasValue);
            Assert.Equal(target, next);
            next = cronExpression.GetNextValidTimeAfter(next.Value);
            // ReSharper restore PossibleInvalidOperationException
            Assert.Null(next);
        }
    }
}
