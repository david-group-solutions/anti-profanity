using System.Collections.ObjectModel;

using DavidGroup.Content.AntiProfanity.DetectionHandlers;
using DavidGroup.Content.AntiProfanity.Enums;
using DavidGroup.Content.AntiProfanity.Models;
using DavidGroup.Content.AntiProfanity.Pipelines;
using DavidGroup.Content.AntiProfanity.Services;

using Moq;

namespace DavidGroup.Content.AntiProfanity.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AntiProfanityService"/>.
/// </summary>
public static class AntiProfanityServiceTests
{
    private static Mock<IProfanityDetectionPipeline> CreatePipelineMock(
        Action<ProfanityDetectionContext>? configureContext = null)
    {
        Mock<IProfanityDetectionPipeline> mock = new();
        mock.Setup(p => p.DetectAsync(It.IsAny<ProfanityDetectionContext>()))
            .Returns((ProfanityDetectionContext context) =>
            {
                configureContext?.Invoke(context);
                return Task.CompletedTask;
            });
        return mock;
    }

    /// <summary>
    /// Tests for <see cref="AntiProfanityService.DetectAsync"/>.
    /// </summary>
    public class DetectAsyncTests
    {
        [Fact]
        public async Task DetectAsync_CallsPipelineWithContextContainingProvidedTextAndSeverityLevel()
        {
            // Arrange
            ProfanityDetectionContext? capturedContext = null;
            Mock<IProfanityDetectionPipeline> pipelineMock = CreatePipelineMock(context => capturedContext = context);
            AntiProfanityService service = new(pipelineMock.Object);

            const string text = "some text";
            const ProfanitySeverityLevel severityLevel = ProfanitySeverityLevel.Mild;

            // Act
            await service.DetectAsync(text, severityLevel);

            // Assert
            Assert.NotNull(capturedContext);
            Assert.Equal(text, capturedContext.Content);
            Assert.Equal(severityLevel, capturedContext.SeverityLevel);
        }

        [Fact]
        public async Task DetectAsync_SeverityLevelNotProvided_DefaultsToNotSpecified()
        {
            // Arrange
            ProfanityDetectionContext? capturedContext = null;
            Mock<IProfanityDetectionPipeline> pipelineMock = CreatePipelineMock(context => capturedContext = context);
            AntiProfanityService service = new(pipelineMock.Object);

            // Act
            await service.DetectAsync("some text");

            // Assert
            Assert.NotNull(capturedContext);
            Assert.Equal(ProfanitySeverityLevel.NotSpecified, capturedContext.SeverityLevel);
        }

        [Fact]
        public async Task DetectAsync_PipelineReportsNoOccurrences_ReturnsEmptyCollection()
        {
            // Arrange
            Mock<IProfanityDetectionPipeline> pipelineMock = CreatePipelineMock();
            AntiProfanityService service = new(pipelineMock.Object);

            // Act
            ReadOnlyCollection<ProfanityOccurrence> result = await service.DetectAsync("hello world");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task DetectAsync_PipelineReportsOccurrences_ReturnsThemAsReadOnlyCollection()
        {
            // Arrange
            ProfanityOccurrence expectedOccurrence = new(
                Profanity: "badword",
                Index: 10,
                Length: 7
            );

            Mock<IProfanityDetectionPipeline> pipelineMock = CreatePipelineMock(context => context.Occurrences.Add(expectedOccurrence));
            AntiProfanityService service = new(pipelineMock.Object);

            // Act
            ReadOnlyCollection<ProfanityOccurrence> result = await service.DetectAsync("this is a badword here");

            // Assert
            ProfanityOccurrence actualOccurrence = Assert.Single(result);
            Assert.Equal(expectedOccurrence, actualOccurrence);
        }
    }

    /// <summary>
    /// Tests for <see cref="AntiProfanityService.CensorAsync"/>.
    /// </summary>
    public class CensorAsyncTests
    {
        [Fact]
        public async Task CensorAsync_NoOccurrencesDetected_ReturnsTextUnchanged()
        {
            // Arrange
            Mock<IProfanityDetectionPipeline> pipelineMock = CreatePipelineMock();
            AntiProfanityService service = new(pipelineMock.Object);

            const string text = "hello world";

            // Act
            string result = await service.CensorAsync(text);

            // Assert
            Assert.Equal(text, result);
        }

