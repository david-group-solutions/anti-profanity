using System.Text.RegularExpressions;

using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.Tests.Models;

/// <summary>
/// Unit tests for <see cref="JsonProfanity"/>.
/// </summary>
public static class JsonProfanityTests
{
    /// <summary>
    /// Tests for <see cref="JsonProfanity.PrecompileValues"/>.
    /// </summary>
    public class PrecompileValuesTests
    {
        [Fact]
        public void PrecompileValues_AlwaysCompiles_MatchRegexHasCompiledIgnoreCaseAndCultureInvariantOptions()
        {
            // Arrange
            JsonProfanity profanity = new()
            {
                Match = "swearword"
            };

            // Act
            profanity.PrecompileValues();

            // Assert
            Assert.True(profanity.MatchRegex.Options.HasFlag(RegexOptions.Compiled));
            Assert.True(profanity.MatchRegex.Options.HasFlag(RegexOptions.IgnoreCase));
            Assert.True(profanity.MatchRegex.Options.HasFlag(RegexOptions.CultureInvariant));
        }

        [Fact]
        public void PrecompileValues_PartialMatchTrue_MatchRegexMatchesSubstringWithinLargerWord()
        {
            // Arrange
            JsonProfanity profanity = new()
            {
                Match = "ass",
                PartialMatch = true
            };

            // Act
            profanity.PrecompileValues();

            // Assert
            Assert.Matches(profanity.MatchRegex, "class");
            Assert.Matches(profanity.MatchRegex, "ass");
        }

        [Fact]
        public void PrecompileValues_PartialMatchFalse_MatchRegexDoesNotMatchSubstringWithinLargerWord()
        {
            // Arrange
            JsonProfanity profanity = new()
            {
                Match = "ass",
                PartialMatch = false
            };

            // Act
            profanity.PrecompileValues();

            // Assert
            Assert.DoesNotMatch(profanity.MatchRegex, "class");
        }

        [Fact]
        public void PrecompileValues_PartialMatchFalse_MatchRegexMatchesStandaloneWholeWord()
        {
            // Arrange
            JsonProfanity profanity = new()
            {
                Match = "ass",
                PartialMatch = false
            };

            // Act
            profanity.PrecompileValues();

            // Assert
            Assert.Matches(profanity.MatchRegex, "ass");
            Assert.Matches(profanity.MatchRegex, "my ass hurts");
        }

        [Fact]
        public void PrecompileValues_MatchContainsPipeSeparatedValues_MatchesEitherAlternative()
        {
            // Arrange
            JsonProfanity profanity = new()
            {
                Match = "cat|dog"
            };

            // Act
            profanity.PrecompileValues();

            // Assert
            Assert.Matches(profanity.MatchRegex, "I have a cat");
            Assert.Matches(profanity.MatchRegex, "I have a dog");
            Assert.DoesNotMatch(profanity.MatchRegex, "I have a bird");
        }

        [Fact]
        public void PrecompileValues_MatchContainsPlus_ReplacesItWithOneOrMoreQuantifier()
        {
            // Arrange
            JsonProfanity profanity = new()
            {
                Match = "da*n"
            };

            // Act
            profanity.PrecompileValues();

            // Assert
            Assert.Matches(profanity.MatchRegex, "dan");
            Assert.Matches(profanity.MatchRegex, "daaan");
            Assert.DoesNotMatch(profanity.MatchRegex, "dn");
        }

        [Fact]
        public void PrecompileValues_NoExceptions_ExceptionRegexesRemainsEmpty()
        {
            // Arrange
            JsonProfanity profanity = new()
            {
                Match = "swearword",
                Exceptions = []
            };

            // Act
            profanity.PrecompileValues();

            // Assert
            Assert.Empty(profanity.ExceptionRegexes);
        }

        [Fact]
        public void PrecompileValues_ExceptionWithoutWildcard_OnlyMatchesExactWordAnchored()
        {
            // Arrange
            JsonProfanity profanity = new()
            {
                Match = "grass",
                Exceptions = ["grass"]
            };

            // Act
            profanity.PrecompileValues();

            // Assert
            Regex exceptionRegex = Assert.Single(profanity.ExceptionRegexes);
            Assert.Matches(exceptionRegex, "grass");
            Assert.Matches(exceptionRegex, "Grass");
            Assert.DoesNotMatch(exceptionRegex, "grasses");
            Assert.DoesNotMatch(exceptionRegex, "thegrass");
        }

        [Fact]
        public void PrecompileValues_ExceptionWithWildcard_MatchesWordsSharingThePrefix()
        {
            // Arrange
            JsonProfanity profanity = new()
            {
                Match = "class",
                Exceptions = ["class*"]
            };

            // Act
            profanity.PrecompileValues();

            // Assert
            Regex exceptionRegex = Assert.Single(profanity.ExceptionRegexes);
            Assert.Matches(exceptionRegex, "class");
            Assert.Matches(exceptionRegex, "classes");
            Assert.Matches(exceptionRegex, "classroom");
            Assert.DoesNotMatch(exceptionRegex, "subclass");
        }

        [Fact]
        public void PrecompileValues_ExceptionRegex_HasCompiledIgnoreCaseAndCultureInvariantOptions()
        {
            // Arrange
            JsonProfanity profanity = new()
            {
                Match = "swearword",
                Exceptions = ["grass"]
            };

            // Act
            profanity.PrecompileValues();

            // Assert
            Regex exceptionRegex = Assert.Single(profanity.ExceptionRegexes);
            Assert.True(exceptionRegex.Options.HasFlag(RegexOptions.Compiled));
            Assert.True(exceptionRegex.Options.HasFlag(RegexOptions.IgnoreCase));
            Assert.True(exceptionRegex.Options.HasFlag(RegexOptions.CultureInvariant));
        }

        [Fact]
        public void PrecompileValues_MultipleExceptions_CompilesOneRegexPerExceptionInOrder()
        {
            // Arrange
            JsonProfanity profanity = new()
            {
                Match = "swearword",
                Exceptions = ["grass", "class*"]
            };

            // Act
            profanity.PrecompileValues();

            // Assert
            Assert.Equal(2, profanity.ExceptionRegexes.Count);

            Regex firstExceptionRegex = profanity.ExceptionRegexes[0];
            Assert.Matches(firstExceptionRegex, "grass");
            Assert.DoesNotMatch(firstExceptionRegex, "classroom");

            Regex secondExceptionRegex = profanity.ExceptionRegexes[1];
            Assert.Matches(secondExceptionRegex, "classroom");
            Assert.DoesNotMatch(secondExceptionRegex, "grass");
        }
    }
}
