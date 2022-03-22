// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Newtonsoft.Json;

namespace SpeechAlphanumericPostProcessing
{
    [JsonObject]
    public class Substitution
    {
        [JsonProperty("substring", Required = Required.Always)]
        public string Substring { get; } = string.Empty;

        [JsonProperty("replacement", Required = Required.Always)]
        public string Replacement { get; } = string.Empty;

        [JsonProperty("isAmbiguous", Required = Required.Default)]
        public bool IsAmbiguous { get; } = false;

        public Substitution(string substring, string replacement, bool isAmbiguous = false)
        {
            this.Substring = substring;
            this.Replacement = replacement;
            this.IsAmbiguous = isAmbiguous;
        }
    }
}
