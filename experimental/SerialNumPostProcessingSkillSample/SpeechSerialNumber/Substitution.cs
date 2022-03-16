// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Newtonsoft.Json;

namespace SpeechSerialNumber
{
    [JsonObject]
    public class Substitution
    {
        [JsonProperty("substring")]
        public string Substring { get; } = string.Empty;

        [JsonProperty("replace_with")]
        public string ReplaceWith { get; } = string.Empty;

        public Substitution(string substring, string replaceWith)
        {
            this.Substring = substring;
            this.ReplaceWith = replaceWith;
        }
    }
}
