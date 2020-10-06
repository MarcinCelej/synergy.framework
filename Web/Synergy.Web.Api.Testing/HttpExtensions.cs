﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Synergy.Contracts;

namespace Synergy.Web.Api.Testing
{
    public static class HttpExtensions
    {
        [MustUseReturnValue]
        public static JToken? ReadJson([CanBeNull] this HttpContent? content)
        {
            if (content == null)
                return null;
            
            Task<string> task = content.ReadAsStringAsync();
            task?.Wait();
            var str = task?.Result;
            if (string.IsNullOrWhiteSpace(str))
                return null;

            var contentType = content.Headers.ContentType.MediaType;
            if (contentType != MediaTypeNames.Application.Json && contentType != "application/problem+json")
            {
                throw Fail.Because(Violation.Of("Content-Type is not JSON. It is \"{0}\"", contentType));
            }

            return JToken.Parse(str);
        }

        [MustUseReturnValue]
        public static HttpContent? Read<T>([NotNull] this HttpContent? content, string jsonPath, out T value)
        {
            value = content.Read<T>(jsonPath);
            return content;
        }

        public static T Read<T>([NotNull] this HttpContent? content)
            => Read<T>(content, "");
        
        [MustUseReturnValue]
        public static T Read<T>([NotNull] this HttpContent? content, string jsonPath)
        {
            Fail.IfNull(content, nameof(content));
            JToken? json = content.ReadJson();
            var node = json!.SelectToken(jsonPath).FailIfNull(Violation.Of($"Cannot find JSON node '{jsonPath}'"));
            if (node is JObject)
            {
                return node.ToObject<T>();
            }
            
            return node.Value<T>();
        }

        [Pure]
        public static string GetRequestFullMethod(this HttpRequestMessage request)
            => $"{request.Method} {request.GetRequestRelativeUrl()}";

        
        [Pure]
        public static string GetRequestRelativeUrl(this HttpRequestMessage request)
            => request.RequestUri.ToString().Replace("http://localhost", "");

        public static List<KeyValuePair<string, IEnumerable<string>>> GetAllHeaders(this HttpRequestMessage request)
        {
            var headers = request.Headers.ToList();
            if (request.Content != null)
                headers.AddRange(request.Content.Headers);

            return headers;
        }

        public static List<KeyValuePair<string, IEnumerable<string>>> GetAllHeaders(this HttpResponseMessage response)
        {
            var headers = response.Headers.ToList();
            if (response.Content != null)
                headers.AddRange(response.Content.Headers);

            return headers;
        }

        [NotNull]
        public static string ToHttpLook(this HttpRequestMessage request)
        {
            var report = new StringBuilder();
            report.AppendLine(request.GetRequestFullMethod());
            InsertHeaders(report, request.GetAllHeaders());
            var requestBody = request.Content.ReadJson();
            if (requestBody != null)
            {
                report.Append(requestBody.ToString(Formatting.Indented));
            }
            return report.ToString().Trim();
        }

        [NotNull]
        public static string ToHttpLook(this HttpResponseMessage response)
        {
            var report = new StringBuilder();
            report.AppendLine($"HTTP/{response.Version} {(int) response.StatusCode} {response.StatusCode}");
            InsertHeaders(report, response.GetAllHeaders());
            var responseBody = response.Content.ReadJson();
            if (responseBody != null)
                report.Append(responseBody.ToString(Formatting.Indented));

            return report.ToString().Trim();
        }

        private static void InsertHeaders(StringBuilder report, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            foreach (var header in headers)
            {
                var value = String.Join(", ", header.Value);
                if (String.IsNullOrWhiteSpace(value))
                    continue;

                report.AppendLine($"{header.Key}: {value}");
            }
        }
    }
}