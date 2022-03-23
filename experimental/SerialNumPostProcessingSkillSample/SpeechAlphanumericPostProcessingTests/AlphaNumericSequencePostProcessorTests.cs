// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SpeechAlphanumericPostProcessing;

namespace SpeechAlphanumericPostProcessingTests
{
    [TestClass]
    public class AlphaNumericSequencePostProcessorTests
    {
        private static readonly string SubstitutionEnglishFilePath = Path.Combine(".", "substitution-en.json");
        private static readonly string SubstitutionSpanishFilePath = Path.Combine(".", "substitution-es.json");
        private static readonly string SubstitutionFrenchFilePath = Path.Combine(".", "substitution-fr.json");

        [TestInitialize]
        public void CreateSubstitutionFiles()
        {
            // English
            Substitution[] substitutionsForEn = {
                new Substitution("DENIED", "D9"),
                new Substitution("SEE", "C"),
                new Substitution("8", "A", true),
                new Substitution("KATIE", "KT"),
                new Substitution("BEE", "B"),
                new Substitution("BEFORE", "B4"),
                new Substitution("EMPTY", "MT"),
                new Substitution("CUTIE", "QT"),
            };
            Dictionary<string, Substitution[]> substitutionsEnJsonKVP = new Dictionary<string, Substitution[]>
            {
                { "substitutions", substitutionsForEn }
            };

            using (StreamWriter fs = File.CreateText(SubstitutionEnglishFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(fs, substitutionsEnJsonKVP);
            }

            // Spanish
            Substitution[] substitutionsForEs =
            {
                new Substitution("SE", "C"),
                new Substitution("EL", "L"),
                new Substitution("EN", "N"),
                new Substitution("SIGLOS", "S"),
                new Substitution("UN", "1"),
            };

            Dictionary<string, Substitution[]> substitutionsEsJsonKVP = new Dictionary<string, Substitution[]>
            {
                { "substitutions", substitutionsForEs }
            };

            using (StreamWriter fs = File.CreateText(SubstitutionSpanishFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(fs, substitutionsEsJsonKVP);
            }

            // French
            Substitution[] substitutionsForFr = { new Substitution("UNE", "N"), new Substitution("WATT", "W"),
                new Substitution("WATTS", "W"), new Substitution("SÉCU", "CQ") };
            Dictionary<string, Substitution[]> substitutionsFrJsonKVP = new Dictionary<string, Substitution[]>
            {
                { "substitutions", substitutionsForFr }
            };

            using (StreamWriter fs = File.CreateText(SubstitutionFrenchFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(fs, substitutionsFrJsonKVP);
            }
        }

        [TestCleanup]
        public void DeleteSubstitutionFile()
        {
            this.DeleteEnglishSubstitutionFile();
            this.DeleteSpanishSubstitutionFile();
            this.DeleteFrenchSubstitutionFile();
        }

        [TestMethod]
        public void MilitaryCodeTest()
        {
            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = false,
                AcceptsAlphabet = true,
                LengthInChars = 4,
            };
            groups.Add(g1);

            var input = "ABC, as in Charlie Z as in Zeta.";
            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly());
            var result = pattern.Inference(input);
            Assert.AreEqual("ABCZ", result[0]);

            // Test with all uppercase
            input = "A AS IN APPLE BCD";
            result = pattern.Inference(input);
            Assert.AreEqual("ABCD", result[0]);

            // Test with mix cases
            input = "A as in APPLE XYZ";
            result = pattern.Inference(input);
            Assert.AreEqual("AXYZ", result[0]);

            // Test with all lowercases
            input = "yz, as in zeta d as in dog t as in tom.";
            result = pattern.Inference(input);
            Assert.AreEqual("yzdt", result[0]);
        }

        [TestMethod]
        public void ReplacementAndInvalidTest()
        {
            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = false,
                AcceptsAlphabet = true,
                LengthInChars = 2,
            };
            groups.Add(g1);

            var g2 = new AlphaNumericTextGroup
            {
                AcceptsAlphabet = true,
                AcceptsDigits = true,
                LengthInChars = 5,
            };
            groups.Add(g2);

            // 8 replaced as A
            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly());
            var result = pattern.Inference("8E1LO98");
            Assert.IsTrue(result.Length == 2);

