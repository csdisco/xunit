using System;
using System.IO;
using System.Linq;
using Xunit.Abstractions;

namespace Xunit
{
    /// <summary>
    /// Default implementation of <see cref="IMessageSink"/> used to report
    /// messages for test runners.
    /// </summary>
    public class DefaultRunnerMessageHandler : TestMessageVisitor
    {
        readonly string defaultDirectory = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRunnerMessageHandler"/> class.
        /// </summary>
        /// <param name="logger">The logger used to report messages</param>
        public DefaultRunnerMessageHandler(IRunnerLogger logger)
        {
#if !NETFX_CORE
            defaultDirectory = Directory.GetCurrentDirectory();
#endif

            Logger = logger;
        }

        /// <summary>
        /// Get the logger used to report messages.
        /// </summary>
        protected IRunnerLogger Logger { get; private set; }

        /// <summary>
        /// Escapes text for display purposes.
        /// </summary>
        /// <param name="text">The text to be escaped</param>
        /// <returns>The escaped text</returns>
        protected virtual string Escape(string text)
        {
            if (text == null)
                return string.Empty;

            return text.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\0", "\\0");
        }

        /// <summary>
        /// Gets the display name of a test assembly from a test assembly message.
        /// </summary>
        /// <param name="assemblyMessage">The test assembly message</param>
        /// <returns>The assembly display name</returns>
        protected virtual string GetAssemblyDisplayName(ITestAssemblyMessage assemblyMessage)
        {
            return Path.GetFileNameWithoutExtension(assemblyMessage.TestAssembly.Assembly.AssemblyPath);
        }

        /// <summary>
        /// Gets the display name of a test assembly from a test assembly message.
        /// </summary>
        /// <param name="assembly">The test assembly</param>
        /// <returns>The assembly display name</returns>
        protected virtual string GetAssemblyDisplayName(XunitProjectAssembly assembly)
        {
            return Path.GetFileNameWithoutExtension(assembly.AssemblyFilename);
        }

        /// <summary>
        /// Logs an error message to the logger.
        /// </summary>
        /// <param name="failureType">The type of the failure</param>
        /// <param name="failureInfo">The failure information</param>
        protected void LogError(string failureType, IFailureInformation failureInfo)
        {
            lock (Logger.LockObject)
            {
                Logger.LogError("   [{0}] {1}", failureType, Escape(failureInfo.ExceptionTypes.FirstOrDefault() ?? "(Unknown Exception Type)"));
                Logger.LogImportantMessage("      {0}", Escape(ExceptionUtility.CombineMessages(failureInfo)));
                LogStackTrace(ExceptionUtility.CombineStackTraces(failureInfo));
            }
        }


        /// <summary>
        /// Logs a stack trace to the logger.
        /// </summary>
        /// <param name="stackTrace"></param>
        protected virtual void LogStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return;

            Logger.LogMessage("      Stack Trace:");

            foreach (var stackFrame in stackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
                Logger.LogImportantMessage("         {0}", StackFrameTransformer.TransformFrame(stackFrame, defaultDirectory));
        }

        /// <inheritdoc/>
        protected override bool Visit(ITestAssemblyDiscoveryStarting discoveryStarting)
        {
            var assemblyDisplayName = GetAssemblyDisplayName(discoveryStarting.Assembly);

            if (discoveryStarting.DiscoveryOptions.GetDiagnosticMessagesOrDefault())
                Logger.LogImportantMessage("Discovering: {0} (method display = {1}, parallel test collections = {2}, max threads = {3})",
                                           assemblyDisplayName,
                                           discoveryStarting.DiscoveryOptions.GetMethodDisplayOrDefault(),
                                           !discoveryStarting.ExecutionOptions.GetDisableParallelizationOrDefault(),
                                           discoveryStarting.ExecutionOptions.GetMaxParallelThreadsOrDefault());
            else
                Logger.LogImportantMessage("Discovering: {0}", assemblyDisplayName);

            return base.Visit(discoveryStarting);
        }

