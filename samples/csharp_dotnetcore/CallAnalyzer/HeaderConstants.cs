// <copyright file="HeaderConstants.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

namespace TIClient.Constants
{
    /// <summary>
    /// The commonly used header constants.
    /// </summary>
    public sealed class HeaderConstants
    {
        /// <summary>
        /// The user agent header name.
        /// </summary>
        public const string UserAgent = "User-Agent";

        /// <summary>
        /// The content type header name.
        /// </summary>
        public const string ContentType = "Content-Type";

        /// <summary>
        /// The content type json.
        /// </summary>
        public const string ContentTypeJson = "application/json";

        /// <summary>
        /// The content type bond.
        /// </summary>
        public const string ContentTypeBond = "application/bond";

        /// <summary>
        /// The content type plain text.
        /// </summary>
        public const string ContentTypePlainText = "text/plain";

        /// <summary>
        /// The content type plain text.
        /// </summary>
        public const string ContentTypeXml = "text/xml";

        /// <summary>
        /// The content type application SSML XML.
        /// </summary>
        public const string ContentTypeApplicationSsmlXml = "application/ssml+xml";

        /// <summary>
        /// The timestamp header.
        /// </summary>
        public const string Timestamp = "X-Timestamp";

        /// <summary>
        /// The impression unique identifier header name.
        /// </summary>
        public const string FDImpressionGuid = "X-FD-ImpressionGUID";

        /// <summary>
        /// The fd client identifier header name.
        /// </summary>
        public const string FDClientId = "X-FD-ClientID";

        /// <summary>
        /// The fdui language header name.
        /// </summary>
        public const string FDUILang = "X-FD-UILang";

        /// <summary>
        /// The search ig header name.
        /// </summary>
        public const string SearchIG = "X-Search-IG";

        /// <summary>
        /// The Search App ID header name.
        /// </summary>
        public const string SearchAppID = "X-Search-AppID";

        /// <summary>
        /// The Search Client ID header name.
        /// </summary>
        public const string SearchClientID = "X-Search-ClientID";

        /// <summary>
        /// The CU Log level.
        /// </summary>
        public const string LogLevel = "X-CU-LogLevel";

        /// <summary>
        /// The Request Lattice header name.
        /// </summary>
        public const string RequestLattice = "X-CU-RequestLattice";

        /// <summary>
        /// The search marker header name.
        /// </summary>
        public const string SearchMarker = "X-Search-Market";

        /// <summary>
        /// The cu client version header name.
        /// </summary>
        public const string CuClientVersion = "X-CU-ClientVersion";

        /// <summary>
        /// The cu application identifier header name.
        /// </summary>
        public const string CuApplicationId = "X-CU-ApplicationId";

        /// <summary>
        /// The cu request identifier header name.
        /// </summary>
        public const string CuRequestId = "X-CU-RequestId";

        /// <summary>
        /// The cu request identifier header name.
        /// </summary>
        public const string CuLobbyMessageType = "X-LOBBY-MESSAGE-TYPE";

        /// <summary>
        /// The cu request identifier header name.
        /// </summary>
        public const string CuConversationId = "X-CU-ConversationId";

        /// <summary>
        /// The cu result type header name.
        /// </summary>
        public const string CuResultType = "X-CU-ResultType";

        /// <summary>
        /// The cu result type header name.
        /// </summary>
        public const string CuServiceVersion = "X-CU-ServiceVersion";

        /// <summary>
        /// The locale header name.
        /// </summary>
        public const string Locale = "X-CU-Locale";

        /// <summary>
        /// The request identifier header name.
        /// </summary>
        public const string RequestId = "X-RequestId";

        /// <summary>
        /// The path header name.
        /// </summary>
        public const string Path = "Path";

        /// <summary>
        /// The stream identifier header name.
        /// </summary>
        public const string StreamId = "X-StreamId";

        /// <summary>
        /// The status code header name.
        /// </summary>
        public const string StatusCode = "Status-Code";

        /// <summary>
        /// The turn identifier header name.
        /// </summary>
        public const string TurnId = "X-CU-TurnId";

        /// <summary>
        /// The RPS authorization header.
        /// </summary>
        public const string Authorization = "Authorization";

