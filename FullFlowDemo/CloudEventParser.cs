using System;
using System.Text;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FullFlowDemo
{
    public class CloudEventParser
    {
        public CloudEvent Parse(string messageBody)
        {
            var formatter = new JsonEventFormatter();
            return formatter.DecodeStructuredModeMessage(
                Encoding.UTF8.GetBytes(messageBody), null, null);
        }

        public T DeserializeData<T>(object data)
        {
            if (data is System.Text.Json.JsonElement jsonElement)
            {
                return JsonConvert.DeserializeObject<T>(jsonElement.GetRawText());
            }
            else if (data is JObject jObject)
            {
                return jObject.ToObject<T>();
            }
            else if (data is string dataString)
            {
                return JsonConvert.DeserializeObject<T>(dataString);
            }
            else
            {
                var dataJson = JsonConvert.SerializeObject(data);
                return JsonConvert.DeserializeObject<T>(dataJson);
            }
        }
    }
}
