﻿//-----------------------------------------------------------------------
// <copyright file="Azuqua.cs" company="Azuqua, Inc">
//     Copyright (c) Azuqua, Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using System.Net;
using System.Text;
using System.IO;

namespace Azuqua
{
/// This class encapsulates all necessary data that's needed to make
/// a request to the Flo API
    public sealed class Azuqua 
    {
        private string accessKey, accessSecret;

        /// Constructor
        public Azuqua(string accessKey, string accessSecret) {
            this.accessKey = accessKey;
            this.accessSecret = accessKey;
        }

        public void InitFromEnvironmentVariables() {
            if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("floAccessKey"))
                    && !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("floAccessSecret")))
            {
                this.accessKey = System.Environment.GetEnvironmentVariable("floAccessKey");
                this.accessSecret = System.Environment.GetEnvironmentVariable("floAccessKey");
            }
            else
            {
                throw new Exception("Missing floAccessKey or floAccessSecret environment variable");
            }
        }

        public string InvokeFlo(string alias, string data) {
            string path = "/flo/"+alias+"/invoke";
            string resp = this.MakeRequest(path, "POST", data);
            return resp;
        }

        public string MakeRequest(string path, string verb, string data) {
            if (data.Length < 1) {
                data = "";
            }
            //TODO: DOUBLE CHECK TIMESTAMP
            string timestamp = DateTime.UtcNow.ToString("s"); // ISO formatted date
            string hash = this.SignData(path, verb, data, timestamp);

            verb = verb.ToUpper();
            string host = "https://api.azuqua.com";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(host+path);
            request.Method = verb;
            request.ContentType = "application/json";
            request.Headers.Add("x-api-accessKey", this.accessKey);
            request.Headers.Add("x-api-timestamp", timestamp);
            request.Headers.Add("x-api-hash", hash);

            // Make it happen! 
            if (verb == "GET") {
                WebResponse response = request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                using (var streamReader = new StreamReader(responseStream)) {
                    string responseText = streamReader.ReadToEnd();
                    return responseText;        
                }
            }
            else if (verb == "POST") {
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                request.ContentLength = dataBytes.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(dataBytes, 0, dataBytes.Length);

                // .GetResponse both sends to the server and gets the response
                WebResponse response = request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                using (var streamReader = new StreamReader(responseStream)) {
                    string responseText = streamReader.ReadToEnd();
                    return responseText;        
                }
            }
            else {
                // Should never get here- requests should be only GET or POST
                throw new Exception("Azuqua only supports GET or POST calls");
            }

        }

        /// Generate Hash-based Message Authentication Code (HMAC) with 
        /// SHA256 and secret key
        public string SignData(string path, string verb, string data, string timestamp) {
            if (string.IsNullOrEmpty(data)) {
                return "";
            }

            string meta = verb.ToLower()+":"+path+":"+timestamp;

            var encoding = new System.Text.UTF8Encoding();
            byte[] keyByte = encoding.GetBytes(this.accessSecret);
            byte[] messageBytes = encoding.GetBytes(data+meta);

            using (var hmacsha256 = new HMACSHA256(keyByte)) {
                byte[] hashMessage = hmacsha256.ComputeHash(messageBytes);
                var hexString = BitConverter.ToString(hashMessage);
                // By default, BitConverter's output is '-' separated hex pairs
                return hexString.Replace("-", "");
            }
        }
    }
}