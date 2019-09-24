using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable ImplicitlyCapturedClosure

namespace Elastic.Apm.Tests
{
	public class CentralConfigFetcherTests : LoggingTestBase
	{
		private const string ThisClassName = nameof(CentralConfigFetcherTests);

		public CentralConfigFetcherTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		[Fact]
		public void Dispose_stops_the_thread()
		{
			CentralConfigFetcher lastCentralConfigFetcher;
			using (var agent = new ApmAgent(new AgentComponents(LoggerBase)))
			{
				lastCentralConfigFetcher = (CentralConfigFetcher)agent.CentralConfigFetcher;
				lastCentralConfigFetcher.IsRunning.Should().BeTrue();

				Thread.Sleep(1.Minute());
			}
			lastCentralConfigFetcher.IsRunning.Should().BeFalse();
		}

		[Theory]
		[InlineData(1)]
		[InlineData(5)]
		[InlineData(9)]
		[InlineData(10)]
		[InlineData(11)]
		[InlineData(20)]
		[InlineData(40)]
		public void Create_many_concurrent_instances(int numberOfAgentInstances)
		{
			var agents = new ApmAgent[numberOfAgentInstances];
			numberOfAgentInstances.Repeat(i =>
			{
				agents[i] = new ApmAgent(new AgentComponents(LoggerBase));
				((CentralConfigFetcher)agents[i].CentralConfigFetcher).IsRunning.Should().BeTrue();
				((PayloadSenderV2)agents[i].PayloadSender).IsRunning.Should().BeTrue();
			});

			// Sleep a few seconds to let backend component to get to the stage where they contact APM Server
			Thread.Sleep(5.Seconds());

			numberOfAgentInstances.Repeat(i =>
			{
				agents[i].Dispose();
				((CentralConfigFetcher)agents[i].CentralConfigFetcher).IsRunning.Should().BeFalse();
				((PayloadSenderV2)agents[i].PayloadSender).IsRunning.Should().BeFalse();
			});
		}
	}
}
