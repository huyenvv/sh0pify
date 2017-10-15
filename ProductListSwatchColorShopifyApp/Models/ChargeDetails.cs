using Newtonsoft.Json;

namespace ProductListSwatchColorShopifyApp.Models
{
    public class ChargeDetails
    {
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("price", NullValueHandling = NullValueHandling.Ignore)]
        public string Price { get; set; }

        [JsonProperty("returnurl", NullValueHandling = NullValueHandling.Ignore)]
        public string ReturnUrl { get; set; }

        [JsonProperty("test", NullValueHandling = NullValueHandling.Ignore)]
        public string Test { get; set; }

        [JsonProperty("trialdays", NullValueHandling = NullValueHandling.Ignore)]
        public string TrialDays { get; set; }
    }
}