        /// <summary>
        /// The default subscription key header.
        /// </summary>
        public const string SubscriptionKey = "Ocp-Apim-Subscription-Key";

        /// <summary>
        /// The region override key header.
        /// </summary>
        public const string RegionOverrideKey = "Ocp-Apim-Subscription-Region-Override";

        /// <summary>
        /// The Skype Translation Authentication Header.
        /// </summary>
        public const string SkypeTranslationAuthentication = "Sec-WebSocket-Key";

        /// <summary>
        /// The content type application json with encoding.
        /// </summary>
        public const string ContentTypeApplicationJsonWithEncoding = "application/json; charset=utf-8";

        /// <summary>
        /// The content type plain text with encoding.
        /// </summary>
        public const string ContentTypePlainTextWithEncoding = "text/plain; charset=utf-8";

        /// <summary>
        /// The content type audio.
        /// </summary>
        public const string ContentTypeAudio = "audio/wav";

        /// <summary>
        /// The RPS token header name.
        /// </summary>
        public const string RPSAuthToken = "X-Search-RPSToken";

        /// <summary>
        /// The delegation RPS authentication token header name.
        /// </summary>
        public const string DelegationRPSAuthToken = "X-Search-DelegationRPSToken";

        /// <summary>
        /// The Ais token header name.
        /// </summary>
        public const string AISAuthToken = "X-AIS-AuthToken";

        /// <summary>
        /// The set cookie header name.
        /// </summary>
        public const string SetCookie = "Set-Cookie";

        /// <summary>
        /// The status reason.
        /// </summary>
        public const string StatusReason = "Status-Reason";

        /// <summary>
        /// The status reason.
        /// </summary>
        public const string MicrosoftAudioFormat = "X-Microsoft-OutputFormat";

        /// <summary>
        /// The content type header name.
        /// </summary>
        public const string ContentLength = "Content-Length";

        /// <summary>
        /// The content type application bond.
        /// </summary>
        public const string ContentTypeApplicationBond = "application/X-CUResponse.Bond";

        /// <summary>
        /// The speech session token header name.
        /// </summary>
        public const string SpeechSessionToken = "X-SpeechSession-Token";

        /// <summary>
        /// The guest service URL.
        /// </summary>
        public const string GuestServiceUrl = "X-GuestService-URL";

        /// <summary>
        /// The callback URL.
        /// </summary>
        public const string CallbackUrl = "X-Callback-URL";

        /// <summary>
        /// The callback token.
        /// </summary>
        public const string CallbackToken = "X-Callback-Token";

        /// <summary>
        /// The guest service protocol version.
        /// </summary>
        public const string GuestServiceProtocolVersion = "X-GuestService-ProtocolVersion";

        /// <summary>
        /// The speaker identifier header name.
        /// </summary>
        public const string SpeakerIdentifier = "X-Speaker-Id";

        /// <summary>
        /// The machine name.
        /// </summary>
        public const string MachineName = "X-CU-MachineName";

        /// <summary>
        /// The connection identifier header name.
        /// </summary>
        public const string ConnectionId = "X-ConnectionId";

        /// <summary>
        /// The front door quality of response header.
        /// <remarks>https://azfddocs.azurewebsites.net/domore/routing/probes/</remarks>
        /// </summary>
        public const string FdQualityOfResponse = "X-AS-QualityOfResponse";

        /// <summary>
        /// Header name for CommandsAppId
        /// </summary>
        public const string CommandsAppId = "X-CommandsAppId";

        /// <summary>
        /// Pronunciation header key.
        /// </summary>
        public const string Pronunciation = "Pronunciation";

        /// <summary>
        /// The subscription-id header. It is used by command runtime.
        /// </summary>
        public const string SubscriptionId = "subscription-id";

        /// <summary>
        /// The correlation-id header.
        /// </summary>
        public const string CorrelationId = "X-Correlation-ID";

        /// <summary>
        /// The output audio codec header name
        /// </summary>
        public const string OutputAudioCodec = "X-Output-AudioCodec";

        /// <summary>
        /// Features header, used for assignment of flights
        /// </summary>
        public const string Features = "X-Features";
    }
}

