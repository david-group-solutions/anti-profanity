using DavidGroup.Content.AntiProfanity.DataSources;
using DavidGroup.Content.AntiProfanity.DataSources.Implementations;
using DavidGroup.Content.AntiProfanity.DetectionHandlers;
using DavidGroup.Content.AntiProfanity.DetectionHandlers.Implementations;
using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.Tests.DetectionHandlers.Implementations;

/// <summary>
/// Unit tests for <see cref="ProfanityDetectionTxtHandler"/>.
/// </summary>
public static class ProfanityDetectionTxtHandlerTests
{
    /// <summary>
    /// Tests for the constructor of <see cref="ProfanityDetectionTxtHandler"/>.
    /// </summary>
    public class ConstructorTests
    {
        [Fact]
        public void Constructor_NoTxtDataSourcePresent_ThrowsInvalidOperationException()
        {
            // Arrange
            IProfanityDataSource[] dataSources = [new ProfanityJsonDataSource()];

            // Act
            Action act = () => new ProfanityDetectionTxtHandler(dataSources);

            // Assert
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void Constructor_MultipleTxtDataSourcesPresent_ThrowsInvalidOperationException()
        {
            // Arrange
            IProfanityDataSource[] dataSources = [new ProfanityTxtDataSource(), new ProfanityTxtDataSource()];

            // Act
            Action act = () => new ProfanityDetectionTxtHandler(dataSources);

            // Assert
            Assert.Throws<InvalidOperationException>(act);
        }

        [Fact]
        public void Constructor_ExactlyOneTxtDataSourcePresent_DoesNotThrow()
        {
            // Arrange
            IProfanityDataSource[] dataSources = [new ProfanityJsonDataSource(), new ProfanityTxtDataSource()];

            // Act
            Exception? exception = Record.Exception(() => new ProfanityDetectionTxtHandler(dataSources));

            // Assert
            Assert.Null(exception);
        }
    }

    /// <summary>
    /// Tests for <see cref="ProfanityDetectionTxtHandler.DetectAsync"/>.
    /// </summary>
    public class DetectAsyncTests : IDisposable
    {
        private readonly List<string> _createdFiles = [];

        private async Task<ProfanityDetectionTxtHandler> CreateHandlerAsync(params string[] words)
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(path, string.Join('\n', words));
            _createdFiles.Add(path);

            ProfanityTxtDataSource dataSource = new();
            await dataSource.LoadAsync([path]);

            return new ProfanityDetectionTxtHandler([dataSource]);
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
        public async Task DetectAsync_ContentContainsMatchingWord_AddsOccurrenceWithCorrectIndexAndLength()
        {
            // Arrange
            ProfanityDetectionTxtHandler handler = await CreateHandlerAsync("badword");

            const string content = "this is a badword here";
            ProfanityDetectionContext context = new()
            {
                Content = content
            };
            int expectedIndex = content.IndexOf("badword", StringComparison.Ordinal);

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            ProfanityOccurrence occurrence = Assert.Single(context.Occurrences);
            Assert.Equal("badword", occurrence.Profanity);
            Assert.Equal(expectedIndex, occurrence.Index);
            Assert.Equal("badword".Length, occurrence.Length);
        }

        [Fact]
        public async Task DetectAsync_ContentHasDifferentCasing_StillMatchesAndRecordsStoredCasing()
        {
            // Arrange
            ProfanityDetectionTxtHandler handler = await CreateHandlerAsync("badword");

            ProfanityDetectionContext context = new()
            {
                Content = "this is a BADWORD here"
            };

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            ProfanityOccurrence occurrence = Assert.Single(context.Occurrences);
            Assert.Equal("badword", occurrence.Profanity);
        }

        [Fact]
        public async Task DetectAsync_WordIsEmbeddedInsideALargerWord_IsIgnored()
        {
            // Arrange
            ProfanityDetectionTxtHandler handler = await CreateHandlerAsync("ass");

            ProfanityDetectionContext context = new()
            {
                Content = "this is in class"
            };

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            Assert.Empty(context.Occurrences);
        }

        [Fact]
        public async Task DetectAsync_MultipleNonOverlappingOccurrencesOfSameWord_AddsOccurrenceForEach()
        {
            // Arrange
            ProfanityDetectionTxtHandler handler = await CreateHandlerAsync("cat");

            ProfanityDetectionContext context = new()
            {
                Content = "cat cat cat"
            };

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            Assert.Equal(3, context.Occurrences.Count);
            Assert.All(context.Occurrences, occurrence => Assert.Equal("cat", occurrence.Profanity));
        }

        [Fact]
        public async Task DetectAsync_MultipleDifferentWords_DetectsAllOfThem()
        {
            // Arrange
            ProfanityDetectionTxtHandler handler = await CreateHandlerAsync("cat", "dog");

            ProfanityDetectionContext context = new()
            {
                Content = "I have a cat and a dog"
            };

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            Assert.Equal(2, context.Occurrences.Count);
            Assert.Contains(context.Occurrences, occurrence => occurrence.Profanity == "cat");
            Assert.Contains(context.Occurrences, occurrence => occurrence.Profanity == "dog");
        }

        [Fact]
        public async Task DetectAsync_NoWordsMatchContent_OccurrencesRemainsEmpty()
        {
            // Arrange
            ProfanityDetectionTxtHandler handler = await CreateHandlerAsync("zzz");

            ProfanityDetectionContext context = new()
            {
                Content = "hello world"
            };

            // Act
            await handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            Assert.Empty(context.Occurrences);
        }

        [Fact]
        public async Task DetectAsync_ContentIsEmpty_DoesNotThrowAndOccurrencesRemainsEmpty()
        {
            // Arrange
            ProfanityDetectionTxtHandler handler = await CreateHandlerAsync("badword");

            ProfanityDetectionContext context = new()
            {
                Content = string.Empty
            };

            // Act
            Func<Task> act = () => handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            Exception? exception = await Record.ExceptionAsync(act);
            Assert.Null(exception);
            Assert.Empty(context.Occurrences);
        }

        [Fact]
        public async Task DetectAsync_WordIsLongerThanContent_DoesNotThrowAndOccurrencesRemainsEmpty()
        {
            // Arrange
            ProfanityDetectionTxtHandler handler = await CreateHandlerAsync("verylongprofanity");

            ProfanityDetectionContext context = new()
            {
                Content = "hi"
            };

            // Act
            Func<Task> act = () => handler.DetectAsync(context, _ => Task.CompletedTask);

            // Assert
            Exception? exception = await Record.ExceptionAsync(act);
            Assert.Null(exception);
            Assert.Empty(context.Occurrences);
        }

        [Fact]
        public async Task DetectAsync_NextDelegateProvided_InvokesNextWithSameContext()
        {
            // Arrange
            ProfanityDetectionTxtHandler handler = await CreateHandlerAsync("zzz");

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