            // test invalid pattern
            result = pattern.Inference("3E1LO98");
            Assert.IsTrue(result.Length == 0);
        }

        [TestMethod]
        public void IntializeWithRegexTest()
        {
            var pattern = new AlphaNumericSequencePostProcessor("([a-zA-Z]{2})([0-9a-zA-Z][^125AEIOULNSZ]{5})");
            var result = pattern.Inference("8E1B098");

            // The last character could be 8 or A
            Assert.IsTrue(result.Length == 2);
            Assert.AreEqual(result[0], "AE1B09A");
            Assert.AreEqual(result[1], "AE1B098");

            // test invalid pattern
            result = pattern.Inference("3E1LO98");
            Assert.IsTrue(result.Length == 0);
        }

        [TestMethod]
        public void HPSerialNumberTest()
        {
            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly());
            var result = pattern.Inference("A as in Apple 123456789");
            Assert.IsTrue(result.Length == 2);
            Assert.AreEqual(result[0], "A1234567A9");
            Assert.AreEqual(result[1], "A123456789");

            result = pattern.Inference("ONE CR 00703 F 3.");
            Assert.IsTrue(result.Length == 1);
            Assert.AreEqual(result[0], "1CR00703F3");
        }

        [TestMethod]
        public void BatchingTest()
        {
            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            // Inference should return result even if length is less than pattern length
            // when batching is set to true
            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly(), true);
            var result = pattern.Inference("A as in");
            Assert.IsTrue(result.Length == 1);
            Assert.AreEqual(result[0], "A as in");

            result = pattern.Inference("A as in Apple ONE CR 00703 F");
            Assert.IsTrue(result.Length == 1);
            Assert.AreEqual(result[0], "A1CR00703F");
        }

        [TestMethod]
        public void Spanish_HPSerialNumberTest()
        {
            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly(), false, "es");
            var result = pattern.Inference("UN SE, R 1240 GSC.");
            Assert.AreEqual(result.Length, 1);
            Assert.AreEqual(result[0], "1CR1240GSC");

        }

        [TestMethod]
        public void French_HPSerialNumberTest()
        {
            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly(), false, "fr");
            var result = pattern.Inference("UN CR 14 1 L 8 C UN.");
            Assert.AreEqual(result.Length, 1);
            Assert.AreEqual(result[0], "1CR141L8C1");

            result = pattern.Inference("1CZ 00100 WATTS W.");
            Assert.AreEqual(result.Length, 1);
            Assert.AreEqual(result[0], "1CZ00100WW");
        }

        [TestMethod]
        public void KatieVariationsTest()
        {
            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly(), false, "en");
            var result = pattern.Inference("KATIE BEE BEFORE 4 EMPTY CUTIE");
            Assert.AreEqual(result.Length, 1);
            Assert.AreEqual(result[0], "KTBB44MTQT");
        }

        [TestMethod]
        public void HPSerialNumberTestWithoutCustomSubstitutionFile()
        {
            this.DeleteEnglishSubstitutionFile();

            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly());
            var result = pattern.Inference("A as in Apple 123456789");
            Assert.IsTrue(result.Length == 2);
            Assert.AreEqual(result[0], "A1234567A9");
            Assert.AreEqual(result[1], "A123456789");

            result = pattern.Inference("ONE CR 00703 F 3.");
            Assert.IsTrue(result.Length == 1);
            Assert.AreEqual(result[0], "1CR00703F3");
        }

        [TestMethod]
        public void EnglishCustomSubstitutionWithDigitWordReplacement()
        {
            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly());

            var result = pattern.Inference("ONE DENIED ONE 4 FIVE 6 SEE 1 SEE");
            Assert.IsTrue(result.Length == 1);
            Assert.AreEqual(result[0], "1D91456C1C");
        }

        [TestMethod]
        public void EnglishCustomSubstitutionWithMilitaryCode()
        {
            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly());

            var result = pattern.Inference("ONE DENIED A AS IN APPLE 4 FIVE 6 SEE 1 SEE");
            Assert.IsTrue(result.Length == 1);
            Assert.AreEqual(result[0], "1D9A456C1C");
        }

        [TestMethod]
        public void EnglishCustomSubstitutionWitAmbiguousInput()
        {
            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly());

            var result = pattern.Inference("ONE DENIED A 4 FIVE 8 SEE 1 SEE"); // 8 is ambiguous input
            Assert.IsTrue(result.Length == 2);
            Assert.AreEqual(result[0], "1D9A45AC1C");
            Assert.AreEqual(result[1], "1D9A458C1C");
        }

        [TestMethod]
        public void EnglishCustomSubstitutionWithAlphabetWordMapping()
        {
            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly());

            var result = pattern.Inference("ONE DENIED KATIE FIVE 8 SEE 1 SEE"); // 8 is ambiguous input
            Assert.IsTrue(result.Length == 2);
            Assert.AreEqual(result[0], "1D9KT5AC1C");
            Assert.AreEqual(result[1], "1D9KT58C1C");
        }

        [TestMethod]
        public void PostProcessedOutputShouldBeTruncatedToPatternLength()
        {
            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly());

            var result = pattern.Inference("ONE DENIED A 4 FIVE 6 SEE 1 DENIED"); // 9 at the end should be truncated
            Assert.IsTrue(result.Length == 1);
            Assert.AreEqual(result[0], "1D9A456C1D");
        }

        [TestMethod]
        public void WhenPatternIsAllDigitsOutputShouldBeEmptyWithSubstitution()
        {
            var groups = new List<AlphaNumericTextGroup>();
            var g1 = new AlphaNumericTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = false,
                LengthInChars = 4,
            };
            groups.Add(g1);

            var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly());

            var result = pattern.Inference("ONE BEFORE BEE");
            Assert.IsTrue(result.Length == 0);
        }

        [TestMethod]
        public void WhenSubstitutionMismatchesSpecifiedPatternOutputShouldBeEmpty()
        {
            var pattern = new AlphaNumericSequencePostProcessor("([a-zA-Z]{2})([0-9]{3})");

            var result = pattern.Inference("BEFORE ONE TWO THREE"); // BEFORE -> B4 is invalid since the first 2 chars shouldn't have letters per regex
            Assert.IsTrue(result.Length == 0);
        }

        [TestMethod]
        public void WhenSubstitutionMatchesSpecifiedPatternOutputShouldBeCorrect()
        {
            var pattern = new AlphaNumericSequencePostProcessor("([0-9]{1})([a-zA-Z]{3})([0-9]{3})");

            var result = pattern.Inference("FIVE A AS IN APPLE BEE BEFORE ONE TWO");
            Assert.IsTrue(result.Length == 1);
            Assert.AreEqual(result[0], "5ABB412");
        }

        private void DeleteEnglishSubstitutionFile()
        {
            if (File.Exists(SubstitutionEnglishFilePath))
            {
                File.Delete(SubstitutionEnglishFilePath);
            }
        }

        private void DeleteSpanishSubstitutionFile()
        {
            if (File.Exists(SubstitutionSpanishFilePath))
            {
                File.Delete(SubstitutionSpanishFilePath);
            }
        }

        private void DeleteFrenchSubstitutionFile()
        {
            if (File.Exists(SubstitutionFrenchFilePath))
            {
                File.Delete(SubstitutionFrenchFilePath);
            }
        }
    }
}
