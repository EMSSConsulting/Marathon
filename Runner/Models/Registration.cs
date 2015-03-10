using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marathon.Models
{
    public class RegistrationRequest
    {
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("tag_list")]
        public string TagList { get; set; }
    }

    public class RegistrationResponse
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
