using System.Text.Json;

namespace DeepSwarmCommon
{
    public static class JsonHelper
    {
        public static JsonDocumentOptions ParseOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        };

        public static JsonElement Parse(string jsonText) => JsonDocument.Parse(jsonText, ParseOptions).RootElement;
    }
}
