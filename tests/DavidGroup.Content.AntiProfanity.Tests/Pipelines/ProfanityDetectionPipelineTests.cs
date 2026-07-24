using DavidGroup.Content.AntiProfanity.DetectionHandlers;
using DavidGroup.Content.AntiProfanity.Pipelines;

using Moq;

namespace DavidGroup.Content.AntiProfanity.Tests.Pipelines;

/// <summary>
/// Unit tests for <see cref="ProfanityDetectionPipeline"/>.
/// </summary>
public static class ProfanityDetectionPipelineTests
{
    private static Mock<IProfanityDetectionHandler> CreatePassThroughHandlerMock(List<int> callOrder, int id)
    {
        Mock<IProfanityDetectionHandler> mock = new();
        mock.Setup(h => h.DetectAsync(It.IsAny<ProfanityDetectionContext>(), It.IsAny<NextProfanityDetectionHandlerDelegate>()))
            .Returns((ProfanityDetectionContext ctx, NextProfanityDetectionHandlerDelegate next) =>
            {
                callOrder.Add(id);
                return next.Invoke(ctx);
            });
        return mock;
    }

    /// <summary>
    /// Tests for <see cref="ProfanityDetectionPipeline.DetectAsync"/>.
    /// </summary>
    public class DetectAsyncTests
    {
        [Fact]
        public async Task DetectAsync_NoHandlersRegistered_CompletesWithoutError()
        {
            // Arrange
            ProfanityDetectionPipeline pipeline = new([]);
            ProfanityDetectionContext context = new();

            // Act
            Func<Task> act = () => pipeline.DetectAsync(context);

            // Assert
            Exception? exception = await Record.ExceptionAsync(act);
            Assert.Null(exception);
        }

        [Fact]
        public async Task DetectAsync_SingleHandlerRegistered_InvokesItOnceWithTheContext()
        {
            // Arrange
            Mock<IProfanityDetectionHandler> handlerMock = new();
            handlerMock
                .Setup(h => h.DetectAsync(It.IsAny<ProfanityDetectionContext>(), It.IsAny<NextProfanityDetectionHandlerDelegate>()))
                .Returns(Task.CompletedTask);

            ProfanityDetectionPipeline pipeline = new([handlerMock.Object]);
            ProfanityDetectionContext context = new();

            // Act
            await pipeline.DetectAsync(context);

            // Assert
            handlerMock.Verify(
                h => h.DetectAsync(context, It.IsAny<NextProfanityDetectionHandlerDelegate>()),
                Times.Once);
        }

        [Fact]
        public async Task DetectAsync_MultipleHandlersRegistered_InvokesThemInRegistrationOrder()
        {
            // Arrange
            List<int> callOrder = [];

            Mock<IProfanityDetectionHandler> firstHandlerMock = CreatePassThroughHandlerMock(callOrder, 1);
            Mock<IProfanityDetectionHandler> secondHandlerMock = CreatePassThroughHandlerMock(callOrder, 2);
            Mock<IProfanityDetectionHandler> thirdHandlerMock = CreatePassThroughHandlerMock(callOrder, 3);

            ProfanityDetectionPipeline pipeline =
                new([firstHandlerMock.Object, secondHandlerMock.Object, thirdHandlerMock.Object]);

            ProfanityDetectionContext context = new();

            // Act
            await pipeline.DetectAsync(context);

            // Assert
            Assert.Equal([1, 2, 3], callOrder);
        }

        [Fact]
        public async Task DetectAsync_HandlerDoesNotInvokeNext_SubsequentHandlersAreNeverCalled()
        {
            // Arrange
            Mock<IProfanityDetectionHandler> firstHandlerMock = new();
            firstHandlerMock
                .Setup(h => h.DetectAsync(It.IsAny<ProfanityDetectionContext>(), It.IsAny<NextProfanityDetectionHandlerDelegate>()))
                .Returns(Task.CompletedTask);
            Mock<IProfanityDetectionHandler> secondHandlerMock = new();
            secondHandlerMock
                .Setup(h => h.DetectAsync(It.IsAny<ProfanityDetectionContext>(), It.IsAny<NextProfanityDetectionHandlerDelegate>()))
                .Returns(Task.CompletedTask);

            ProfanityDetectionPipeline pipeline = new(
                [firstHandlerMock.Object, secondHandlerMock.Object]);

            ProfanityDetectionContext context = new();

            // Act
            await pipeline.DetectAsync(context);

            // Assert
            firstHandlerMock.Verify(
                h => h.DetectAsync(It.IsAny<ProfanityDetectionContext>(), It.IsAny<NextProfanityDetectionHandlerDelegate>()),
                Times.Once);

            secondHandlerMock.Verify(
                h => h.DetectAsync(It.IsAny<ProfanityDetectionContext>(), It.IsAny<NextProfanityDetectionHandlerDelegate>()),
                Times.Never);
        }

