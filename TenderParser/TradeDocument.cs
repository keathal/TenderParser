using Newtonsoft.Json;
using System;

namespace TenderParser
{
    public class TradeDocument
    {
        [JsonProperty(PropertyName = "Id")]
        public string Id { get; }

        [JsonProperty(PropertyName = "UploadDate")]
        public DateTime UploadDate { get; }

        [JsonProperty(PropertyName = "FileName")]
        public string FileName { get; }

        [JsonProperty(PropertyName = "Url")]
        public string Url { get; }

        [JsonProperty(PropertyName = "UserFileNameFromOuterSystem")]
        public string UserFileNameFromOuterSystem { get; }

        [JsonProperty(PropertyName = "Type")]
        public string Type { get; }

        public TradeDocument(string id, DateTime uploadDate, string fileName, string url, string userFileNameOuterSystem, string type)
        {
            Id = id;
            UploadDate = uploadDate;
            FileName = fileName;
            Url = url;
            UserFileNameFromOuterSystem = userFileNameOuterSystem;
            Type = type;
        }

        public override string ToString()
        {
            return $"{FileName}: {Url}";
        }
    }
}
