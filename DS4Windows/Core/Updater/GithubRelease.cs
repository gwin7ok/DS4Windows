using System.Text.Json.Serialization;

namespace DS4Windows.Core.Updater
{
    // https://docs.github.com/en/rest/releases/releases?apiVersion=2022-11-28
    public class GithubRelease
    {
        [JsonPropertyName("tag_name")] public string TagName { get; set; } = null!; // JSON の tag_name から設定される
        [JsonPropertyName("prerelease")] public bool PreRelease { get; set; }
        [JsonPropertyName("body")] public string Body { get; set; } = null!; // JSON の body から設定される
    }
}
