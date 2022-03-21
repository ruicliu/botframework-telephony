## AlphaNumericSequencePostProcessor

AlphaNumericSequencePostProcessor class is initialized with a list of text groups or a simplified regex expression which represents the text groups.
A text group represents a fixed length set of letters or numbers.  The more specific the definition of each text group, the more accurate the result of the post processing. 
In a group of alphabet only set, '8' could be auto corrected to 'A'.  
If one or more results are returned, there are ambiguous choices and user should be prompted to confirm the right sequence.

AlphaNumericSequencePostProcessor class supports inputting letters with military code, ie "A as in Alpha", "B as in Beta", etc.  User could use any word after the "as in" phrase.

Sample usage:

    var groups = new List<AlphaNumericTextGroup>();
    // The first char can only be letter
    var g1 = new AlphaNumericTextGroup
    {
    AcceptsDigits = false,
    AcceptsAlphabet = true,
    LengthInChars = 1,
    };
    groups.Add(g1);
        
    // The next three characters could be alphanumeric
    var g2 = new AlphaNumericTextGroup
    {
        AcceptsDigits = true,
        AcceptsAlphabet = true,
        LengthInChars = 3,
    };
    groups.Add(g1);
    
    // Test the pattern with valid input with military code
    var input = "ABC, as in Charlie Z as in Zeta.";
    var pattern = new AlphaNumericSequencePostProcessor(groups.AsReadOnly());
    var result = pattern.Inference(input);
    
    // result[0] should be "ABCZ"
    
    // Test the pattern with valid input common STT translation error
    input = "8 B D as in Dog T as in Tom.";
    result = pattern.Inference(input);
    
    // result[0] should be "ABDT"
    
     // Test the pattern with invalid input
    input = "4XYZ";
    result = pattern.Inference(input);
    
    // invalid input, result.Length is 0

## Custom Substitution
We now offer the ability for developers to define their own substitutions as a JSON file and AlphaNumericSequencePostProcessor will try to replace all matching input occurrences with the desired character(s).
This is right now only available when developers have a skill that focuses on post-processing, and is using AlphaNumericSequencePostProcessor and AlphaNumericAlphaNumericTextGroup.
To make this work, be sure to include all the substitutions for a particular language in a file name like this "substitution-<language code>.json" in the same directory as appsettings.json.
For example, for substitutions in English, please make a file with the name: substitution-en.json, and have the content as below:
{
  "substitutions": [
    {
      "substring": "DENIED",
      "replacement": "D9"
    },
    {
      "substring": "DENY",
      "replacement": "D9"
    },
    {
      "substring": "SEE",
      "replacement": "C"
    },
    {
      "substring": "SEA",
      "replacement": "C"
    },
    {
      "substring": "SEEN",
      "replacement": "CN"
    }
  ]
}

The values in "substring" and "replacement" for English substitutions should all be in capital letters where appropriate.

Currently, post processing for English language is supported.  We are working to add support for Spanish and French.



