// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SpeechSerialNumber;

namespace SpeechSerialNumberTests
{
    [TestClass]
    public class SerialNumberPatternTests
    {
        private static readonly string SubstitutionEnglishFilePath = Path.Combine(".", "substitution-en.json");

        [TestInitialize]
        public void CreateEnglishSubstitutionFile()
        {
            Substitution[] substitutions = { new Substitution("DENIED", "D9"), new Substitution("SEE", "C") };
            Dictionary<string, Substitution[]> substitutionsJsonKVP = new Dictionary<string, Substitution[]>();
            substitutionsJsonKVP.Add("substitutions", substitutions);

            using (StreamWriter fs = File.CreateText(SubstitutionEnglishFilePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(fs, substitutionsJsonKVP);
            }
        }

        [TestCleanup]
        public void DeleteEnglishSubstitutionFile()
        {
            this.DeleteSubstitutionFileForEnglish();
        }

        [TestMethod]
        public void MilitaryCodeTest()
        {
            var groups = new List<SerialNumberTextGroup>();
            var g1 = new SerialNumberTextGroup
            {
                AcceptsDigits = false,
                AcceptsAlphabet = true,
                LengthInChars = 4,
            };
            groups.Add(g1);

            var input = "ABC, as in Charlie Z as in Zeta.";
            var pattern = new SerialNumberPattern(groups.AsReadOnly());
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
            var groups = new List<SerialNumberTextGroup>();
            var g1 = new SerialNumberTextGroup
            {
                AcceptsDigits = false,
                AcceptsAlphabet = true,
                LengthInChars = 2,
            };
            groups.Add(g1);

            var g2 = new SerialNumberTextGroup
            {
                AcceptsAlphabet = true,
                AcceptsDigits = true,
                LengthInChars = 5,
            };
            groups.Add(g2);

            // 8 replaced as A
            var pattern = new SerialNumberPattern(groups.AsReadOnly());
            var result = pattern.Inference("8E1LO98");
            Assert.IsTrue(result.Length == 2);

            // test invalid pattern
            result = pattern.Inference("3E1LO98");
            Assert.IsTrue(result.Length == 0);
        }

        [TestMethod]
        public void IntializeWithRegexTest()
        {
            var pattern = new SerialNumberPattern("([a-zA-Z]{2})([0-9a-zA-Z][^125AEIOULNSZ]{5})");
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
            var groups = new List<SerialNumberTextGroup>();
            var g1 = new SerialNumberTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new SerialNumberPattern(groups.AsReadOnly());
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
            var groups = new List<SerialNumberTextGroup>();
            var g1 = new SerialNumberTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            // Inference should return result even if length is less than pattern length
            // when batching is set to true
            var pattern = new SerialNumberPattern(groups.AsReadOnly(), true);
            var result = pattern.Inference("A as in");
            Assert.IsTrue(result.Length == 1);
            Assert.AreEqual(result[0], "A as in");

            result = pattern.Inference("A as in Apple ONE CR 00703 F");
            Assert.IsTrue(result.Length == 1);
            Assert.AreEqual(result[0], "A1CR00703F");
        }

        [TestMethod]
        public void HPSerialNumberTest_fr()
        {
            var groups = new List<SerialNumberTextGroup>();
            var g1 = new SerialNumberTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new SerialNumberPattern(groups.AsReadOnly(), false, "fr");
            var result = pattern.Inference("UN CR 14 1 L 8 C UN.");
            Assert.AreEqual(result.Length, 1);
            Assert.AreEqual(result[0], "1CR141L8C1");

            result = pattern.Inference("1CZ 00100 WATTS W.");
            Assert.AreEqual(result.Length, 1);
            Assert.AreEqual(result[0], "1CZ00100WW");
        }

        [TestMethod]
        public void HPSerialNumberTestWithoutCustomSubstitutionFile()
        {
            this.DeleteSubstitutionFileForEnglish();

            var groups = new List<SerialNumberTextGroup>();
            var g1 = new SerialNumberTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new SerialNumberPattern(groups.AsReadOnly());
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
            var groups = new List<SerialNumberTextGroup>();
            var g1 = new SerialNumberTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new SerialNumberPattern(groups.AsReadOnly());

            var result = pattern.Inference("ONE DENIED ONE 4 FIVE 6 SEE 1 SEE");
            Assert.IsTrue(result.Length == 1);
            Assert.AreEqual(result[0], "1D91456C1C");
        }

        [TestMethod]
        public void EnglishCustomSubstitutionWithMilitaryCode()
        {
            var groups = new List<SerialNumberTextGroup>();
            var g1 = new SerialNumberTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new SerialNumberPattern(groups.AsReadOnly());

            var result = pattern.Inference("ONE DENIED A AS IN APPLE 4 FIVE 6 SEE 1 SEE");
            Assert.IsTrue(result.Length == 1);
            Assert.AreEqual(result[0], "1D9A456C1C");
        }

        [TestMethod]
        public void EnglishCustomSubstitutionWitAmbiguousInput()
        {
            var groups = new List<SerialNumberTextGroup>();
            var g1 = new SerialNumberTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new SerialNumberPattern(groups.AsReadOnly());

            var result = pattern.Inference("ONE DENIED A 4 FIVE 8 SEE 1 SEE"); // 8 is ambiguous input
            Assert.IsTrue(result.Length == 2);
            Assert.AreEqual(result[0], "1D9A45AC1C");
            Assert.AreEqual(result[1], "1D9A458C1C");
        }

        [TestMethod]
        public void EnglishCustomSubstitutionWithAlphabetWordMapping()
        {
            var groups = new List<SerialNumberTextGroup>();
            var g1 = new SerialNumberTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new SerialNumberPattern(groups.AsReadOnly());

            var result = pattern.Inference("ONE DENIED KATIE FIVE 8 SEE 1 SEE"); // 8 is ambiguous input
            Assert.IsTrue(result.Length == 2);
            Assert.AreEqual(result[0], "1D9KT5AC1C");
            Assert.AreEqual(result[1], "1D9KT58C1C");
        }

        [TestMethod]
        public void PostProcessedOutputShouldBeTruncatedToPatternLength()
        {
            var groups = new List<SerialNumberTextGroup>();
            var g1 = new SerialNumberTextGroup
            {
                AcceptsDigits = true,
                AcceptsAlphabet = true,
                LengthInChars = 10,
            };
            groups.Add(g1);

            var pattern = new SerialNumberPattern(groups.AsReadOnly());

            var result = pattern.Inference("ONE DENIED A 4 FIVE 6 SEE 1 DENIED"); // 9 at the end should be truncated
            Assert.IsTrue(result.Length == 1);
            Assert.AreEqual(result[0], "1D9A456C1D");
        }

        private void DeleteSubstitutionFileForEnglish()
        {
            if (File.Exists(SubstitutionEnglishFilePath))
            {
                File.Delete(SubstitutionEnglishFilePath);
            }
        }
    }
}
