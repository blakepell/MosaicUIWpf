/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Scripting;
using System.Net;

namespace Mosaic.UI.Wpf.Scripting.ScriptCommands
{
    /// <summary>
    /// Hash Script Commands
    /// </summary>
    /// <remarks>
    /// TODO: Use a shared WebClient where the headers can be passed in.
    /// </remarks>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [ScriptModule(Name = "http")]
    public partial class HttpScriptCommands
    {
        /// <summary>
        /// Downloads a string from a URL using the GET method.
        /// </summary>
        /// <param name="url"></param>
        public string Get(string url)
        {
            using (var client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }

        /// <summary>
        /// Downloads a string from a URL using the GET method.
        /// </summary>
        /// <param name="url"></param>
        public string Get(string url, string headerKey, string headerValue)
        {
            using (var client = new WebClient())
            {
                client.Headers.Add(headerKey, headerValue);
                return client.DownloadString(url);
            }
        }

        /// <summary>
        /// Downloads a string from a URL using the POST method.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        public string Post(string url)
        {
            using (var client = new WebClient())
            {
                return client.UploadString(url, "");
            }
        }

        /// <summary>
        /// Downloads a string from a URL using the POST method.  Data is a formatted string
        /// posted as a form in the format: "Time = 12:00am temperature = 50";
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        [Description("Downloads a string from a URL using the POST method.  Post form values in the format: LastName=Smith")]
        public string Post(string url, string data)
        {
            using (var client = new WebClient())
            {
                return client.UploadString(url, data);
            }
        }

        /// <summary>
        /// Downloads a string from a URL using the POST method.  Data is a formatted string
        /// posted as a form in the format: "Time = 12:00am temperature = 50";
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="headerKey"></param>
        /// <param name="headerValue"></param>
        [Description("Downloads a string from a URL using the POST method.  Post form values in the format: LastName=Smith")]
        public string Post(string url, string data, string headerKey, string headerValue)
        {
            using (var client = new WebClient())
            {
                client.Headers.Add(headerKey, headerValue);
                return client.UploadString(url, data);
            }
        }

        /// <summary>
        /// Downloads a string from a URL using the PUT method.
        /// </summary>
        /// <param name="url"></param>
        public string Put(string url)
        {
            using (var client = new WebClient())
            {
                return client.UploadString(url, "PUT");
            }
        }

        /// <summary>
        /// Downloads a string from a URL using the PUT method.  Data is a formatted string
        /// posted as a form in the format: "Time = 12:00am temperature = 50";
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        [Description("Downloads a string from a URL using the PUT method.  Post form values in the format: LastName=Smith")]
        public string Put(string url, string data)
        {
            using (var client = new WebClient())
            {
                return client.UploadString(url, "PUT", data);
            }
        }

        /// <summary>
        /// Downloads a string from a URL using the PUT method.  Data is a formatted string
        /// posted as a form in the format: "Time = 12:00am temperature = 50";
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="headerKey"></param>
        /// <param name="headerValue"></param>
        [Description("Downloads a string from a URL using the PUT method.  Post form values in the format: LastName=Smith")]
        public string Put(string url, string data, string headerKey, string headerValue)
        {
            using (var client = new WebClient())
            {
                client.Headers.Add(headerKey, headerValue);
                return client.UploadString(url, "PUT", data);
            }
        }

        /// <summary>
        /// Downloads a string from a URL using the DELETE method.
        /// </summary>
        /// <param name="url"></param>
        public string Delete(string url)
        {
            using (var client = new WebClient())
            {
                return client.UploadString(url, "DELETE", "");
            }
        }

        /// <summary>
        /// Downloads a string from a URL using the DELETE method.  Data is a formatted string
        /// posted as a form in the format: "Time = 12:00am temperature = 50";
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        [Description("Downloads a string from a URL using the DELETE method.  Post form values in the format: LastName=Smith")]
        public string Delete(string url, string data)
        {
            using (var client = new WebClient())
            {
                return client.UploadString(url, "DELETE", data);
            }
        }

        /// <summary>
        /// Downloads a string from a URL using the DELETE method.  Data is a formatted string
        /// posted as a form in the format: "Time = 12:00am temperature = 50";
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="headerKey"></param>
        /// <param name="headerValue"></param>
        [Description("Downloads a string from a URL using the DELETE method.  Post form values in the format: LastName=Smith")]
        public string Delete(string url, string data, string headerKey, string headerValue)
        {
            using (var client = new WebClient())
            {
                client.Headers.Add(headerKey, headerValue);
                return client.UploadString(url, "DELETE", data);
            }
        }

        /// <summary>
        /// Encodes a value for use in a URL.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Description("Encodes a value for use in a URL.")]
        public string UrlEncode(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            return value.UrlEncode() ?? "";
        }

        /// <summary>
        /// Encodes a value for use in a URL.
        /// </summary>
        /// <param name="value"></param>
        public string UrlDecode(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            return value.UrlDecode() ?? "";
        }
    }
}