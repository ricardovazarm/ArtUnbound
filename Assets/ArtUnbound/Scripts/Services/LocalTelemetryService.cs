using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ArtUnbound.Services
{
    public class LocalTelemetryService
    {
        private readonly string logPath;

        public LocalTelemetryService()
        {
            logPath = Path.Combine(Application.persistentDataPath, "telemetry.log");
        }

        public void LogEvent(string name, Dictionary<string, object> data)
        {
            string payload = data == null ? string.Empty : JsonUtility.ToJson(new SerializableDict(data));
            File.AppendAllText(logPath, $"{System.DateTime.Now:o} {name} {payload}\n");
        }

        [System.Serializable]
        private class SerializableDict
        {
            public List<string> keys = new List<string>();
            public List<string> values = new List<string>();

            public SerializableDict(Dictionary<string, object> data)
            {
                foreach (var item in data)
                {
                    keys.Add(item.Key);
                    values.Add(item.Value != null ? item.Value.ToString() : string.Empty);
                }
            }
        }
    }
}