        /// <inheritdoc/>
        protected override bool Visit(ITestAssemblyDiscoveryFinished discoveryFinished)
        {
            var assemblyDisplayName = GetAssemblyDisplayName(discoveryFinished.Assembly);

            if (discoveryFinished.DiscoveryOptions.GetDiagnosticMessagesOrDefault())
                Logger.LogImportantMessage("Discovered:  {0} (running {1} of {2} test cases)", assemblyDisplayName, discoveryFinished.TestCasesToRun, discoveryFinished.TestCasesDiscovered);
            else
                Logger.LogImportantMessage("Discovered:  {0}", assemblyDisplayName);

            return base.Visit(discoveryFinished);
        }

        /// <inheritdoc/>
        protected override bool Visit(ITestAssemblyStarting assemblyStarting)
        {
            Logger.LogImportantMessage("Starting:    {0}", GetAssemblyDisplayName(assemblyStarting));

            return base.Visit(assemblyStarting);
        }

        /// <inheritdoc/>
        protected override bool Visit(ITestAssemblyFinished assemblyFinished)
        {
            Logger.LogImportantMessage("Finished:    {0}", GetAssemblyDisplayName(assemblyFinished));

            return base.Visit(assemblyFinished);
        }

        /// <inheritdoc/>
        protected override bool Visit(ITestFailed testFailed)
        {
            lock (Logger.LockObject)
            {
                Logger.LogError("   {0} [FAIL]", Escape(testFailed.Test.DisplayName));
                Logger.LogImportantMessage("      {0}", ExceptionUtility.CombineMessages(testFailed).Replace(Environment.NewLine, Environment.NewLine + "      "));
                LogStackTrace(ExceptionUtility.CombineStackTraces(testFailed));
            }

            return base.Visit(testFailed);
        }

        /// <inheritdoc/>
        protected override bool Visit(ITestSkipped testSkipped)
        {
            lock (Logger.LockObject)
            {
                Logger.LogWarning("   {0} [SKIP]", Escape(testSkipped.Test.DisplayName));
                Logger.LogImportantMessage("      {0}", Escape(testSkipped.Reason));
            }

            return base.Visit(testSkipped);
        }

        /// <inheritdoc/>
        protected override bool Visit(IErrorMessage error)
        {
            LogError("FATAL ERROR", error);

            return base.Visit(error);
        }

        /// <inheritdoc/>
        protected override bool Visit(ITestAssemblyCleanupFailure cleanupFailure)
        {
            LogError(string.Format("Test Assembly Cleanup Failure ({0})", cleanupFailure.TestAssembly.Assembly.AssemblyPath), cleanupFailure);

            return base.Visit(cleanupFailure);
        }

        /// <inheritdoc/>
        protected override bool Visit(ITestCaseCleanupFailure cleanupFailure)
        {
            LogError(string.Format("Test Case Cleanup Failure ({0})", cleanupFailure.TestCase.DisplayName), cleanupFailure);

            return base.Visit(cleanupFailure);
        }

        /// <inheritdoc/>
        protected override bool Visit(ITestClassCleanupFailure cleanupFailure)
        {
            LogError(string.Format("Test Class Cleanup Failure ({0})", cleanupFailure.TestClass.Class.Name), cleanupFailure);

            return base.Visit(cleanupFailure);
        }

        /// <inheritdoc/>
        protected override bool Visit(ITestCollectionCleanupFailure cleanupFailure)
        {
            LogError(string.Format("Test Collection Cleanup Failure ({0})", cleanupFailure.TestCollection.DisplayName), cleanupFailure);

            return base.Visit(cleanupFailure);
        }

        /// <inheritdoc/>
        protected override bool Visit(ITestCleanupFailure cleanupFailure)
        {
            LogError(string.Format("Test Cleanup Failure ({0})", cleanupFailure.Test.DisplayName), cleanupFailure);

            return base.Visit(cleanupFailure);
        }

        /// <inheritdoc/>
        protected override bool Visit(ITestMethodCleanupFailure cleanupFailure)
        {
            LogError(string.Format("Test Method Cleanup Failure ({0})", cleanupFailure.TestMethod.Method.Name), cleanupFailure);

            return base.Visit(cleanupFailure);
        }
    }
}
