using DavidGroup.Content.AntiProfanity.DataSources;
using DavidGroup.Content.AntiProfanity.DataSources.Implementations;
using DavidGroup.Content.AntiProfanity.DetectionHandlers;
using DavidGroup.Content.AntiProfanity.DetectionHandlers.Implementations;
using DavidGroup.Content.AntiProfanity.Enums;
using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.Tests.DetectionHandlers.Implementations;

/// <summary>
/// Unit tests for <see cref="ProfanityDetectionJsonHandler"/>.
/// </summary>
public static class ProfanityDetectionJsonHandlerTests
{
    /// <summary>
    /// Tests for the constructor of <see cref="ProfanityDetectionJsonHandler"/>.
    /// </summary>
    public class ConstructorTests
    {
        [Fact]
        public void Constructor_NoJsonDataSourcePresent_ThrowsInvalidOperationException()
        {
            // Arrange
            IProfanityDataSource[] dataSources = [new ProfanityTxtDataSource()];

            // Act
            Action act = () => new ProfanityDetectionJsonHandler(dataSources);

            // Assert
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void Constructor_MultipleJsonDataSourcesPresent_ThrowsInvalidOperationException()
        {
            // Arrange
            IProfanityDataSource[] dataSources = [new ProfanityJsonDataSource(), new ProfanityJsonDataSource()];

            // Act
            Action act = () => new ProfanityDetectionJsonHandler(dataSources);

            // Assert
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void Constructor_ExactlyOneJsonDataSourcePresent_DoesNotThrow()
        {
            // Arrange
            IProfanityDataSource[] dataSources = [new ProfanityTxtDataSource(), new ProfanityJsonDataSource()];

            // Act
            Exception? exception = Record.Exception(() => new ProfanityDetectionJsonHandler(dataSources));

            // Assert
            Assert.Null(exception);
        }
    }

    /// <summary>
    /// Tests for <see cref="ProfanityDetectionJsonHandler.DetectAsync"/>.
    /// </summary>
    public class DetectAsyncTests : IDisposable
    {
        private readonly List<string> _createdFiles = [];

        private async Task<ProfanityDetectionJsonHandler> CreateHandlerAsync(string json)
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
            await File.WriteAllTextAsync(path, json);
            _createdFiles.Add(path);

            ProfanityJsonDataSource dataSource = new();
            await dataSource.LoadAsync([path]);

            return new ProfanityDetectionJsonHandler([dataSource]);
        }

        public void Dispose()
        {
            foreach (string path in _createdFiles)
            {
                try
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch (IOException)
                {
                    // Best-effort cleanup; ignore files that could not be removed.
                }
            }
        }

        [Fact]
        public async Task DetectAsync_ContentContainsMatchingProfanity_AddsOccurrenceWithCorrectIndexAndLenght()
        {
            // Arrange
            const string json = """
                                [ { "Id": "p1", "Match": "badword", "Severity": 1 } ]
                                """;
            ProfanityDetectionJsonHandler handler = await CreateHandlerAsync(json);

            const string content = "this is a badword here";
            ProfanityDetectionContext context = new()
            {
                Content = content,
                SeverityLevel = ProfanitySeverityLevel.Mild
            };
            int expectedIndex = content.IndexOf("badword", StringComparison.Ordinal);

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            ProfanityOccurrence occurrence = Assert.Single(context.Occurrences);
            Assert.Equal("p1", occurrence.Profanity);
            Assert.Equal(expectedIndex, occurrence.Index);
            Assert.Equal("badword".Length, occurrence.Length);

            JsonProfanity details = Assert.IsType<JsonProfanity>(occurrence.Details);
            Assert.Equal("p1", details.Id);
        }

        [Fact]
        public async Task DetectAsync_ProfanitySeverityBelowContextSeverityLevel_DoesNotAddOccurrence()
        {
            // Arrange
            const string json = """
                                [ { "Id": "p1", "Match": "badword", "Severity": 1 } ]
                                """;
            ProfanityDetectionJsonHandler handler = await CreateHandlerAsync(json);

            ProfanityDetectionContext context = new()
            {
                Content = "this contains a badword",
                SeverityLevel = ProfanitySeverityLevel.Medium
            };

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            Assert.Empty(context.Occurrences);
        }

        [Fact]
        public async Task DetectAsync_ProfanitySeverityEqualToContextSeverityLevel_AddsOccurrence()
        {
            // Arrange
            const string json = """
                                [ { "Id": "p1", "Match": "badword", "Severity": 1 } ]
                                """;
            ProfanityDetectionJsonHandler handler = await CreateHandlerAsync(json);

            ProfanityDetectionContext context = new()
            {
                Content = "this contains a badword",
                SeverityLevel = ProfanitySeverityLevel.Mild
            };

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            Assert.Single(context.Occurrences);
        }

        [Fact]
        public async Task DetectAsync_ProfanitySeverityAboveContextSeverityLevel_AddsOccurrence()
        {
            // Arrange
            const string json = """
                                [ { "Id": "p1", "Match": "badword", "Severity": 2 } ]
                                """;
            ProfanityDetectionJsonHandler handler = await CreateHandlerAsync(json);

            ProfanityDetectionContext context = new()
            {
                Content = "this contains a badword",
                SeverityLevel = ProfanitySeverityLevel.Mild
            };

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            Assert.Single(context.Occurrences);
        }

        [Fact]
        public async Task DetectAsync_MatchedWordSatisfiesExceptionPattern_OccurrenceIsExcluded()
        {
            // Arrange
            const string json = """
                                [ { "Id": "p1", "Match": "ass", "Severity": 1, "Exceptions": ["class*"] } ]
                                """;
            ProfanityDetectionJsonHandler handler = await CreateHandlerAsync(json);

            ProfanityDetectionContext context = new()
            {
                Content = "he is in class today",
                SeverityLevel = ProfanitySeverityLevel.NotSpecified
            };

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            Assert.Empty(context.Occurrences);
        }

        [Fact]
        public async Task DetectAsync_MatchedWordDoesNotSatisfyExceptionPattern_OccurrenceIsIncluded()
        {
            // Arrange
            const string json = """
                                [ { "Id": "p1", "Match": "ass", "Severity": 1, "Exceptions": ["class*"] } ]
                                """;
            ProfanityDetectionJsonHandler handler = await CreateHandlerAsync(json);

            const string content = "you are an ass";
            ProfanityDetectionContext context = new()
            {
                Content = content,
                SeverityLevel = ProfanitySeverityLevel.NotSpecified
            };

            int expectedIndex = content.IndexOf("ass", StringComparison.Ordinal);

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            ProfanityOccurrence occurrence = Assert.Single(context.Occurrences);
            Assert.Equal(expectedIndex, occurrence.Index);
        }

        [Fact]
        public async Task DetectAsync_ContentContainsMultipleOccurrencesOfSameProfanity_AddsOneOccurrencePerMatch()
        {
            // Arrange
            const string json = """
                                [ { "Id": "p1", "Match": "cat", "Severity": 1 } ]
                                """;
            ProfanityDetectionJsonHandler handler = await CreateHandlerAsync(json);

            ProfanityDetectionContext context = new()
            {
                Content = "cat cat cat",
                SeverityLevel = ProfanitySeverityLevel.NotSpecified
            };

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            Assert.Equal(3, context.Occurrences.Count);
            Assert.All(context.Occurrences, occurrence => Assert.Equal("p1", occurrence.Profanity));
        }

        [Fact]
        public async Task DetectAsync_MultipleProfanityEntriesMatchContent_DetectsAllOfThem()
        {
            // Arrange
            const string json = """
                                [
                                  { "Id": "p1", "Match": "cat", "Severity": 1 },
                                  { "Id": "p2", "Match": "dog", "Severity": 1 }
                                ]
                                """;
            ProfanityDetectionJsonHandler handler = await CreateHandlerAsync(json);

            ProfanityDetectionContext context = new()
            {
                Content = "I have a cat and a dog",
                SeverityLevel = ProfanitySeverityLevel.NotSpecified
            };

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            Assert.Equal(2, context.Occurrences.Count);
            Assert.Contains(context.Occurrences, occurrence => occurrence.Profanity == "p1");
            Assert.Contains(context.Occurrences, occurrence => occurrence.Profanity == "p2");
        }

        [Fact]
        public async Task DetectAsync_ContentHasNoMatches_OccurrencesRemainsEmpty()
        {
            // Arrange
            const string json = """
                                [ { "Id": "p1", "Match": "zzz", "Severity": 1 } ]
                                """;
            ProfanityDetectionJsonHandler handler = await CreateHandlerAsync(json);

            ProfanityDetectionContext context = new()
            {
                Content = "hello world",
                SeverityLevel = ProfanitySeverityLevel.NotSpecified
            };

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            Assert.Empty(context.Occurrences);
        }

        [Fact]
        public async Task DetectAsync_NextDelegateProvided_InvokesNextWithSameContext()
        {
            // Arrange
            const string json = """
                                [ { "Id": "p1", "Match": "zzz", "Severity": 1 } ]
                                """;
            ProfanityDetectionJsonHandler handler = await CreateHandlerAsync(json);

            ProfanityDetectionContext context = new()
            {
                Content = "hello world"
            };

            ProfanityDetectionContext? capturedContext = null;
            int callCount = 0;
            NextProfanityDetectionHandlerDelegate next = ctx =>
            {
                capturedContext = ctx;
                callCount++;
                return Task.CompletedTask;
            };

            // Act
            await handler.DetectAsync(context, next);

            // Assert
            Assert.Equal(1, callCount);
            Assert.Same(context, capturedContext);
        }
    }
}
