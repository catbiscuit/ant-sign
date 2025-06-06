﻿using HtmlAgilityPack;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace AntSign
{
    public static class SendUtil
    {
        public static int SendEMail(string smtp_Server, int smtp_Port, string smtp_Email, string smtp_Password, List<string> receive_Email_List, string title, string content, string topicName)
        {
            if (string.IsNullOrWhiteSpace(smtp_Email) || string.IsNullOrWhiteSpace(smtp_Password) || receive_Email_List == null || receive_Email_List.Count == 0 || receive_Email_List.All(string.IsNullOrWhiteSpace))
            {
                Console.WriteLine("【EMail】RECEIVE_EMAIL_LIST is null");
                return 0;
            }

            MailAddress fromMail = new(smtp_Email, topicName);
            foreach (var item in receive_Email_List)
            {
                if (string.IsNullOrWhiteSpace(item))
                    continue;

                MailAddress toMail = new(item);

                MailMessage mail = new(fromMail, toMail)
                {
                    IsBodyHtml = false,
                    Subject = title,
                    Body = content
                };

                SmtpClient client = new()
                {
                    EnableSsl = true,
                    Host = smtp_Server,
                    Port = smtp_Port,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(smtp_Email, smtp_Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                client.Send(mail);
            }

            Console.WriteLine("【EMail】Success");
            return 1;
        }

        public static async Task<int> SendBark(string bark_Devicekey, string bark_Icon, string title, string content)
        {
            if (string.IsNullOrWhiteSpace(bark_Devicekey))
            {
                Console.WriteLine("【Bark】BARK_DEVICEKEY is empty");
                return 0;
            }

            string url = "https://api.day.app/push";
            if (string.IsNullOrWhiteSpace(bark_Icon) == false)
                url = url + "?icon=" + bark_Icon;

            Dictionary<string, string> headers = new()
            {
                { "charset", "utf-8" }
            };

            Dictionary<string, object> param = new()
            {
                { "title", title },
                { "body", content },
                { "device_key", bark_Devicekey }
            };

            var _client = new HttpClient();

            var p = param.ToJson();

            HttpContent httpContent = new StringContent(p);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            foreach (var item in headers)
            {
                httpContent.Headers.Remove(item.Key);
                httpContent.Headers.Add(item.Key, item.Value);
            }

            HttpResponseMessage response = await _client.PostAsync(url, httpContent);

            string result = string.Empty;
            if (response.IsSuccessStatusCode)
                result = await response.Content.ReadAsStringAsync();

            var jObject = result.TryToObject<JsonObject>();
            try
            {
                if (jObject == null)
                {
                    Console.WriteLine("【Bark】Send message to Bark Error");
                    return -1;
                }
                else
                {
                    if (int.TryParse(jObject["code"]?.ToString(), out int code) && code == 200)
                    {
                        Console.WriteLine("【Bark】Send message to Bark successfully");
                        return 1;
                    }
                    else
                    {
                        Console.WriteLine($"【Bark】Send Message Response.{jObject["text"]?.ToString()}");
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("【Bark】Send message to Bark Catch." + (ex?.Message ?? ""));
                return -1;
            }
        }
    }

    public static class Util
    {
        public static string DesensitizeStr(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return "";

            if (str.Length <= 8)
            {
                int ln = Math.Max((int)Math.Floor((double)str.Length / 3), 1);
                return str[..ln] + "**" + str[^ln..];
            }

            return str[..3] + "**" + str[^4..];
        }

        public static long GetTimeStamp_Seconds()
        {
            DateTime currentTime = DateTime.UtcNow;
            DateTime unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan elapsedTime = currentTime - unixEpoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static long GetTimeStamp_Milliseconds()
        {
            DateTime currentTime = DateTime.UtcNow;
            DateTime unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan elapsedTime = currentTime - unixEpoch;
            return (long)elapsedTime.TotalMilliseconds;
        }

        public static string GetFakeIP()
        {
            Random rd = new(Guid.NewGuid().GetHashCode());
            return $"233.{rd.Next(64, 117)}.{rd.Next(0, 255)}.{rd.Next(0, 255)}";
        }

        public static DateTime GetBeiJingTime()
        {
            DateTime nowUtc = DateTime.UtcNow;
            TimeZoneInfo beijingTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
            DateTime nowBeiJing = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, beijingTimeZone);
            return nowBeiJing;
        }

        public static string GetBeiJingTimeStr()
        {
            var dt = GetBeiJingTime();
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string BodyUrlEncode(Dictionary<string, string> parameters) => string.Join("&", parameters.Select(p => WebUtility.UrlEncode(p.Key) + "=" + WebUtility.UrlEncode(p.Value)));

        public static T ToObject<T>(this string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            return string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });
        }

        public static T TryToObject<T>(this string json)
        {
            try
            {
                return json.ToObject<T>();
            }
            catch
            {
                return default;
            }
        }

        public static string ToJson(this object obj)
        {
            var options = new JsonSerializerOptions
            {
                Converters =
                {
                    new DateTimeConverterUsingDateTimeFormat("yyyy-MM-dd HH:mm:ss")
                }
            };

            return JsonSerializer.Serialize(obj, options);
        }

        public static string GetEnvValue(string key)
        {
            string str = Environment.GetEnvironmentVariable(key);

#if DEBUG
            if (string.IsNullOrWhiteSpace(str))
            {

            }
#endif

            return str;
        }
    }

    public class DateTimeConverterUsingDateTimeFormat : JsonConverter<DateTime>
    {
        private readonly string _format;

        public DateTimeConverterUsingDateTimeFormat(string format)
        {
            _format = format;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.ParseExact(reader.GetString(), _format, null);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format));
        }
    }

    public static class HttpClientHelper
    {
        private static readonly HttpClient _httpClient = new();

        public static async Task<string> HttpGet(string url, int seconds = 15)
        {
            if (_httpClient.Timeout == default)
                _httpClient.Timeout = new TimeSpan(0, 0, seconds);

            HttpResponseMessage response = await _httpClient.GetAsync(url);

            string result = string.Empty;
            if (response.IsSuccessStatusCode)
                result = await response.Content.ReadAsStringAsync();

            return result;
        }

        public static async Task<HtmlDocument> HttpHtml(string url, int seconds = 15)
        {
            var html = await HttpGet(url, seconds);

            HtmlDocument document = new();
            document.LoadHtml(html);

            return document;
        }
    }
}
