﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace ProductListSwatchColorShopifyApp.Common
{
    public class Helpers
    {
        public static bool IsAuthenticRequest(NameValueCollection querystring, string shopifySecretKey)
        {
            string hmac = querystring.Get("hmac");

            if (string.IsNullOrEmpty(hmac))
            {
                return false;
            }

            Func<string, bool, string> replaceChars = (string s, bool isKey) =>
            {
                //Important: Replace % before replacing &. Else second replace will replace those %25s.
                string output = (s?.Replace("%", "%25").Replace("&", "%26")) ?? "";

                if (isKey)
                {
                    output = output.Replace("=", "%3D");
                }

                return output;
            };

            var kvps = querystring.Cast<string>()
                .Select(s => new { Key = replaceChars(s, true), Value = replaceChars(querystring[s], false) })
                .Where(kvp => kvp.Key != "signature" && kvp.Key != "hmac")
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Key}={kvp.Value}");

            var hmacHasher = new HMACSHA256(Encoding.UTF8.GetBytes(shopifySecretKey));
            var hash = hmacHasher.ComputeHash(Encoding.UTF8.GetBytes(string.Join("&", kvps)));

            //Convert bytes back to string, replacing dashes, to get the final signature.
            var calculatedSignature = BitConverter.ToString(hash).Replace("-", "");

            //Request is valid if the calculated signature matches the signature from the querystring.
            return calculatedSignature.ToUpper() == hmac.ToUpper();
        }
    }
}