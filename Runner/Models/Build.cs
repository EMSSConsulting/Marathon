using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marathon.Models
{
    [JsonObject(MemberSerialization.OptIn)]
    public class BuildInfo
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("project_id")]
        public int ProjectID { get; set; }

        [JsonProperty("project_name")]
        public string ProjectName { get; set; }

        [JsonProperty("commands")]
        public string Commands { get; set; }

        [JsonProperty("repo_url")]
        public string RepoURL { get; set; }

        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("sha")]
        public string SHA { get; set; }

        [JsonProperty("before_sha")]
        public string BeforeSHA { get; set; }

        [JsonProperty("allow_git_fetch")]
        public bool AllowGitFetch { get; set; }

        [JsonProperty("timeout")]
        public int Timeout { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class BuildRequest
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class BuildStatus
    {
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("state")]
        public string State { get; set; }
        [JsonProperty("trace")]
        public string Trace { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class BuildResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        [JsonProperty("output")]
        public string Output { get; set; }
    }
}
