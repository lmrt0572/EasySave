using Xunit;
using EasySave.Core.Services;

namespace EasySave.Tests.Services
{
    // ===== COMMAND LINE PARSER TESTS =====
    // Coverage: parsing single index, range, list (;),
    //           invalid inputs, error state, reset between calls.

    public class ServiceCommandLineParserTests
    {
        private readonly ServiceCommandLineParser _parser = new();

        // ==========================================
        // ===== SINGLE INDEX TESTS =====
        // ==========================================

        [Theory]
        [InlineData("1", new[] { 0 })]    // index 1 → 0-based = 0
        [InlineData("3", new[] { 2 })]    // index 3 → 0-based = 2
        [InlineData("10", new[] { 9 })]   // grands index acceptés (illimité v3)
        [InlineData("100", new[] { 99 })] // très grand index accepté
        public void Parse_SingleValidIndex_ReturnsCorrectZeroBased(string input, int[] expected)
        {
            // A valid index (≥ 1) must be converted to base 0 without error
            var result = _parser.Parse(new[] { input });

            Assert.False(_parser.HasError);
            Assert.Equal(expected, result.ToArray());
        }

        [Theory]
        [InlineData("0")]  // strictement en dessous du minimum (1)
        [InlineData("-1")] // négatif
        public void Parse_IndexBelowMinimum_SetsError(string input)
        {
            // Only index 0 and negatives are out of bounds (minimum = 1)
            var result = _parser.Parse(new[] { input });

            Assert.True(_parser.HasError);
            Assert.Empty(result);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("x")]
        [InlineData("1.5")]
        public void Parse_SingleNonNumeric_SetsError(string input)
        {
            // Non-numeric input must trigger HasError
            var result = _parser.Parse(new[] { input });

            Assert.True(_parser.HasError);
            Assert.Empty(result);
        }

        // ==========================================
        // ===== RANGE TESTS ("1-3") =====
        // ==========================================

        [Fact]
        public void Parse_Range_1To3_ReturnsThreeIndices()
        {
            // "1-3" → zero-based indices: 0, 1, 2
            var result = _parser.Parse(new[] { "1-3" }).ToArray();

            Assert.False(_parser.HasError);
            Assert.Equal(new[] { 0, 1, 2 }, result);
        }

        [Fact]
        public void Parse_Range_1To5_ReturnsFiveIndices()
        {
            // "1-5" → 5 consecutive indices
            var result = _parser.Parse(new[] { "1-5" }).ToArray();

            Assert.False(_parser.HasError);
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, result);
        }

        [Fact]
        public void Parse_Range_LargeRange_Accepted()
        {
            // In v3, jobs are unlimited: "1-10" should be accepted
            var result = _parser.Parse(new[] { "1-10" }).ToArray();

            Assert.False(_parser.HasError);
            Assert.Equal(10, result.Length);
            Assert.Equal(0, result[0]);
            Assert.Equal(9, result[9]);
        }

        [Fact]
        public void Parse_Range_SameStartAndEnd_ReturnsSingleIndex()
        {
            // "2-2" is valid and returns a single index
            var result = _parser.Parse(new[] { "2-2" }).ToArray();

            Assert.False(_parser.HasError);
            Assert.Equal(new[] { 1 }, result);
        }

        [Fact]
        public void Parse_Range_Reversed_SetsError()
        {
            // "3-1" is a reversed range: error expected
            var result = _parser.Parse(new[] { "3-1" });

            Assert.True(_parser.HasError);
            Assert.Empty(result);
        }

        // ==========================================
        // ===== LIST TESTS ("1;3;5") =====
        // ==========================================

        [Fact]
        public void Parse_List_ValidIndices_ReturnsCorrectZeroBased()
        {
            // "1;3;5" → 0, 2, 4
            var result = _parser.Parse(new[] { "1;3;5" }).ToArray();

            Assert.False(_parser.HasError);
            Assert.Equal(new[] { 0, 2, 4 }, result);
        }

        [Fact]
        public void Parse_List_DuplicateIndices_ReturnsUnique()
        {
            // Duplicates must be deduplicated
            var result = _parser.Parse(new[] { "2;2;3" }).ToArray();

            Assert.False(_parser.HasError);
            Assert.Equal(new[] { 1, 2 }, result);
        }

        [Fact]
        public void Parse_List_AllInvalid_SetsError()
        {
            // All invalid elements → HasError, empty list
            var result = _parser.Parse(new[] { "0;-1" });

            Assert.True(_parser.HasError);
            Assert.Empty(result);
        }

        // ==========================================
        // ===== NULL / EMPTY INPUT TESTS =====
        // ==========================================

        [Fact]
        public void Parse_NullArgs_SetsError()
        {
            // null args → immediate HasError
            var result = _parser.Parse(null!);

            Assert.True(_parser.HasError);
            Assert.Empty(result);
        }

        [Fact]
        public void Parse_EmptyArray_SetsError()
        {
            // Empty array → HasError
            var result = _parser.Parse(Array.Empty<string>());

            Assert.True(_parser.HasError);
            Assert.Empty(result);
        }

        [Fact]
        public void Parse_WhitespaceOnly_SetsError()
        {
            // Whitespace-only input → HasError
            var result = _parser.Parse(new[] { "   " });

            Assert.True(_parser.HasError);
            Assert.Empty(result);
        }

        // ==========================================
        // ===== ERROR STATE RESET =====
        // ==========================================

        [Fact]
        public void Parse_ResetsErrorState_BetweenCalls()
        {
            // HasError should be reset on each Parse call
            _parser.Parse(new[] { "abc" });
            Assert.True(_parser.HasError);

            _parser.Parse(new[] { "1" });
            Assert.False(_parser.HasError);
            Assert.Empty(_parser.ErrorMessage);
        }
    }
}