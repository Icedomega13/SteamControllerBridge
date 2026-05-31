using System;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Web.Http;

namespace SteamControllerBridge.GameBarWidget
{
    internal sealed class LocalControlClient
    {
        private static readonly Uri Endpoint = new Uri("http://127.0.0.1:47309/");

        public Task<LocalControlResponse> GetStatusAsync()
        {
            return SendAsync("{\"action\":\"status\"}");
        }

        public Task<LocalControlResponse> ToggleAsync()
        {
            return SendAsync("{\"action\":\"toggle\"}");
        }

        public Task<LocalControlResponse> SetRumbleIntensityAsync(int value)
        {
            return SendAsync("{\"action\":\"setRumbleIntensity\",\"value\":" + value + "}");
        }

        public Task<LocalControlResponse> SetPaddleAsync(string paddle, string output)
        {
            return SendAsync("{\"action\":\"setPaddle\",\"paddle\":\"" + Escape(paddle) + "\",\"output\":\"" + Escape(output) + "\"}");
        }

        private static async Task<LocalControlResponse> SendAsync(string json)
        {
            var client = new HttpClient();
            var content = new HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
            var response = await client.PostAsync(Endpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();
            response.Dispose();
            content.Dispose();
            client.Dispose();
            return LocalControlResponse.FromJson(responseJson);
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }

    internal sealed class LocalControlResponse
    {
        public bool Ok { get; private set; }
        public string Error { get; private set; }
        public LocalControlStatus Status { get; private set; }

        public LocalControlResponse()
        {
            Error = string.Empty;
        }

        public static LocalControlResponse FromJson(string json)
        {
            var root = JsonObject.Parse(json);
            IJsonValue ok;
            IJsonValue error;
            var result = new LocalControlResponse
            {
                Ok = root.TryGetValue("ok", out ok) && ok.GetBoolean(),
                Error = root.TryGetValue("error", out error) ? error.GetString() : string.Empty
            };

            IJsonValue statusValue;
            if (root.TryGetValue("status", out statusValue) && statusValue.ValueType == JsonValueType.Object)
            {
                var status = statusValue.GetObject();
                result.Status = new LocalControlStatus
                {
                    IsEnabled = GetBool(status, "isEnabled"),
                    IsWorking = GetBool(status, "isWorking"),
                    HasError = GetBool(status, "hasError"),
                    Message = GetString(status, "message"),
                    RumbleIntensityPercent = GetInt(status, "rumbleIntensityPercent"),
                    L4 = GetString(status, "l4", "Y"),
                    L5 = GetString(status, "l5", "X"),
                    R4 = GetString(status, "r4", "B"),
                    R5 = GetString(status, "r5", "A")
                };
            }

            return result;
        }

        private static bool GetBool(JsonObject root, string key)
        {
            IJsonValue value;
            return root.TryGetValue(key, out value) && value.GetBoolean();
        }

        private static int GetInt(JsonObject root, string key)
        {
            IJsonValue value;
            return root.TryGetValue(key, out value) ? (int)value.GetNumber() : 0;
        }

        private static string GetString(JsonObject root, string key, string fallback = "")
        {
            IJsonValue value;
            return root.TryGetValue(key, out value) ? value.GetString() : fallback;
        }
    }

    internal sealed class LocalControlStatus
    {
        public bool IsEnabled { get; set; }
        public bool IsWorking { get; set; }
        public bool HasError { get; set; }
        public string Message { get; set; }
        public int RumbleIntensityPercent { get; set; }
        public string L4 { get; set; }
        public string L5 { get; set; }
        public string R4 { get; set; }
        public string R5 { get; set; }

        public LocalControlStatus()
        {
            Message = string.Empty;
            L4 = "Y";
            L5 = "X";
            R4 = "B";
            R5 = "A";
        }
    }
}
