using Xunit;
using EasySave.Core.Services;

namespace EasySave.Tests.Services
{
    /* ===== COMMAND LINE PARSER TESTS =====
    public class ServiceCommandLineParserTests
    {
        private readonly ServiceCommandLineParser _parser = new();

        // ==========================================
        // ===== SINGLE INDEX TESTS =====
        // ==========================================

        [Theory]
        [InlineData("1", new[] { 0 })]       // index 1 → 0-based = 0
        [InlineData("3", new[] { 2 })]       // index 3 → 0-based = 2
        [InlineData("5", new[] { 4 })]       // index 5 → 0-based = 4
        public void Parse_SingleValidIndex_ReturnsCorrectZeroBased(string input, int[] expected)
        {
            var result = _parser.Parse(new[] { input });

            Assert.False(_parser.HasError);
            Assert.Equal(expected, result.ToArray());
        }

        [Theory]
        [InlineData("0")]   // below min
        [InlineData("6")]   // above max
        [InlineData("99")]  // way above max
        public void Parse_SingleOutOfRange_SetsError(string input)
        {
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
            var result = _parser.Parse(new[] { "1-3" }).ToArray();

            Assert.False(_parser.HasError);
            Assert.Equal(new[] { 0, 1, 2 }, result);
        }

        [Fact]
        public void Parse_Range_1To5_ReturnsAllFive()
        {
            var result = _parser.Parse(new[] { "1-5" }).ToArray();

            Assert.False(_parser.HasError);
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, result);
        }

        [Fact]
        public void Parse_Range_SameStartAndEnd_ReturnsSingleIndex()
        {
            var result = _parser.Parse(new[] { "2-2" }).ToArray();

            Assert.False(_parser.HasError);
            Assert.Equal(new[] { 1 }, result);
        }

        [Fact]
        public void Parse_Range_Reversed_SetsError()
        {
            var result = _parser.Parse(new[] { "3-1" });

            Assert.True(_parser.HasError);
            Assert.Empty(result);
        }

        [Fact]
        public void Parse_Range_PartiallyOutOfBounds_ReturnsValidOnly()
        {
            // "3-7" → only 3,4,5 are valid (max is 5)
            var result = _parser.Parse(new[] { "3-7" }).ToArray();

            Assert.Contains(2, result); // index 3 → 0-based 2
            Assert.Contains(3, result); // index 4 → 0-based 3
            Assert.Contains(4, result); // index 5 → 0-based 4
        }

        // ==========================================
        // ===== LIST TESTS ("1;3;5") =====
        // ==========================================

        [Fact]
        public void Parse_List_ValidIndices_ReturnsCorrectZeroBased()
        {
            var result = _parser.Parse(new[] { "1;3;5" }).ToArray();

            Assert.False(_parser.HasError);
            Assert.Equal(new[] { 0, 2, 4 }, result);
        }

        [Fact]
        public void Parse_List_WithSpaces_HandlesCorrectly()
        {
            // Note: string.Concat(args) is used, so spaces in separate args are concatenated
            var result = _parser.Parse(new[] { "1;3" }).ToArray();

            Assert.False(_parser.HasError);
            Assert.Equal(new[] { 0, 2 }, result);
        }

        [Fact]
        public void Parse_List_DuplicateIndices_ReturnsUnique()
        {
            var result = _parser.Parse(new[] { "2;2;3" }).ToArray();

            Assert.False(_parser.HasError);
            Assert.Equal(new[] { 1, 2 }, result); // 2 appears once (0-based)
        }

        [Fact]
        public void Parse_List_AllInvalid_SetsError()
        {
            var result = _parser.Parse(new[] { "0;6;99" });

            Assert.True(_parser.HasError);
            Assert.Empty(result);
        }

        // ==========================================
        // ===== NULL / EMPTY INPUT TESTS =====
        // ==========================================

        [Fact]
        public void Parse_NullArgs_SetsError()
        {
            var result = _parser.Parse(null!);

            Assert.True(_parser.HasError);
            Assert.Empty(result);
        }

        [Fact]
        public void Parse_EmptyArray_SetsError()
        {
            var result = _parser.Parse(Array.Empty<string>());

            Assert.True(_parser.HasError);
            Assert.Empty(result);
        }

        [Fact]
        public void Parse_WhitespaceOnly_SetsError()
        {
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
            // First call: invalid
            _parser.Parse(new[] { "abc" });
            Assert.True(_parser.HasError);

            // Second call: valid
            _parser.Parse(new[] { "1" });
            Assert.False(_parser.HasError);
            Assert.Empty(_parser.ErrorMessage);
        }
    } */
}