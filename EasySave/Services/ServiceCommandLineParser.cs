using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace EasySave.Services
{
    public class ServiceCommandLineParser
    {
        private const int MinIndex = 1;
        private const int MaxIndex = 5;

        
        public IEnumerable<int> Parse(string[] args)
        {
            if (args == null || args.Length == 0)
                return Enumerable.Empty<int>();

            var input = args[0];
            var indices = new HashSet<int>(); //éviter les doublons

            // Range syntax: "1-3"
            if (input.Contains('-'))
            {
                var parts = input.Split('-');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int start_input) &&
                    int.TryParse(parts[1], out int end_input))
                {
                    for (int i = start_input; i <= end_input; i++)
                    {
                        if (IsValidIndex(i))
                            indices.Add(i - 1); // Convert to 0-based
                    }
                }
            }
            // List syntax: "1;3;5"
            else if (input.Contains(';'))
            {
                var input_numbers = input.Split(';');
                foreach (var number in input_numbers)
                {
                    if (int.TryParse(number.Trim(), out int index) && IsValidIndex(index))
                        indices.Add(index - 1);
                }
            }
            // Single index: "2"
            else if (int.TryParse(input, out int single))
            {
                if (IsValidIndex(single))
                    indices.Add(single - 1);
            }

            return indices.OrderBy(x => x);
        }

        private bool IsValidIndex(int index) => index >= MinIndex && index <= MaxIndex;
    }
}