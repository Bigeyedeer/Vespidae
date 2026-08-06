using System;
using System.IO;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CreativeHub
{
    /// <summary>
    /// Talks to the CreativeHub desktop app on loopback.
    ///
    /// CreativeHub owns the portfolio library, so we ask it where to drop files,
    /// write the PNG ourselves, then tell it to ingest. If CreativeHub isn't
    /// running the capture is skipped quietly - Unity must never stall or error
    /// because a companion app is closed.
    /// </summary>
    public static class CreativeHubBridge
    {
        private const string Endpoint = "http://127.0.0.1:8765/capture";
        private const int TimeoutMs = 2000;

        /// <summary>Project name used to group captures. Falls back to the folder name.</summary>
        public static string ProjectName
        {
            get
            {
                var custom = EditorPrefs.GetString(CreativeHubPrefs.ProjectKey, "");
                if (!string.IsNullOrEmpty(custom)) return custom;
                if (!string.IsNullOrEmpty(Application.productName)) return Application.productName;
                return new DirectoryInfo(Path.GetDirectoryName(Application.dataPath) ?? "Unity").Name;
            }
        }

        /// <summary>Absolute path of the Unity project root (the folder holding Assets/).</summary>
        public static string ProjectPath =>
            Path.GetDirectoryName(Application.dataPath)?.Replace('/', Path.DirectorySeparatorChar) ?? "";

        /// <summary>
        /// Announce the project and get the folder to drop captures into.
        /// Returns null when CreativeHub is unreachable or has no portfolio folder set.
        /// </summary>
        public static string RequestIncomingFolder()
        {
            var payload = string.Format(
                "{{\"project\":{0},\"app\":\"Unity\",\"source_path\":{1}}}",
                Quote(ProjectName), Quote(ProjectPath));

            var reply = Post(payload);
            if (string.IsNullOrEmpty(reply)) return null;

            var incoming = ExtractString(reply, "incoming");
            return Directory.Exists(incoming) ? incoming : null;
        }

        /// <summary>Tell CreativeHub to ingest whatever we just wrote.</summary>
        public static void NotifyIngest()
        {
            Post(string.Format(
                "{{\"project\":{0},\"app\":\"Unity\",\"source_path\":{1}}}",
                Quote(ProjectName), Quote(ProjectPath)));
        }

        public static bool IsConnected()
        {
            return !string.IsNullOrEmpty(Post("{\"project\":\"__ping__\"}"));
        }

        private static string Post(string json)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(Endpoint);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = TimeoutMs;
                request.ReadWriteTimeout = TimeoutMs;

                var bytes = Encoding.UTF8.GetBytes(json);
                request.ContentLength = bytes.Length;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream() ?? Stream.Null))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                // CreativeHub closed, port busy, or blocked - never surface this.
                return null;
            }
        }

        // Small helpers so the extension has no JSON dependency.

        private static string Quote(string value)
        {
            if (value == null) value = "";
            var sb = new StringBuilder("\"");
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.Append('"').ToString();
        }

        private static string ExtractString(string json, string key)
        {
            var marker = "\"" + key + "\"";
            var at = json.IndexOf(marker, StringComparison.Ordinal);
            if (at < 0) return null;
            var colon = json.IndexOf(':', at + marker.Length);
            if (colon < 0) return null;
            var open = json.IndexOf('"', colon + 1);
            if (open < 0) return null;

            var sb = new StringBuilder();
            for (var i = open + 1; i < json.Length; i++)
            {
                var c = json[i];
                if (c == '\\' && i + 1 < json.Length)
                {
                    var next = json[++i];
                    sb.Append(next == 'n' ? '\n' : next == 't' ? '\t' : next);
                    continue;
                }
                if (c == '"') break;
                sb.Append(c);
            }
            return sb.ToString();
        }

        public static string JsonEscape(string value) => Quote(value);
    }

    internal static class CreativeHubPrefs
    {
        public const string ProjectKey = "CreativeHub.ProjectName";
        public const string AutoKey = "CreativeHub.AutoCapture";
        public const string IntervalKey = "CreativeHub.IntervalMinutes";
        public const string SourceKey = "CreativeHub.CaptureSource";
    }
}
