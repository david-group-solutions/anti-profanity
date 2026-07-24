using DavidGroup.Content.AntiProfanity.DataSources;
using DavidGroup.Content.AntiProfanity.Extensions;
using DavidGroup.Content.AntiProfanity.Options;

using Microsoft.Extensions.DependencyInjection;

using Moq;

namespace DavidGroup.Content.AntiProfanity.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="ServiceProviderExtensions"/>.
/// </summary>
public static class ServiceProviderExtensionsTests
{
    private static ServiceProvider CreateServiceProvider(
        string dataSourcesBasePath,
        string[] dataSourceFileNames,
        params IProfanityDataSource[] dataSources)
    {
        ServiceCollection services = new();
        AntiProfanityOptions options = new()
        {
            DataSourcesBasePath = dataSourcesBasePath,
            DataSources = dataSourceFileNames
        };
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        foreach (IProfanityDataSource dataSource in dataSources)
            services.AddSingleton(dataSource);

        return services.BuildServiceProvider();
    }

    private static string ExpectedPath(string basePath, string fileName)
        => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, basePath, fileName);

    /// <summary>
    /// Tests for <see cref="ServiceProviderExtensions.InitializeAntiProfanityDataSourcesAsync"/>.
    /// </summary>
    public class InitializeAntiProfanityDataSourcesAsyncTests
    {
        [Fact]
        public async Task InitializeAntiProfanityDataSourcesAsync_SingleMatchingFile_CallsLoadAsyncWithFullPath()
        {
            // Arrange
            Mock<IProfanityDataSource> jsonDataSourceMock = new();
            jsonDataSourceMock.Setup(x => x.CanLoad(".json")).Returns(true);
            jsonDataSourceMock
                .Setup(x => x.LoadAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await using ServiceProvider provider = CreateServiceProvider(
                "Data", ["profanities.json"], jsonDataSourceMock.Object);
            string expectedPath = ExpectedPath("Data", "profanities.json");

            // Act
            await provider.InitializeAntiProfanityDataSourcesAsync();

            // Assert
            jsonDataSourceMock.Verify(
                x => x.LoadAsync(
                    It.Is<IEnumerable<string>>(paths => paths.SequenceEqual(new[]
                    {
                        expectedPath
                    })),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task InitializeAntiProfanityDataSourcesAsync_MultipleFilesWithSameExtension_GroupsThemIntoOneCall()
        {
            // Arrange
            List<string>? capturedPaths = null;

            Mock<IProfanityDataSource> txtDataSourceMock = new();
            txtDataSourceMock.Setup(x => x.CanLoad(".txt")).Returns(true);
            txtDataSourceMock
                .Setup(x => x.LoadAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<string>, CancellationToken>((paths, _) => capturedPaths = paths.ToList())
                .Returns(Task.CompletedTask);

            await using ServiceProvider provider = CreateServiceProvider(
                "Data", ["a.txt", "b.txt", "c.txt"], txtDataSourceMock.Object);

            List<string> expectedPaths =
            [
                ExpectedPath("Data", "a.txt"),
                ExpectedPath("Data", "b.txt"),
                ExpectedPath("Data", "c.txt")
            ];

            // Act
            await provider.InitializeAntiProfanityDataSourcesAsync();

            // Assert
            txtDataSourceMock.Verify(
                x => x.LoadAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Once
            );

            Assert.NotNull(capturedPaths);
            Assert.Equal(expectedPaths, capturedPaths);
        }

        [Fact]
        public async Task InitializeAntiProfanityDataSourcesAsync_MultipleExtensions_RoutesEachGroupToMatchingDataSource()
        {
            // Arrange
            Mock<IProfanityDataSource> jsonDataSourceMock = new();
            jsonDataSourceMock.Setup(x => x.CanLoad(".json")).Returns(true);
            jsonDataSourceMock.Setup(x => x.CanLoad(".txt")).Returns(false);
            jsonDataSourceMock
                .Setup(x => x.LoadAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Mock<IProfanityDataSource> txtDataSourceMock = new();
            txtDataSourceMock.Setup(x => x.CanLoad(".txt")).Returns(true);
            txtDataSourceMock.Setup(x => x.CanLoad(".json")).Returns(false);
            txtDataSourceMock
                .Setup(x => x.LoadAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            await using ServiceProvider provider = CreateServiceProvider(
                "Data",
                ["a.json", "b.txt"],
                jsonDataSourceMock.Object,
                txtDataSourceMock.Object);
            string expectedJsonPath = ExpectedPath("Data", "a.json");
            string expectedTxtPath = ExpectedPath("Data", "b.txt");

            // Act
            await provider.InitializeAntiProfanityDataSourcesAsync();

            // Assert
            jsonDataSourceMock.Verify(
                x => x.LoadAsync(
                    It.Is<IEnumerable<string>>(paths => paths.SequenceEqual(new[]
                    {
                        expectedJsonPath
                    })),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );

            txtDataSourceMock.Verify(x => x.LoadAsync(
                    It.Is<IEnumerable<string>>(paths => paths.SequenceEqual(new[]
                    {
                        expectedTxtPath
                    })),
                    It.IsAny<CancellationToken>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task InitializeAntiProfanityDataSourcesAsync_NoDataSourceSupportsExtension_ThrowsInvalidOperationException()
        {
            // Arrange
            Mock<IProfanityDataSource> jsonDataSourceMock = new();
            jsonDataSourceMock.Setup(x => x.CanLoad(It.IsAny<string>())).Returns(false);

            await using ServiceProvider provider = CreateServiceProvider(
                "Data", ["file.xml"], jsonDataSourceMock.Object);

            // Act & Assert
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(()
                => provider.InitializeAntiProfanityDataSourcesAsync());

            Assert.Equal("No loader registered for '.xml'.", exception.Message);
        }

        [Fact]
        public async Task InitializeAntiProfanityDataSourcesAsync_FileNameHasNoExtension_NotSupportedException()
        {
            // Arrange
            Mock<IProfanityDataSource> noExtensionDataSourceMock = new();

            await using ServiceProvider provider = CreateServiceProvider(
                "Data", ["profanities"], noExtensionDataSourceMock.Object);
            string expectedPath = ExpectedPath("Data", "profanities");

            // Act & Assert
            NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(()
                => provider.InitializeAntiProfanityDataSourcesAsync());

            Assert.Equal($"The file '{expectedPath}' does not have a supported extension.", exception.Message);
        }

        [Fact]
        public async Task InitializeAntiProfanityDataSourcesAsync_NoConfiguredFiles_NeverCallsAnyDataSource()
        {
            // Arrange
            Mock<IProfanityDataSource> dataSourceMock = new();
            await using ServiceProvider provider = CreateServiceProvider("Data", [], dataSourceMock.Object);

            // Act
            await provider.InitializeAntiProfanityDataSourcesAsync();

            // Assert
            dataSourceMock.Verify(x => x.CanLoad(It.IsAny<string>()), Times.Never);
            dataSourceMock.Verify(
                x => x.LoadAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
                Times.Never
            );
        }
    }
}