        [Fact]
        public async Task CensorAsync_SingleOccurrenceDetected_ReplacesMatchedRangeWithDefaultCensorCharacter()
        {
            // Arrange
            const string text = "this is a badword here";
            int index = text.IndexOf("badword", StringComparison.Ordinal);
            int length = "badword".Length;

            Mock<IProfanityDetectionPipeline> pipelineMock = CreatePipelineMock(context =>
                context.Occurrences.Add(new ProfanityOccurrence(Profanity: "badword", Index: index, Length: length)));
            AntiProfanityService service = new(pipelineMock.Object);

            string expected = text[..index] + new string('*', length) + text[(index + length)..];

            // Act
            string result = await service.CensorAsync(text);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task CensorAsync_CustomCensorCharacterProvided_UsesItInsteadOfDefault()
        {
            // Arrange
            const string text = "this is a badword here";
            int index = text.IndexOf("badword", StringComparison.Ordinal);
            int length = "badword".Length;

            Mock<IProfanityDetectionPipeline> pipelineMock = CreatePipelineMock(context =>
                context.Occurrences.Add(new ProfanityOccurrence(Profanity: "badword", Index: index, Length: length)));
            AntiProfanityService service = new(pipelineMock.Object);

            const char censorCharacter = '#';
            string expected = text[..index] + new string(censorCharacter, length) + text[(index + length)..];

            // Act
            string result = await service.CensorAsync(text, censorCharacter: censorCharacter);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task CensorAsync_MultipleNonOverlappingOccurrencesDetected_CensorsEachRange()
        {
            // Arrange
            const string text = "cat and dog";
            int catIndex = text.IndexOf("cat", StringComparison.Ordinal);
            int dogIndex = text.IndexOf("dog", StringComparison.Ordinal);

            Mock<IProfanityDetectionPipeline> pipelineMock = CreatePipelineMock(context =>
            {
                context.Occurrences.Add(new ProfanityOccurrence(Profanity: "cat", Index: catIndex, Length: 3));
                context.Occurrences.Add(new ProfanityOccurrence(Profanity: "dog", Index: dogIndex, Length: 3));
            });
            AntiProfanityService service = new(pipelineMock.Object);

            // Act
            string result = await service.CensorAsync(text);

            // Assert
            Assert.Equal("*** and ***", result);
        }

        [Fact]
        public async Task CensorAsync_EmptyText_ReturnsEmptyString()
        {
            // Arrange
            Mock<IProfanityDetectionPipeline> pipelineMock = CreatePipelineMock();
            AntiProfanityService service = new(pipelineMock.Object);

            // Act
            string result = await service.CensorAsync(string.Empty);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task CensorAsync_AlwaysReturnsStringWithSameLengthAsInput()
        {
            // Arrange
            const string text = "this is a badword here";
            int index = text.IndexOf("badword", StringComparison.Ordinal);

            Mock<IProfanityDetectionPipeline> pipelineMock = CreatePipelineMock(context =>
                context.Occurrences.Add(new ProfanityOccurrence(Profanity: "badword", Index: index, Length: 7)));
            AntiProfanityService service = new(pipelineMock.Object);

            // Act
            string result = await service.CensorAsync(text);

            // Assert
            Assert.Equal(text.Length, result.Length);
        }

        [Fact]
        public async Task CensorAsync_ForwardsSeverityLevelToPipelineThroughDetectAsync()
        {
            // Arrange
            ProfanityDetectionContext? capturedContext = null;
            Mock<IProfanityDetectionPipeline> pipelineMock = CreatePipelineMock(context => capturedContext = context);
            AntiProfanityService service = new(pipelineMock.Object);

            const ProfanitySeverityLevel severityLevel = ProfanitySeverityLevel.Mild;

            // Act
            await service.CensorAsync("hello world", severityLevel);

            // Assert
            Assert.NotNull(capturedContext);
            Assert.Equal(severityLevel, capturedContext.SeverityLevel);
        }
    }
}
