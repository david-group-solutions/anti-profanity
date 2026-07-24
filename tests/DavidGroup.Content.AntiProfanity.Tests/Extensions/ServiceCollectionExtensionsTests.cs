using System.Reflection;

using DavidGroup.Content.AntiProfanity.DataSources;
using DavidGroup.Content.AntiProfanity.DataSources.Implementations;
using DavidGroup.Content.AntiProfanity.DetectionHandlers;
using DavidGroup.Content.AntiProfanity.Extensions;
using DavidGroup.Content.AntiProfanity.Pipelines;
using DavidGroup.Content.AntiProfanity.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DavidGroup.Content.AntiProfanity.Tests.Extensions;

/// <summary>
/// A no-op <see cref="IProfanityDataSource"/> implementation living in the test
/// assembly, used to verify that <see cref="DavidGroup.Content.AntiProfanity.Extensions.ServiceCollectionExtensions.AddAntiProfanity"/>
/// also scans an additional assembly supplied by the caller.
/// </summary>
file sealed class StubProfanityDataSource : IProfanityDataSource
{
    public bool CanLoad(string extension) => false;

    public Task LoadAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

/// <summary>
/// No-op <see cref="IProfanityDetectionHandler"/> implementations used to verify
/// <see cref="DavidGroup.Content.AntiProfanity.Extensions.ServiceCollectionExtensions.AddHandler{THandler}"/> registration behavior.
/// </summary>
file sealed class StubProfanityDetectionHandlerA : IProfanityDetectionHandler
{
    public Task DetectAsync(ProfanityDetectionContext context, NextProfanityDetectionHandlerDelegate? next)
        => Task.CompletedTask;
}

file sealed class StubProfanityDetectionHandlerB : IProfanityDetectionHandler
{
    public Task DetectAsync(ProfanityDetectionContext context, NextProfanityDetectionHandlerDelegate? next)
        => Task.CompletedTask;
}

file sealed class StubProfanityDetectionHandlerC : IProfanityDetectionHandler
{
    public Task DetectAsync(ProfanityDetectionContext context, NextProfanityDetectionHandlerDelegate? next)
        => Task.CompletedTask;
}

/// <summary>
/// Unit tests for <see cref="DavidGroup.Content.AntiProfanity.Extensions.ServiceCollectionExtensions"/>.
/// </summary>
public static class ServiceCollectionExtensionsTests
{
    private static IConfiguration CreateEmptyConfiguration() => new ConfigurationBuilder().Build();

    /// <summary>
    /// Tests for <see cref="DavidGroup.Content.AntiProfanity.Extensions.ServiceCollectionExtensions.AddAntiProfanity"/>.
    /// </summary>
    public class AddAntiProfanityTests
    {
        [Fact]
        public void AddAntiProfanity_ReturnsTheSameServiceCollectionInstance()
        {
            // Arrange
            ServiceCollection services = new();
            IConfiguration configuration = CreateEmptyConfiguration();

            // Act
            IServiceCollection result = services.AddAntiProfanity(configuration);

            // Assert
            Assert.Same(services, result);
        }

        [Fact]
        public void AddAntiProfanity_RegistersProfanityDetectionPipelineAsSingleton()
        {
            // Arrange
            ServiceCollection services = new();
            IConfiguration configuration = CreateEmptyConfiguration();

            // Act
            services.AddAntiProfanity(configuration);

            // Assert
            ServiceDescriptor descriptor = Assert.Single(
                services, d => d.ServiceType == typeof(IProfanityDetectionPipeline));
            Assert.Equal(typeof(ProfanityDetectionPipeline), descriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }

        [Fact]
        public void AddAntiProfanity_RegistersAntiProfanityServiceAsSingleton()
        {
            // Arrange
            ServiceCollection services = new();
            IConfiguration configuration = CreateEmptyConfiguration();

            // Act
            services.AddAntiProfanity(configuration);

            // Assert
            ServiceDescriptor descriptor = Assert.Single(
                services, d => d.ServiceType == typeof(IAntiProfanityService));
            Assert.Equal(typeof(AntiProfanityService), descriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        }

        [Fact]
        public void AddAntiProfanity_ScansOwnAssembly_RegistersKnownDataSourcesAsSingleton()
        {
            // Arrange
            ServiceCollection services = new();
            IConfiguration configuration = CreateEmptyConfiguration();

            // Act
            services.AddAntiProfanity(configuration);

            // Assert
            List<ServiceDescriptor> dataSourceDescriptors = services
                .Where(d => d.ServiceType == typeof(IProfanityDataSource))
                .ToList();

            Assert.Equal(2, dataSourceDescriptors.Count);
            Assert.Contains(
                dataSourceDescriptors,
                d => d.ImplementationType == typeof(ProfanityJsonDataSource) && d.Lifetime == ServiceLifetime.Singleton);
            Assert.Contains(
                dataSourceDescriptors,
                d => d.ImplementationType == typeof(ProfanityTxtDataSource) && d.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void AddAntiProfanity_NoAdditionalAssemblyProvided_DoesNotRegisterDataSourcesFromOtherAssemblies()
        {
            // Arrange
            ServiceCollection services = new();
            IConfiguration configuration = CreateEmptyConfiguration();

            // Act
            services.AddAntiProfanity(configuration);

            // Assert
            Assert.DoesNotContain(
                services,
                d => d.ServiceType == typeof(IProfanityDataSource) && d.ImplementationType == typeof(StubProfanityDataSource));
        }

        [Fact]
        public void AddAntiProfanity_AdditionalAssemblyProvided_AlsoRegistersItsDataSourceImplementations()
        {
            // Arrange
            ServiceCollection services = new();
            IConfiguration configuration = CreateEmptyConfiguration();
            Assembly testAssembly = typeof(StubProfanityDataSource).Assembly;

            // Act
            services.AddAntiProfanity(configuration, testAssembly);

            // Assert
            List<ServiceDescriptor> dataSourceDescriptors = services
                .Where(d => d.ServiceType == typeof(IProfanityDataSource))
                .ToList();

            Assert.Equal(3, dataSourceDescriptors.Count);
            Assert.Contains(
                dataSourceDescriptors,
                d => d.ImplementationType == typeof(StubProfanityDataSource) && d.Lifetime == ServiceLifetime.Singleton);
            Assert.Contains(dataSourceDescriptors, d => d.ImplementationType == typeof(ProfanityJsonDataSource));
            Assert.Contains(dataSourceDescriptors, d => d.ImplementationType == typeof(ProfanityTxtDataSource));
        }

        [Fact]
        public void AddAntiProfanity_ConfigurationHasNoAntiProfanitySection_DoesNotThrowDuringRegistration()
        {
            // Arrange
            ServiceCollection services = new();
            IConfiguration configuration = CreateEmptyConfiguration();

            // Act
            Exception? exception = Record.Exception(() => services.AddAntiProfanity(configuration));

            // Assert
            Assert.Null(exception);
        }
    }

    /// <summary>
    /// Tests for <see cref="DavidGroup.Content.AntiProfanity.Extensions.ServiceCollectionExtensions.AddHandler{THandler}"/>.
    /// </summary>
    public class AddHandlerTests
    {
        [Fact]
        public void AddHandler_ReturnsTheSameServiceCollectionInstance()
        {
            // Arrange
            ServiceCollection services = new();

            // Act
            IServiceCollection result = services.AddHandler<StubProfanityDetectionHandlerA>();

            // Assert
            Assert.Same(services, result);
        }

        [Fact]
        public void AddHandler_RegistersHandlerAsTransient()
        {
            // Arrange
            ServiceCollection services = new();

            // Act
            services.AddHandler<StubProfanityDetectionHandlerA>();

            // Assert
            ServiceDescriptor descriptor = Assert.Single(
                services, d => d.ServiceType == typeof(IProfanityDetectionHandler));
            Assert.Equal(typeof(StubProfanityDetectionHandlerA), descriptor.ImplementationType);
            Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        }

        [Fact]
        public void AddHandler_CalledMultipleTimesWithDifferentHandlers_RegistersEachAsSeparateDescriptor()
        {
            // Arrange
            ServiceCollection services = new();

            // Act
            services.AddHandler<StubProfanityDetectionHandlerA>();
            services.AddHandler<StubProfanityDetectionHandlerB>();
            services.AddHandler<StubProfanityDetectionHandlerC>();

            // Assert
            int handlerDescriptorCount = services.Count(d => d.ServiceType == typeof(IProfanityDetectionHandler));
            Assert.Equal(3, handlerDescriptorCount);
        }

        [Fact]
        public void AddHandler_CalledMultipleTimesWithDifferentHandlers_ResolvedEnumerableRespectsRegistrationOrder()
        {
            // Arrange
            ServiceCollection services = new();
            services.AddHandler<StubProfanityDetectionHandlerA>();
            services.AddHandler<StubProfanityDetectionHandlerB>();
            services.AddHandler<StubProfanityDetectionHandlerC>();

            // Act
            using ServiceProvider provider = services.BuildServiceProvider();
            List<IProfanityDetectionHandler> resolvedHandlers = provider
                .GetServices<IProfanityDetectionHandler>()
                .ToList();

            // Assert
            Assert.Equal(3, resolvedHandlers.Count);
            Assert.IsType<StubProfanityDetectionHandlerA>(resolvedHandlers[0]);
            Assert.IsType<StubProfanityDetectionHandlerB>(resolvedHandlers[1]);
            Assert.IsType<StubProfanityDetectionHandlerC>(resolvedHandlers[2]);
        }
    }
}
