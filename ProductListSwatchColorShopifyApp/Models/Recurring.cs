using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProductListSwatchColorShopifyApp.Models
{
    public class Recurring
    {
        [JsonProperty("recurring_application_charge", NullValueHandling = NullValueHandling.Ignore)]
        public ChargeDetails Charge { get; set; }
    }
}