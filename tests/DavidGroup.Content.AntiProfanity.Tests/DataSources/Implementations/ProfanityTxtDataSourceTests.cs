using DavidGroup.Content.AntiProfanity.DataSources.Implementations;

namespace DavidGroup.Content.AntiProfanity.Tests.DataSources.Implementations;

/// <summary>
/// Unit tests for <see cref="ProfanityTxtDataSource"/>.
/// </summary>
public static class ProfanityTxtDataSourceTests
{
    /// <summary>
    /// Tests for <see cref="ProfanityTxtDataSource.CanLoad(string)"/>.
    /// </summary>
    public class CanLoadTests
    {
        [Fact]
        public void CanLoad_TxtExtension_ReturnsTrue()
        {
            // Arrange
            ProfanityTxtDataSource dataSource = new();

            // Act
            bool result = dataSource.CanLoad(".txt");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanLoad_NonTxtExtension_ReturnsFalse()
        {
            // Arrange
            ProfanityTxtDataSource dataSource = new();

            // Act
            bool result = dataSource.CanLoad(".json");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanLoad_ExtensionDiffersOnlyByCasing_ReturnsFalse()
        {
            // Arrange
            ProfanityTxtDataSource dataSource = new();

            // Act
            bool result = dataSource.CanLoad(".TXT");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CanLoad_EmptyString_ReturnsFalse()
        {
            // Arrange
            ProfanityTxtDataSource dataSource = new();

            // Act
            bool result = dataSource.CanLoad(string.Empty);

            // Assert
            Assert.False(result);
        }
    }

    /// <summary>
    /// Tests for <see cref="ProfanityTxtDataSource.LoadAsync(IEnumerable{string}, CancellationToken)"/>.
    /// </summary>
    public class LoadAsyncTests : IDisposable
    {
        private readonly List<string> _createdFiles = [];

        private string CreateTempTxtFile(string content)
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
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
        public void LoadAsync_SinglePathDoesNotExist_ThrowsFileNotFoundExceptionWithPathInMessage()
        {
            // Arrange
            ProfanityTxtDataSource dataSource = new();
            string missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");

            // Act
            Action act = () => dataSource.LoadAsync([missingPath]);

            // Assert
            FileNotFoundException exception = Assert.Throws<FileNotFoundException>(act);
            Assert.Equal($"The file(s) '{missingPath}' were not found.", exception.Message);
        }

        [Fact]
        public void LoadAsync_AllPathsDoNotExist_ThrowsFileNotFoundExceptionListingAllMissingPaths()
        {
            // Arrange
            ProfanityTxtDataSource dataSource = new();
            string firstMissingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
            string secondMissingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");

            // Act
            Action act = () => dataSource.LoadAsync([firstMissingPath, secondMissingPath]);

            // Assert
            FileNotFoundException exception = Assert.Throws<FileNotFoundException>(act);
            Assert.Equal(
                $"The file(s) '{firstMissingPath}, {secondMissingPath}' were not found.",
                exception.Message);
        }

        [Fact]
        public void LoadAsync_ValidPathMixedWithMissingPath_ExceptionListsOnlyTheMissingPath()
        {
            // Arrange
            ProfanityTxtDataSource dataSource = new();
            string validPath = CreateTempTxtFile("word");
            string missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");

            // Act
            Action act = () => dataSource.LoadAsync([validPath, missingPath]);

            // Assert
            FileNotFoundException exception = Assert.Throws<FileNotFoundException>(act);
            Assert.Equal($"The file(s) '{missingPath}' were not found.", exception.Message);
        }

        [Fact]
        public async Task LoadAsync_ValidFile_PopulatesProfanitiesWithTrimmedNonEmptyLines()
        {
            // Arrange
            ProfanityTxtDataSource dataSource = new();
            string path = CreateTempTxtFile("word1\n\n  word2  \n   \nword3\n");

            // Act
            await dataSource.LoadAsync([path]);

            // Assert
            Assert.Equal(3, dataSource.Profanities.Count);
            Assert.Contains("word1", dataSource.Profanities.AsEnumerable());
            Assert.Contains("word2", dataSource.Profanities.AsEnumerable());
            Assert.Contains("word3", dataSource.Profanities.AsEnumerable());
        }

        [Fact]
        public async Task LoadAsync_LinesWithOnlyWhitespace_AreExcluded()
        {
            // Arrange
            ProfanityTxtDataSource dataSource = new();
            string path = CreateTempTxtFile("   \n\t\n\n");

            // Act
            await dataSource.LoadAsync([path]);

            // Assert
            Assert.Empty(dataSource.Profanities);
        }

        [Fact]
        public async Task LoadAsync_EmptyFile_ResultsInEmptyProfanitiesSet()
        {
            // Arrange
            ProfanityTxtDataSource dataSource = new();
            string path = CreateTempTxtFile(string.Empty);

            // Act
            await dataSource.LoadAsync([path]);

            // Assert
            Assert.Empty(dataSource.Profanities);
        }

        [Fact]
        public async Task LoadAsync_MultipleValidFiles_CombinesLinesFromAllFiles()
        {
            // Arrange
            ProfanityTxtDataSource dataSource = new();
            string firstPath = CreateTempTxtFile("wordone");
            string secondPath = CreateTempTxtFile("wordtwo");

            // Act
            await dataSource.LoadAsync([firstPath, secondPath]);

            // Assert
            Assert.Equal(2, dataSource.Profanities.Count);
            Assert.Contains("wordone", dataSource.Profanities.AsEnumerable());
            Assert.Contains("wordtwo", dataSource.Profanities.AsEnumerable());
        }

        [Fact]
        public async Task LoadAsync_DuplicateLinesDifferingOnlyByCasing_AreDeduplicated()
        {
            // Arrange
            ProfanityTxtDataSource dataSource = new();
            string firstPath = CreateTempTxtFile("word");
            string secondPath = CreateTempTxtFile("WORD\nWord");

            // Act
            await dataSource.LoadAsync([firstPath, secondPath]);

            // Assert
            Assert.Single(dataSource.Profanities);
            Assert.Contains("word", dataSource.Profanities.AsEnumerable());
            Assert.Contains("WORD", dataSource.Profanities.AsEnumerable());
        }
    }
}
