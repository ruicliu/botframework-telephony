// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SpeechAlphanumericPostProcessing
{
    public class AlphaNumericSequencePostProcessor
    {
        private static readonly char[] GroupEndDelimiter = new char[] { ')' };
        private static readonly Dictionary<string, string> SubstitutionFilePath = new Dictionary<string, string>
        {
            { "en", Path.Combine(".", "substitution-en.json") },
            { "es", Path.Combine(".", "substitution-es.json") },
            { "fr", Path.Combine(".", "substitution-fr.json") }
        };

        private static readonly char[] TrimChars = new char[] { '.', ',' };

        private Dictionary<string, Dictionary<string, Substitution>> substitutionMapping =
            new Dictionary<string, Dictionary<string, Substitution>>();

        public AlphaNumericSequencePostProcessor(IReadOnlyCollection<AlphaNumericTextGroup> textGroups, bool allowBatching = false, string language = "en")
        {
            AllowBatching = allowBatching;
            Groups = textGroups;

            foreach (AlphaNumericTextGroup group in Groups)
            {
                PatternLength += group.LengthInChars;
            }

            Language = language;

            TryParseCustomSubstitutionsFromFile(language);
        }

        public AlphaNumericSequencePostProcessor(string regex, bool allowBatching = false, string language = "en")
        {
            AllowBatching = allowBatching;
            List<AlphaNumericTextGroup> groups = new List<AlphaNumericTextGroup>();
            string[] regexGroups = regex.Split(GroupEndDelimiter, StringSplitOptions.RemoveEmptyEntries);

            foreach (string regexGroup in regexGroups)
            {
                AlphaNumericTextGroup group = new AlphaNumericTextGroup($"{regexGroup})");
                PatternLength += group.LengthInChars;
                groups.Add(group);
            }

            Groups = groups.AsReadOnly();
            Language = language;

            TryParseCustomSubstitutionsFromFile(language);
        }

        public int PatternLength { get; }

        /// <summary>
        /// Enum representing valid token type specified in pattern.
        /// </summary>
        private enum Token
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

            /// <summary>
            /// Custom substitution from user supplied file.
            /// </summary>
            Custom = 2,
        }

        private IReadOnlyCollection<AlphaNumericTextGroup> Groups { get; set; }

        private string InputString { get; set; } = string.Empty;

        private string Language { get; } = "en";

        private bool AllowBatching { get; }

        private Token PatternAt(int patternIndex, out HashSet<char> invalidChars)
        {
            int cumulativeIndex = 0;
            int prevGroupCumulative = 0;
            foreach (AlphaNumericTextGroup group in Groups)
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

        private (string First, string Second) AmbiguousOptions(string inputString, int inputIndex)
        {
            (string, string) result = ("*", "*");
            char input = inputString[inputIndex];
            if (this.substitutionMapping[Language].TryGetValue(input.ToString(), out var substitution))
            {
                if (substitution.IsAmbiguous)
                {
                    result = (substitution.Replacement, input.ToString());
                }
            }

            return result;
        }

        private FixupType DetectAlphabetFixup(string inputString, int inputIndex)
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

            return FixupType.None;
        }

        private string AlphabetFixup(int inputIndex, ref int offset)
        {
            char ch = InputString[inputIndex];
            string restOfInput = InputString.Substring(inputIndex);
            offset = 1;
            switch (DetectAlphabetFixup(InputString, inputIndex))
            {
                case FixupType.None:
                    return ch.ToString();
                case FixupType.AsIn:
                    AsInResult asInResult;
                    asInResult = FindAsInFixup(restOfInput);
                    if (asInResult.FixedUp)
                    {
                        offset = asInResult.NewOffset;
                        return asInResult.Char.ToString();
                    }

                    throw new Exception("Should have returned a char described by as in");
            }

            return ch.ToString();
        }

        private string TryCustomSubstitutionFixup(string inputString, int inputIndex, Token elementType, ref int newOffset)
        {
            // 8/A case where we actually want 8 in this position, so we don't substitute
            if (elementType == Token.Digit && char.IsDigit(inputString[inputIndex]))
            {
                newOffset = 1;
                return inputString[inputIndex].ToString();
            }

            // 8 -> A case
            if (this.substitutionMapping[Language].TryGetValue(inputString[inputIndex].ToString(), out var substitution))
            {
                newOffset = substitution.Replacement.Length;
                return substitution.Replacement;
            }

            // Assuming that we have already checked that this.substitutionMapping consists of the substring
            string restOfInput = inputString.Substring(inputIndex);
            string firstToken = restOfInput.Split(' ').FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(firstToken))
            {
                string token = firstToken.Trim(TrimChars);
                if (this.substitutionMapping[Language].ContainsKey(token))
                {
                    newOffset = firstToken.Length;
                    return this.substitutionMapping[Language][token].Replacement;
                }
            }

            return inputString[inputIndex].ToString();
        }

        private FixupType DetectCustomSubstitutionFixup(string inputString, int inputIndex)
        {
            if (this.substitutionMapping.ContainsKey(Language))
            {
                // 8 -> A
                if (this.substitutionMapping[Language].ContainsKey(inputString[inputIndex].ToString()))
                {
                    return FixupType.Custom;
                }

                // DENIED -> D9
                string restOfInput = inputString.Substring(inputIndex);
                string firstToken = restOfInput.Split(' ').FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(firstToken))
                {
                    string token = firstToken.Trim(TrimChars);
                    return this.substitutionMapping[Language].ContainsKey(token) ? FixupType.Custom : FixupType.None;
                }
            }

            return FixupType.None;
        }

        // Pattern : 2 alphabetic, 1 numeric
        // Input:    katie 4
        public string[] Inference(string inputString)
        {
            InputString = inputString;

            List<string> results = new List<string>();

            // Trivial Length check - must be at least pattern length (most likely longer).
            if (inputString.Length < PatternLength && !AllowBatching)
            {
                return results.ToArray();
            }

            bool appendSpacer = inputString.Length < PatternLength && AllowBatching;
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
                    if (appendSpacer)
                    {
                        // append to fixedUpString
                        fixedUpString += inputElement;
                    }

                    continue;
                }

                var inferResult = InferMatch(inputIndex, elementType, invalidChars, ref patternIndex);
                patternIndex++;  // Bump to next element in pattern.

                if (inferResult.IsAmbiguous)
                {
                    // Store ambiguity index from original string.
                    ambiguousInputIndexes.Add(inputIndex);
                    fixedUpString += '*'; // Mark ambiguity
                }
                else if (inferResult.IsFixedUp)
                {
                    fixedUpString += inferResult.Value;
                    inputIndex += inferResult.NewOffset - 1;
                }
                else
                {
                    fixedUpString += inputElement;
                }

                if (inferResult.IsNoMatch)
                {
                    isMatch = false;
                    break;
                }

                inputIndex++;
            }

            if (fixedUpString.Length > PatternLength)
            {
                // truncates any characters that exceeds PatternLength, truncation starts at the last index inclusively
                fixedUpString = fixedUpString.Remove(PatternLength);
            }

            if (!isMatch)
            {
                return AllowBatching ? new string[1] { inputString } : results.ToArray();
            }

            // Handle ambiguous issues.
            if (ambiguousInputIndexes.Count > 0)
            {
#pragma warning disable IDE0042 // Deconstruct variable declaration
                var options = AmbiguousOptions(inputString, ambiguousInputIndexes.FirstOrDefault());
#pragma warning restore IDE0042 // Deconstruct variable declaration
                results.Add(fixedUpString.Replace("*", options.First));
                results.Add(fixedUpString.Replace("*", options.Second));
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

            if (this.substitutionMapping.ContainsKey(Language) &&
                this.substitutionMapping[Language].TryGetValue(input.ToString(), out var substitution))
            {
                match.IsAmbiguous = substitution.IsAmbiguous;
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

        private InferResult InferMatch(
            int inputIndex, Token elementType, HashSet<char> invalidChars, ref int patternIndex)
        {
            InferResult result = new InferResult();
            char currentInputChar = InputString[inputIndex];
            result.Value = currentInputChar.ToString();

            // try custom substitution first
            if (DetectCustomSubstitutionFixup(InputString, inputIndex) == FixupType.Custom)
            {
                if (elementType == Token.Both)
                {
                    AmbiguousResult ambiguousResult = CheckAmbiguous(ref inputIndex);
                    result.Value = ambiguousResult.Ch.ToString();
                    if (ambiguousResult.IsAmbiguous)
                    {
                        result.IsAmbiguous = true;
                        return result;
                    }
                }

                int offset = 0;
                string substitutionResult = TryCustomSubstitutionFixup(InputString, inputIndex, elementType, ref offset);

                int i = 0;
                while (true)
                {
                    char ch = substitutionResult[i++];
                    if (invalidChars.Contains(ch) ||
                        (char.IsDigit(ch) && elementType == Token.Alpha) ||
                        (char.IsLetter(ch) && elementType == Token.Digit))
                    {
                        result.IsNoMatch = true;
                        return result;
                    }

                    if (i < substitutionResult.Length & (patternIndex + 1) < PatternLength)
                    {
                        patternIndex++;
                        elementType = PatternAt(patternIndex, out invalidChars);
                    }
                    else
                    {
                        break;
                    }
                }

                result.Value = substitutionResult;
                result.NewOffset = offset;
                result.IsFixedUp = true;
                return result;
            }

            // Token.Digit should be handled by substitution mapping already
            TryFixupAlpha(inputIndex, currentInputChar, elementType, result, invalidChars);
            return result;
        }

        private void TryFixupAlpha(int inputIndex, char currentInputChar, Token elementType, InferResult result, HashSet<char> invalidChars)
        {
            var alphabetFixup = DetectAlphabetFixup(InputString, inputIndex);
            if (char.IsLetter(currentInputChar) == false && alphabetFixup == FixupType.None)
            {
                if (elementType == Token.Alpha)
                {
                    result.IsNoMatch = true;
                }
            }
            else if (alphabetFixup != FixupType.None)
            {
                int newOffset = 1;
                result.IsFixedUp = true;
                string fixedUpResult = AlphabetFixup(inputIndex, ref newOffset);
                if (fixedUpResult.Length == 1 && invalidChars.Contains(fixedUpResult[0]))
                {
                    result.IsFixedUp = false;
                    result.IsNoMatch = true;
                    return;
                }

                result.Value = fixedUpResult;
                result.NewOffset = newOffset;
            }
        }

        private void TryParseCustomSubstitutionsFromFile(string language)
        {
            if (SubstitutionFilePath.ContainsKey(language))
            {
                if (!this.substitutionMapping.ContainsKey(language))
                {
                    this.substitutionMapping.TryAdd(language, new Dictionary<string, Substitution>());
                }

                string path = SubstitutionFilePath[language];
                if (path == null || !File.Exists(path))
                {
                    return;
                }

                using (StreamReader sr = new StreamReader(path))
                {
                    string json = sr.ReadToEnd();
                    Dictionary<string, Substitution[]> substitutionKVP = JsonConvert.DeserializeObject<Dictionary<string, Substitution[]>>(json);
                    if (substitutionKVP.TryGetValue("substitutions", out var substitutions))
                    {
                        if (substitutions.Length == this.substitutionMapping[language].Count)
                        {
                            return;
                        }

                        foreach (var substitution in substitutions)
                        {
                            this.substitutionMapping[language].TryAdd(substitution.Substring, substitution);
                        }
                    }
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

            public string Value { get; set; }

            public bool IsFixedUp { get; set; }

            public bool IsAmbiguous { get; set; }

            public bool IsNoMatch { get; set; }

            public int NewOffset { get; set; }
        }
    }
}
