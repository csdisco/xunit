using Xunit.Abstractions;

namespace Xunit.Runner.Reporters
{
    public class QuietReporter : IRunnerReporter
    {
        public string Description
        {
            get { return "do not show progress messages"; }
        }

        public bool IsEnvironmentallyEnabled
        {
            get { return false; }
        }

        public string RunnerSwitch
        {
            get { return "quiet"; }
        }

        public IMessageSink CreateMessageHandler(IRunnerLogger logger)
        {
            return new MessageHandler(logger);
        }

        class MessageHandler : DefaultRunnerMessageHandler
        {
            public MessageHandler(IRunnerLogger logger) : base(logger) { }

            protected override bool Visit(ITestAssemblyDiscoveryStarting discoveryStarting)
            {
                return true;
            }

            protected override bool Visit(ITestAssemblyDiscoveryFinished discoveryFinished)
            {
                return true;
            }

            protected override bool Visit(ITestAssemblyStarting assemblyStarting)
            {
                return true;
            }

            protected override bool Visit(ITestAssemblyFinished assemblyFinished)
            {
                return true;
            }
        }
    }
}
