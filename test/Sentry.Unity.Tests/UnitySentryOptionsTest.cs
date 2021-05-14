﻿using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace Sentry.Unity.Tests
{
    public sealed class UnitySentryOptionsTest
    {
        private const string TestSentryOptionsFileName = "TestSentryOptions.json";

        [Test]
        public void Options_ReadFromFile_Success()
        {
            var optionsFilePath = GetTestOptionsFilePath();
            Assert.IsTrue(File.Exists(optionsFilePath));

            var jsonRaw = File.ReadAllText(optionsFilePath);
            using var jsonDocument = JsonDocument.Parse(jsonRaw);

            SentryUnityOptions.FromJson(jsonDocument.RootElement);
        }

        [Test]
        public void Options_WriteRead_Equals()
        {
            // arrange
            var optionsExpected = new SentryUnityOptions
            {
                Enabled = true,
                Dsn = "https://test.com",
                CaptureInEditor = true,
                Debug = true,
                DebugOnlyInEditor = false,
                DiagnosticLevel = SentryLevel.Info,
                RequestBodyCompressionLevel = CompressionLevelWithAuto.NoCompression,
                AttachStacktrace = true,
                SampleRate = 1f,
                Release = "release",
                Environment = "test"
            };

            // act
            using var memory = new MemoryStream();
            using var writer = new Utf8JsonWriter(memory);
            optionsExpected.WriteTo(writer);

            var jsonRaw = Encoding.UTF8.GetString(memory.ToArray());
            using var jsonDocument = JsonDocument.Parse(jsonRaw);
            var optionsActual = SentryUnityOptions.FromJson(jsonDocument.RootElement);

            // assert
            AssertOptions(optionsActual, optionsExpected);
        }

        private static void AssertOptions(SentryUnityOptions actual, SentryUnityOptions expected)
        {
            Assert.AreEqual(expected.Enabled, actual.Enabled);
            Assert.AreEqual(expected.Dsn, actual.Dsn);
            Assert.AreEqual(expected.CaptureInEditor, actual.CaptureInEditor);
            Assert.AreEqual(expected.Debug, actual.Debug);
            Assert.AreEqual(expected.DebugOnlyInEditor, actual.DebugOnlyInEditor);
            Assert.AreEqual(expected.DiagnosticLevel, actual.DiagnosticLevel);
            Assert.AreEqual(expected.AttachStacktrace, actual.AttachStacktrace);
            Assert.AreEqual(expected.SampleRate, actual.SampleRate);
            Assert.AreEqual(expected.Release, actual.Release);
            Assert.AreEqual(expected.Environment, actual.Environment);
            Assert.AreEqual(expected.RequestBodyCompressionLevel, actual.RequestBodyCompressionLevel);
        }

        private static string GetTestOptionsFilePath()
        {
            var assemblyFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Assert.NotNull(assemblyFolderPath);
            return Path.Combine(assemblyFolderPath!, TestSentryOptionsFileName);
        }
    }
}