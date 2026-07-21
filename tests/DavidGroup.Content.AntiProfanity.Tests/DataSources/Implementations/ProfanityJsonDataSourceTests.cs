using System.Text.Json;

using DavidGroup.Content.AntiProfanity.DataSources.Implementations;
using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.Tests.DataSources.Implementations;

/// <summary>
/// Unit tests for <see cref="ProfanityJsonDataSource"/>.
/// </summary>
public static class ProfanityJsonDataSourceTests
{
    /// <summary>
    /// Tests for <see cref="ProfanityJsonDataSource.CanLoad(string)"/>.
    /// </summary>
    public class CanLoadTests
    {
        [Fact]
        public void CanLoad_JsonExtension_ReturnsTrue()
        {
            // Arrange
            ProfanityJsonDataSource dataSource = new();

            // Act
            bool result = dataSource.CanLoad(".json");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanLoad_NonJsonExtension_ReturnsFalse()
        {
            // Arrange
            ProfanityJsonDataSource dataSource = new();

            // Act
            bool result = dataSource.CanLoad(".xml");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanLoad_ExtensionDiffersOnlyByCasing_ReturnsFalse()
        {
            // Arrange
            ProfanityJsonDataSource dataSource = new();

            // Act
            bool result = dataSource.CanLoad(".JSON");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanLoad_EmptyString_ReturnsFalse()
        {
            // Arrange
            ProfanityJsonDataSource dataSource = new();

            // Act
            bool result = dataSource.CanLoad(string.Empty);

            // Assert
            Assert.False(result);
        }
    }

    /// <summary>
    /// Tests for <see cref="ProfanityJsonDataSource.LoadAsync(IEnumerable{string}, CancellationToken)"/>.
    /// </summary>
    public class LoadAsyncTests : IDisposable
    {
        private readonly List<string> _createdFiles = [];

        private string CreateTempJsonFile(string content)
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
            File.WriteAllText(path, content);
            _createdFiles.Add(path);
            return path;
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
        public async Task LoadAsync_SinglePathDoesNotExist_ThrowsFileNotFoundExceptionWithPathInMessage()
        {
            // Arrange
            ProfanityJsonDataSource dataSource = new();
            string missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

            // Act
            Func<Task> act = () => dataSource.LoadAsync([missingPath]);

            // Assert
            FileNotFoundException exception = await Assert.ThrowsAsync<FileNotFoundException>(act);
            Assert.Equal($"The file(s) '{missingPath}' were not found.", exception.Message);
        }

        [Fact]
        public async Task LoadAsync_AllPathsDoNotExist_ThrowsFileNotFoundExceptionListingAllMissingPaths()
        {
            // Arrange
            ProfanityJsonDataSource dataSource = new();
            string firstMissingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
            string secondMissingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

            // Act
            Func<Task> act = () => dataSource.LoadAsync([firstMissingPath, secondMissingPath]);

            // Assert
            FileNotFoundException exception = await Assert.ThrowsAsync<FileNotFoundException>(act);
            Assert.Equal(
                $"The file(s) '{firstMissingPath}, {secondMissingPath}' were not found.",
                exception.Message);
        }

        [Fact]
        public async Task LoadAsync_ValidPathMixedWithMissingPath_ExceptionListsOnlyTheMissingPath()
        {
            // Arrange
            ProfanityJsonDataSource dataSource = new();
            string validPath = CreateTempJsonFile("[]");
            string missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

            // Act
            Func<Task> act = () => dataSource.LoadAsync([validPath, missingPath]);

            // Assert
            FileNotFoundException exception = await Assert.ThrowsAsync<FileNotFoundException>(act);
            Assert.Equal($"The file(s) '{missingPath}' were not found.", exception.Message);
        }

        [Fact]
        public async Task LoadAsync_EmptyJsonArray_ResultsInEmptyProfanitiesSet()
        {
            // Arrange
            ProfanityJsonDataSource dataSource = new();
            string path = CreateTempJsonFile("[]");

            // Act
            await dataSource.LoadAsync([path]);

            // Assert
            Assert.Empty(dataSource.Profanities);
        }

        [Fact]
        public async Task LoadAsync_ValidJsonFile_PopulatesProfanitiesWithPrecompiledMatchRegex()
        {
            // Arrange
            ProfanityJsonDataSource dataSource = new();
            const string json = """
                                [
                                  {
                                    "Id": "1",
                                    "Match": "badword",
                                    "Severity": 1,
                                    "Tags": ["profanity"],
                                    "Exceptions": []
                                  }
                                ]
                                """;
            string path = CreateTempJsonFile(json);

            // Act
            await dataSource.LoadAsync([path]);

            // Assert
            JsonProfanity profanity = Assert.Single(dataSource.Profanities);
            Assert.Equal("badword", profanity.Match);
            Assert.NotNull(profanity.MatchRegex);
            Assert.Matches(profanity.MatchRegex, "this contains a badword in it");
        }

        [Fact]
        public async Task LoadAsync_JsonPropertyNamesUseDifferentCasing_DeserializesSuccessfully()
        {
            // Arrange
            ProfanityJsonDataSource dataSource = new();
            const string json = """
                                [
                                  {
                                    "id": "1",
                                    "match": "badword",
                                    "severity": 1,
                                    "tags": [],
                                    "exceptions": []
                                  }
                                ]
                                """;
            string path = CreateTempJsonFile(json);

            // Act
            await dataSource.LoadAsync([path]);

            // Assert
            JsonProfanity profanity = Assert.Single(dataSource.Profanities);
            Assert.Equal("badword", profanity.Match);
        }

        [Fact]
        public async Task LoadAsync_MultipleValidFiles_CombinesProfanitiesFromAllFiles()
        {
            // Arrange
            ProfanityJsonDataSource dataSource = new();
            const string firstJson = """
                                     [ { "Id": "1", "Match": "wordone", "Severity": 1 } ]
                                     """;
            const string secondJson = """
                                      [ { "Id": "2", "Match": "wordtwo", "Severity": 2 } ]
                                      """;
            string firstPath = CreateTempJsonFile(firstJson);
            string secondPath = CreateTempJsonFile(secondJson);

            // Act
            await dataSource.LoadAsync([firstPath, secondPath]);

            // Assert
            Assert.Equal(2, dataSource.Profanities.Count);
            Assert.Contains(dataSource.Profanities, p => p.Match == "wordone");
            Assert.Contains(dataSource.Profanities, p => p.Match == "wordtwo");
        }

        [Fact]
        public async Task LoadAsync_MalformedJsonContent_ThrowsInvalidDataExceptionWrappingJsonException()
        {
            // Arrange
            ProfanityJsonDataSource dataSource = new();
            string path = CreateTempJsonFile("{ this is not valid json array");

            // Act
            Func<Task> act = () => dataSource.LoadAsync([path]);

            // Assert
            InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(act);
            Assert.Equal($"Failed to parse profanity data from '{path}'.", exception.Message);
            Assert.IsType<JsonException>(exception.InnerException);
        }

        [Fact]
        public async Task LoadAsync_CancellationTokenAlreadyCancelled_ThrowsTaskCanceledException()
        {
            // Arrange
            ProfanityJsonDataSource dataSource = new();
            const string json = """
                                [ { "Id": "1", "Match": "badword", "Severity": 1 } ]
                                """;
            string path = CreateTempJsonFile(json);
            CancellationToken cancellationToken = new(canceled: true);

            // Act
            Func<Task> act = () => dataSource.LoadAsync([path], cancellationToken);

            // Assert
            await Assert.ThrowsAsync<TaskCanceledException>(act);
        }
    }
}