        [Fact]
        public async Task DetectAsync_MultipleHandlersRegistered_SameContextInstancePassedToEachHandler()
        {
            // Arrange
            List<ProfanityDetectionContext> capturedContexts = [];
            Mock<IProfanityDetectionHandler> firstHandlerMock = new();
            firstHandlerMock
                .Setup(h => h.DetectAsync(It.IsAny<ProfanityDetectionContext>(), It.IsAny<NextProfanityDetectionHandlerDelegate>()))
                .Returns((ProfanityDetectionContext ctx, NextProfanityDetectionHandlerDelegate next) =>
                {
                    capturedContexts.Add(ctx);
                    return next.Invoke(ctx);
                });
            Mock<IProfanityDetectionHandler> secondHandlerMock = new();
            secondHandlerMock
                .Setup(h => h.DetectAsync(It.IsAny<ProfanityDetectionContext>(), It.IsAny<NextProfanityDetectionHandlerDelegate>()))
                .Returns((ProfanityDetectionContext ctx, NextProfanityDetectionHandlerDelegate next) =>
                {
                    capturedContexts.Add(ctx);
                    return next.Invoke(ctx);
                });

            ProfanityDetectionPipeline pipeline = new([firstHandlerMock.Object, secondHandlerMock.Object]);

            ProfanityDetectionContext context = new();

            // Act
            await pipeline.DetectAsync(context);

            // Assert
            Assert.Equal(2, capturedContexts.Count);
            Assert.All(capturedContexts, ctx => Assert.Same(context, ctx));
        }

        [Fact]
        public async Task DetectAsync_HandlerThrows_ExceptionPropagatesToCaller()
        {
            // Arrange
            InvalidOperationException expectedException = new("boom");
            Mock<IProfanityDetectionHandler> handlerMock = new();
            handlerMock
                .Setup(h => h.DetectAsync(It.IsAny<ProfanityDetectionContext>(), It.IsAny<NextProfanityDetectionHandlerDelegate>()))
                .Returns(Task.FromException(expectedException));

            ProfanityDetectionPipeline pipeline = new([handlerMock.Object]);

            ProfanityDetectionContext context = new();

            // Act
            Func<Task> act = () => pipeline.DetectAsync(context);

            // Assert
            InvalidOperationException actualException = await Assert.ThrowsAsync<InvalidOperationException>(act);
            Assert.Same(expectedException, actualException);
        }

        [Fact]
        public async Task DetectAsync_EarlierHandlerThrows_LaterHandlersAreNeverInvoked()
        {
            // Arrange
            Mock<IProfanityDetectionHandler> firstHandlerMock = new();
            firstHandlerMock
                .Setup(h => h.DetectAsync(It.IsAny<ProfanityDetectionContext>(), It.IsAny<NextProfanityDetectionHandlerDelegate>()))
                .Returns(Task.FromException(new InvalidOperationException("boom")));

            Mock<IProfanityDetectionHandler> secondHandlerMock = new();
            secondHandlerMock
                .Setup(h => h.DetectAsync(It.IsAny<ProfanityDetectionContext>(), It.IsAny<NextProfanityDetectionHandlerDelegate>()))
                .Returns(Task.CompletedTask);

            ProfanityDetectionPipeline pipeline = new([firstHandlerMock.Object, secondHandlerMock.Object]);

            ProfanityDetectionContext context = new();

            // Act
            Func<Task> act = () => pipeline.DetectAsync(context);
            _ = await Record.ExceptionAsync(act);

            // Assert
            secondHandlerMock.Verify(
                h => h.DetectAsync(It.IsAny<ProfanityDetectionContext>(), It.IsAny<NextProfanityDetectionHandlerDelegate>()),
                Times.Never);
        }
    }
}
