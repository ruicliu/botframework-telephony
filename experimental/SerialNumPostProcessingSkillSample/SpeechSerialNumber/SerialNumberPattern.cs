// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SpeechSerialNumber
{
    public class SerialNumberPattern
    {
        private static readonly char[] GroupEndDelimiter = new char[] { ')' };
        private static readonly Dictionary<string, Dictionary<char, char>> AlphabetReplacementsTable =
            new Dictionary<string, Dictionary<char, char>>
            {
                { "en", new Dictionary<char, char> { { '8', 'A' } } },
                { "es", new Dictionary<char, char>() }
            };

        private static readonly Dictionary<string, Dictionary<char, char>> DigitReplacementsTable =
            new Dictionary<string, Dictionary<char, char>>
            {
                { "en", new Dictionary<char, char>() },
                { "es", new Dictionary<char, char>() }
            };

        private static readonly Dictionary<string, Dictionary<string, char>> DigitWordReplacementsTable =
            new Dictionary<string, Dictionary<string, char>>
            {
                {
                    "en", new Dictionary<string, char>
                    {
                        { "ZER0", '0' },
                        { "ONE", '1' },
                        { "TWO", '2' },
                        { "THREE", '3' },
                        { "FOR", '4' },
                        { "FOUR", '4' },
                        { "FIVE", '5' },
                        { "SIX", '6' },
                        { "SEVEN", '7' },
                        { "EIGHT", '8' },
                        { "NINE", '9' }
                    }
                },
                {
                    "es", new Dictionary<string, char>
                    {
                        { "CERO", '0' },
                        { "ZERO", '0' },
                        { "UN", '1' },
                        { "UNA", '1' },
                        { "UNO", '1' },
                        { "DOS", '2' },
                        { "CUATRO", '4' },
                        { "SIN CO", '5' },
                        { "SE", 'C' },
                        { "EL", 'L' },
                        { "EN", 'N' },
                        { "VOLTIOS", 'V' },
                        { "VATIO", 'W' },
                        { "VATIOS", 'W' },
                        { "SIGLO", 'S' },
                        { "SIGLOS", 'S' }
                    }
                }
            };

        public SerialNumberPattern(IReadOnlyCollection<SerialNumberTextGroup> textGroups, bool allowBatching = false, string language = "en")
        {
            AllowBatching = allowBatching;
            Groups = textGroups;

            foreach (SerialNumberTextGroup group in Groups)
            {
                PatternLength += group.LengthInChars;
            }

            Language = language;
        }

        public SerialNumberPattern(string regex, bool allowBatching = false, string language = "en")
        {
            AllowBatching = allowBatching;
            List<SerialNumberTextGroup> groups = new List<SerialNumberTextGroup>();
            string[] regexGroups = regex.Split(GroupEndDelimiter, StringSplitOptions.RemoveEmptyEntries);

            foreach (string regexGroup in regexGroups)
            {
                SerialNumberTextGroup group = new SerialNumberTextGroup($"{regexGroup})");
                PatternLength += group.LengthInChars;
                groups.Add(group);
            }

            Groups = groups.AsReadOnly();
            Language = language;
        }

        /// <summary>
        /// Enum representing valid token type specified in pattern.
        /// </summary>
        public enum Token
        {
            /// <summary>
            /// Invalid token.
            /// </summary>
            Invalid,

            /// <summary>
            /// Alphabetic token.
            /// </summary>
            Alpha,

            /// <summary>
            /// Numeric token.
            /// </summary>
            Digit,

            /// <summary>
            /// Alphanumeric token.
            /// </summary>
            Both,

            /// <summary>
            /// The char - token.
            /// </summary>
            Dash
        }

        /// <summary>
        /// Enum representing character replacement.
        /// </summary>
        public enum FixupType
        {
            /// <summary>
            /// No replacement.
            /// </summary>
            None = 0,

            /// <summary>
            /// Character replacement.
            /// </summary>
            AlphaMapping = 1,

            /// <summary>
            /// As in replacement.
            /// </summary>
            AsIn = 2,
        }

        public string Regexp
        {
            get
            {
                string result = string.Empty;
                foreach (SerialNumberTextGroup group in Groups)
                {
                    result += group.RegexString;
                }

                return result;
            }
        }

        public string Language { get; set; } = "en";

        public IReadOnlyCollection<SerialNumberTextGroup> Groups { get; set; }

        public int PatternLength { get; set; }

        public string InputString { get; private set; } = string.Empty;

        public bool AllowBatching { get; set; }

        public Token PatternAt(int patternIndex, out HashSet<char> invalidChars)
        {
            int cumulativeIndex = 0;
            int prevGroupCumulative = 0;
            foreach (SerialNumberTextGroup group in Groups)
            {
                cumulativeIndex += group.LengthInChars;
                if (cumulativeIndex > patternIndex)
                {
                    invalidChars = group.InvalidChars;
                    if (group.AcceptsDigits && group.AcceptsAlphabet)
                    {
                        return Token.Both;
                    }

                    if (group.AcceptsDigits)
                    {
                        return Token.Digit;
                    }

                    if (group.AcceptsAlphabet)
                    {
                        return Token.Alpha;
                    }
                }

                prevGroupCumulative += group.LengthInChars;
            }

            invalidChars = new HashSet<char>();
            return Token.Invalid;
        }

        public (char First, char Second) AmbiguousOptions(string inputString, int inputIndex)
        {
            (char, char) result = ('*', '*');
            char input = inputString[inputIndex];

            switch (input)
            {
                case '8':
                    result = ('A', input);
                    break;
                default:
                    Debug.Assert(false, "No ambiguous input is found");
                    break;
            }

            return result;
        }

        public bool DetectDigitFixup(string inputString, int inputIndex)
        {
            char ch = inputString[inputIndex];

            // Handle (One) = 1
            string restOfInput = inputString.Substring(inputIndex);
            string firstToken = restOfInput.Split(' ').FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstToken))
            {
                string token = firstToken;
                if (DigitWordReplacementsTable[Language].ContainsKey(token))
                {
                    return true;
                }
            }

            // Handle (A) = 8
            return DigitReplacementsTable[Language].ContainsKey(ch);
        }

        public char DigitFixup(int inputIndex, ref int newOffset)
        {
            char ch = InputString[inputIndex];
            char replacement = char.MinValue;

            // Handle (One) = 1
            string restOfInput = InputString.Substring(inputIndex);
            string firstToken = restOfInput.Split(' ').FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstToken))
            {
                string token = firstToken;
                if (DigitWordReplacementsTable[Language].ContainsKey(token))
                {
                    replacement = DigitWordReplacementsTable[Language][token];
                    newOffset = inputIndex + token.Length;
                }
            }

            // Handle (A) = 8
            if (DigitReplacementsTable[Language].ContainsKey(ch))
            {
                replacement = DigitReplacementsTable[Language][ch];
            }

            Debug.WriteLine($"Digit {ch} was replaced by character {replacement}");
            return replacement;
        }

        public FixupType DetectAlphabetFixup(string inputString, int inputIndex)
        {
            char ch = inputString[inputIndex];
            string restOfInput = inputString.Substring(inputIndex);

            // (A as in Apple)BC
            // ABC, as in Charlie Z as in Zeta.
            // [A-Z]{3}
            var asInResult = FindAsInFixup(restOfInput);
            if (asInResult.FixedUp)
            {
                return FixupType.AsIn;
            }

            // Find direct letter mapping
            return AlphabetReplacementsTable[Language].ContainsKey(ch) ? FixupType.AlphaMapping : FixupType.None;
        }

        public char AlphabetFixup(int inputIndex, ref int offset)
        {
            char ch = InputString[inputIndex];
            string restOfInput = InputString.Substring(inputIndex);
            offset = 1;
            switch (DetectAlphabetFixup(InputString, inputIndex))
            {
                case FixupType.None:
                    return ch;
                case FixupType.AlphaMapping:
                    char replacement = AlphabetReplacementsTable[Language][ch];
                    Console.WriteLine($"Alphabetic Character {ch} was replaced by digit {replacement}");
                    return replacement;
                case FixupType.AsIn:
                    AsInResult asInResult;
                    asInResult = FindAsInFixup(restOfInput);
                    if (asInResult.FixedUp)
                    {
                        Debug.WriteLine($"'as in' fixed up {restOfInput} to letter {asInResult.Char}");
                        offset = asInResult.NewOffset;
                        return asInResult.Char;
                    }

                    throw new Exception("Should have returned a char described by as in");
            }

            // TODO: Do additional fixups here
            return ch;
        }

        // Pattern : 2 alphabetic, 1 numeric
        // Input:    katie 4
        public string[] Inference(string inputString)
        {
            InputString = inputString;

            List<string> results = new List<string>();
            Console.WriteLine($"Original text      : '{inputString}'");
            Console.WriteLine($"Regular Expression : '{Regexp}'");

            // Trivial Length check - must be at least pattern length (most likely longer).
            if (inputString.Length < PatternLength && !AllowBatching)
            {
                Console.WriteLine($"Input string is too short!  Must be at least {PatternLength} characters/digits.");
                return results.ToArray();
            }

            int patternIndex = 0;
            int inputIndex = 0;

            List<int> ambiguousInputIndexes = new List<int>();
            bool isMatch = true;
            string fixedUpString = string.Empty;

            // Initial Scan to see how many things to correct
            while (patternIndex < PatternLength && inputIndex < inputString.Length)
            {
                HashSet<char> invalidChars;
                var elementType = PatternAt(patternIndex, out invalidChars);    // What type the pattern is expecting
                var inputElement = inputString[inputIndex];

                // Skip white space, period, comma, dash if not expected
                if (inputElement == ' ' || inputElement == '.' || inputElement == ',' ||
                    (inputElement == '-' && elementType != Token.Dash))
                {
                    inputIndex++;
                    continue;
                }

                Debug.WriteLine($"Token at index {patternIndex} is {elementType}");
                Debug.WriteLine($"Input at index {inputIndex} is {inputElement}");

                patternIndex++;  // Bump to next element in pattern.

                var inferResult = InferMatch(inputIndex, elementType, invalidChars);

                if (inferResult.IsAmbiguous)
                {
                    // Store ambiguity index from original string.
                    ambiguousInputIndexes.Add(inputIndex);
                    fixedUpString += '*'; // Mark ambiguity
                }
                else if (inferResult.IsFixedUp)
                {
                    fixedUpString += inferResult.Ch;
                    inputIndex += inferResult.NewOffset - 1;
                }
                else
                {
                    fixedUpString += inputElement;
                }

                if (inferResult.IsNoMatch)
                {
                    Console.WriteLine("ERROR: No match");
                    isMatch = false;
                    break;
                }

                inputIndex++;
            }

            if (!isMatch)
            {
                return results.ToArray();
            }

            // Handle ambiguous issues.
            if (ambiguousInputIndexes.Count > 0)
            {
#pragma warning disable IDE0042 // Deconstruct variable declaration
                var options = AmbiguousOptions(inputString, ambiguousInputIndexes.FirstOrDefault());
#pragma warning restore IDE0042 // Deconstruct variable declaration
                results.Add(fixedUpString.Replace('*', options.First));
                results.Add(fixedUpString.Replace('*', options.Second));
            }
            else
            {
                results.Add(fixedUpString);
            }

            return results.ToArray();
        }

        private AmbiguousResult CheckAmbiguous(ref int inputIndex)
        {
            AmbiguousResult match = new AmbiguousResult();
            char input = InputString[inputIndex];
            match.Ch = input;

            switch (input)
            {
                case '8':
                    match.IsAmbiguous = true;
                    return match;
                default:
                    break;
            }

            return match;
        }

        private AsInResult FindAsInFixup(string input)
        {
            AsInResult result = new AsInResult();
            char[] delimiters = { ' ', ',', '.', '/', '-' };
            var tokens = input.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length > 3 && tokens[0].Length == 1)
            {
                char proposedChar = '*';
                if (tokens[1].ToLowerInvariant() == "as" && tokens[2].ToLowerInvariant() == "in")
                {
                    proposedChar = tokens[3][0];
                    result.FixedUp = true;
                    result.Char = proposedChar;
                    int initialOffset = input.IndexOf(" in ", StringComparison.OrdinalIgnoreCase);
                    result.NewOffset = initialOffset + 4 + tokens[3].Length;
                }
            }

            return result;
        }

        private InferResult InferMatch(int inputIndex, Token elementType, HashSet<char> invalidChars)
        {
            InferResult result = new InferResult();
            char currentInputChar = InputString[inputIndex];
            result.Ch = currentInputChar;

            switch (elementType)
            {
                case Token.Digit:
                    TryFixupDigit(inputIndex, currentInputChar, elementType, result, invalidChars);
                    break;
                case Token.Alpha:
                    TryFixupAlpha(inputIndex, currentInputChar, elementType, result, invalidChars);
                    break;
                case Token.Both:
                    AmbiguousResult ambiguousResult = CheckAmbiguous(ref inputIndex);
                    result.Ch = ambiguousResult.Ch;
                    if (ambiguousResult.IsAmbiguous)
                    {
                        result.IsAmbiguous = true;
                    }
                    else
                    {
                        TryFixupDigit(inputIndex, currentInputChar, elementType, result, invalidChars);
                        if (!result.IsFixedUp)
                        {
                            TryFixupAlpha(inputIndex, currentInputChar, elementType, result, invalidChars);
                        }
                    }

                    break;
                default:
                    break;
            }

            return result;
        }

        private void TryFixupAlpha(int inputIndex, char currentInputChar, Token elementType, InferResult result, HashSet<char> invalidChars)
        {
            if (char.IsLetter(currentInputChar) == false && DetectAlphabetFixup(InputString, inputIndex) == FixupType.None)
            {
                if (elementType == Token.Alpha)
                {
                    result.IsNoMatch = true;
                }
            }
            else
            {
                int newOffset = 1;
                result.IsFixedUp = true;
                char ch = AlphabetFixup(inputIndex, ref newOffset);
                if (invalidChars.Contains(ch))
                {
                    result.IsFixedUp = false;
                    result.IsNoMatch = true;
                    return;
                }

                result.Ch = ch;
                result.NewOffset = newOffset;
            }
        }

        private void TryFixupDigit(int inputIndex, char currentInputChar, Token elementType, InferResult result, HashSet<char> invalidChars)
        {
            int newOffset = 1;
            result.IsFixedUp = DetectDigitFixup(InputString, inputIndex);

            if (char.IsDigit(currentInputChar) == false && !result.IsFixedUp)
            {
                if (elementType == Token.Digit)
                {
                    result.IsNoMatch = true;
                }
            }
            else if (result.IsFixedUp)
            {
                char ch = DigitFixup(inputIndex, ref newOffset);
                if (invalidChars.Contains(ch))
                {
                    result.IsNoMatch = true;
                }
                else
                {
                    result.Ch = ch;
                    result.NewOffset = newOffset;
                }
            }
        }

        private class AmbiguousResult
        {
            public char? Ch { get; set; }

            public bool IsAmbiguous { get; set; }
        }

        private class AsInResult
        {
            public AsInResult()
            {
                NewOffset = 1;
                Char = '*';
            }

            public bool FixedUp { get; set; }

            public int NewOffset { get; set; }

            public char Char { get; set; }
        }

        private class InferResult
        {
            public InferResult()
            {
                NewOffset = 1;
            }

            public char? Ch { get; set; }

            public bool IsFixedUp { get; set; }

            public bool IsAmbiguous { get; set; }

            public bool IsNoMatch { get; set; }

            public int NewOffset { get; set; }
        }
    }
}
