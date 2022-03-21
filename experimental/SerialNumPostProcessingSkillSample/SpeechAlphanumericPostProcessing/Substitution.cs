// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Newtonsoft.Json;

namespace SpeechAlphanumericPostProcessing
{
    [JsonObject]
    public class Substitution
    {
        [JsonProperty("substring")]
        public string Substring { get; } = string.Empty;

        [JsonProperty("replacement")]
        public string Replacement { get; } = string.Empty;

        public Substitution(string substring, string replacement)
        {
            this.Substring = substring;
            this.Replacement = replacement;
        }
    }
}
