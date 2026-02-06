using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySave.Services
{
    // ===== COMMAND LINE PARSER =====
    public class ServiceCommandLineParser
    {
        // ===== CONSTANTS =====
        private const int MinIndex = 1;
        private const int MaxIndex = 5;

        // ===== ERROR TRACKING =====
        public bool HasError { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;

        // ===== PARSING =====
        public IEnumerable<int> Parse(string[] args)
        {
            // Reset error state
            HasError = false;
            ErrorMessage = string.Empty;

            // Check null or empty args
            if (args == null || args.Length == 0)
            {
                HasError = true;
                ErrorMessage = "No arguments provided.";
                return Enumerable.Empty<int>();
            }

            var input = args[0].Trim();

            // Check empty input
            if (string.IsNullOrWhiteSpace(input))
            {
                HasError = true;
                ErrorMessage = "Empty argument provided.";
                return Enumerable.Empty<int>();
            }

            var indices = new HashSet<int>();

            try
            {
                // Range syntax: "1-3"
                if (input.Contains('-'))
                {
                    indices = ParseRange(input);
                }
                // List syntax: "1;3;5"
                else if (input.Contains(';'))
                {
                    indices = ParseList(input);
                }
                // Single index: "2"
                else
                {
                    indices = ParseSingle(input);
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Parse error: {ex.Message}";
                return Enumerable.Empty<int>();
            }

            // Check if any valid indices were found
            if (indices.Count == 0 && !HasError)
            {
                HasError = true;

                ErrorMessage = $"No valid job indices found. Valid range: {MinIndex}-{MaxIndex}";
            }

            return indices.OrderBy(x => x);
        }

        private HashSet<int> ParseRange(string input)
        {
            var indices = new HashSet<int>();
            var parts = input.Split('-');

            if (parts.Length != 2)
            {
                HasError = true;
                ErrorMessage = $"Invalid range format: '{input}'. Expected format: 'start-end' (e.g., '1-3')";
                return indices;
            }

            if (!int.TryParse(parts[0].Trim(), out int start))
            {
                HasError = true;
                ErrorMessage = $"Invalid start value: '{parts[0]}'. Must be a number.";
                return indices;
            }

            if (!int.TryParse(parts[1].Trim(), out int end))
            {
                HasError = true;
                ErrorMessage = $"Invalid end value: '{parts[1]}'. Must be a number.";
                return indices;
            }

            if (start > end)
            {
                HasError = true;
                ErrorMessage = $"Invalid range: start ({start}) is greater than end ({end}).";
                return indices;
            }

            int validCount = 0;
            int invalidCount = 0;

            for (int i = start; i <= end; i++)
            {
                if (IsValidIndex(i))
                {
                    indices.Add(i - 1); // Convert to 0-based
                    validCount++;
                }
                else
                {
                    invalidCount++;
                }
            }

            if (invalidCount > 0 && validCount > 0)
            {
                // Warning: some indices were out of range but we still have valid ones
                ErrorMessage = $"Warning: Some indices were out of valid range ({MinIndex}-{MaxIndex}).";
            }

            return indices;
        }

        private HashSet<int> ParseList(string input)
        {
            var indices = new HashSet<int>();
            var parts = input.Split(';');
            var invalidValues = new List<string>();

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                if (!int.TryParse(trimmed, out int index))
                {
                    invalidValues.Add(trimmed);
                    continue;
                }

                if (IsValidIndex(index))
                {
                    indices.Add(index - 1); // Convert to 0-based
                }
                else
                {
                    invalidValues.Add(trimmed);
                }
            }

            if (invalidValues.Count > 0)
            {
                HasError = indices.Count == 0;
                ErrorMessage = $"Invalid or out-of-range values: {string.Join(", ", invalidValues)}. Valid range: {MinIndex}-{MaxIndex}";
            }

            return indices;
        }

        private HashSet<int> ParseSingle(string input)
        {
            var indices = new HashSet<int>();

            if (!int.TryParse(input, out int index))
            {
                HasError = true;
                ErrorMessage = $"Invalid value: '{input}'. Must be a number.";
                return indices;
            }

            if (!IsValidIndex(index))
            {
                HasError = true;
                ErrorMessage = $"Index {index} is out of valid range ({MinIndex}-{MaxIndex}).";
                return indices;
            }

            indices.Add(index - 1); // Convert to 0-based
            return indices;
        }

        // ===== VALIDATION =====
        private bool IsValidIndex(int index) => index >= MinIndex && index <= MaxIndex;
    }
}