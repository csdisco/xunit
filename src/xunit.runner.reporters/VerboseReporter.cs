using Xunit.Abstractions;

namespace Xunit.Runner.Reporters
{
    public class VerboseReporter : IRunnerReporter
    {
        public string Description
        {
            get { return "show verbose progress messages"; }
        }

        public bool IsEnvironmentallyEnabled
        {
            get { return false; }
        }

        public string RunnerSwitch
        {
            get { return "verbose"; }
        }

        public IMessageSink CreateMessageHandler(IRunnerLogger logger)
        {
            return new MessageHandler(logger);
        }

        class MessageHandler : DefaultRunnerMessageHandler
        {
            public MessageHandler(IRunnerLogger logger) : base(logger) { }

            protected override bool Visit(ITestPassed testPassed)
            {
                Logger.LogMessage("    PASS:  {0}", Escape(testPassed.Test.DisplayName));

                return base.Visit(testPassed);
            }

            protected override bool Visit(ITestStarting testStarting)
            {
                Logger.LogMessage("    START: {0}", Escape(testStarting.Test.DisplayName));

                return base.Visit(testStarting);
            }

        }
    }
}
