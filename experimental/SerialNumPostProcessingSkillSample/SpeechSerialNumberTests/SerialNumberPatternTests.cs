// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpeechSerialNumber;

namespace SpeechSerialNumberTests
{
    [TestClass]
    public class SerialNumberPatternTests
    {
        [TestMethod]
        public void MilitaryCodeTest()
        {
            var groups = new List<TextGroup>();
            var g1 = new TextGroup
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
            var groups = new List<TextGroup>();
            var g1 = new TextGroup
            {
                AcceptsDigits = false,
                AcceptsAlphabet = true,
                LengthInChars = 2,
            };
            groups.Add(g1);

            var g2 = new TextGroup
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
            var result = pattern.Inference("8E1LO98");

            // The last character could be 8 or A
            Assert.IsTrue(result.Length == 2);

            // test invalid pattern
            result = pattern.Inference("3E1LO98");
            Assert.IsTrue(result.Length == 0);
        }
    }
}
