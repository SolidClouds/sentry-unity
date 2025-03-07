using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Sentry.Extensibility;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Editor.iOS.Tests
{
    public class SentryXcodeProjectTests
    {
        private class NativeMainTest : INativeMain
        {
            public void AddSentry(string pathToMain, IDiagnosticLogger? logger) { }
        }

        private class NativeOptionsTest : INativeOptions
        {
            public void CreateFile(string path, SentryUnityOptions options) { }
        }

        private class Fixture
        {
            public string ProjectRoot { get; set; } =
                Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestFiles", "2019_4");
            public SentryUnityOptions Options { get; set; }
            public TestLogger TestLogger { get; set; }
            public INativeMain NativeMain { get; set; } = new NativeMainTest();
            public INativeOptions NativeOptions { get; set; } = new NativeOptionsTest();

            public Fixture()
            {
                TestLogger = new TestLogger();
                Options = new SentryUnityOptions
                {
                    Debug = true,
                    DiagnosticLevel = SentryLevel.Debug,
                    DiagnosticLogger = TestLogger
                };
            }

            public SentryXcodeProject GetSut() => new(ProjectRoot, NativeMain, NativeOptions);
        }

        private Fixture _fixture = new();

        [SetUp]
        public void SetUp() => _fixture = new Fixture();

        [TearDown]
        public void DestroyFrameworkDirectories()
        {
            var frameworkPath = Path.Combine(_fixture.ProjectRoot, "Frameworks");
            if (Directory.Exists(frameworkPath))
            {
                Directory.Delete(frameworkPath, true);
            }
        }

        [Test]
        public void ReadFromProjectFile_ProjectExists_ReadsProject()
        {
            var xcodeProject = _fixture.GetSut();

            xcodeProject.ReadFromProjectFile();

            Assert.IsNotEmpty(xcodeProject.ProjectToString());
        }

        [Test]
        public void ReadFromProjectFile_ProjectDoesNotExist_ThrowsFileNotFoundException()
        {
            _fixture.ProjectRoot = "Path/That/Does/Not/Exist";
            var xcodeProject = _fixture.GetSut();

            Assert.Throws<FileNotFoundException>(() => xcodeProject.ReadFromProjectFile());
        }

        [Test]
        public void AddSentryFramework_CleanXcodeProject_SentryWasAdded()
        {
            var xcodeProject = _fixture.GetSut();
            xcodeProject.ReadFromProjectFile();

            xcodeProject.AddSentryFramework();

            StringAssert.Contains(SentryXcodeProject.FrameworkName, xcodeProject.ProjectToString());
        }

        [Test]
        public void AddSentryFramework_FrameworkSearchPathAlreadySet_DoesNotGetOverwritten()
        {
            const string testPath = "path_that_should_not_get_overwritten";
            var xcodeProject = _fixture.GetSut();
            xcodeProject.ReadFromProjectFile();
            xcodeProject.SetSearchPathBuildProperty(testPath);

            xcodeProject.AddSentryFramework();

            StringAssert.Contains(testPath, xcodeProject.ProjectToString());
        }

        [Test]
        public void AddSentryNativeBridges_FrameworkSearchPathAlreadySet_DoesNotGetOverwritten()
        {
            var xcodeProject = _fixture.GetSut();
            xcodeProject.ReadFromProjectFile();

            xcodeProject.AddSentryNativeBridge();

            StringAssert.Contains(SentryXcodeProject.BridgeName, xcodeProject.ProjectToString());
        }

        [Test]
        public void CreateNativeOptions_CleanXcodeProject_NativeOptionsAdded()
        {
            var xcodeProject = _fixture.GetSut();
            xcodeProject.ReadFromProjectFile();

            xcodeProject.AddNativeOptions(_fixture.Options);

            StringAssert.Contains(SentryXcodeProject.OptionsName, xcodeProject.ProjectToString());
        }

        [Test]
        public void AddBuildPhaseSymbolUpload_CleanXcodeProject_BuildPhaseSymbolUploadAdded()
        {
            var xcodeProject = _fixture.GetSut();
            xcodeProject.ReadFromProjectFile();

            var didContainUploadPhase = xcodeProject.MainTargetContainsSymbolUploadBuildPhase();
            xcodeProject.AddBuildPhaseSymbolUpload(_fixture.Options.DiagnosticLogger, new SentryCliOptions());
            var doesContainUploadPhase = xcodeProject.MainTargetContainsSymbolUploadBuildPhase();

            Assert.IsFalse(didContainUploadPhase);
            Assert.IsTrue(doesContainUploadPhase);
        }

        [Test]
        public void AddBuildPhaseSymbolUpload_PhaseAlreadyAdded_LogsAndDoesNotAddAgain()
        {
            const int expectedBuildPhaseOccurence = 1;
            var xcodeProject = _fixture.GetSut();
            xcodeProject.ReadFromProjectFile();

            xcodeProject.AddBuildPhaseSymbolUpload(_fixture.Options.DiagnosticLogger, new SentryCliOptions());
            xcodeProject.AddBuildPhaseSymbolUpload(_fixture.Options.DiagnosticLogger, new SentryCliOptions());

            var actualBuildPhaseOccurence = Regex.Matches(xcodeProject.ProjectToString(),
                Regex.Escape(SentryXcodeProject.SymbolUploadPhaseName)).Count;

            Assert.AreEqual(1, _fixture.TestLogger.Logs.Count);
            Assert.AreEqual(expectedBuildPhaseOccurence, actualBuildPhaseOccurence);
        }
    }
}
