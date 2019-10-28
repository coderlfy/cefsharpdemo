using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AppHandleMessage
{
    public class JsonHelper
    {
        private static JsonSerializerSettings _jsParseSetting = null;
        public static JsonSerializerSettings JsParseSetting 
        {
            get {
                if (_jsParseSetting == null)
                {
                    _jsParseSetting = new JsonSerializerSettings();
                    _jsParseSetting.NullValueHandling = NullValueHandling.Ignore;
                }
                return _jsParseSetting;
            }
        }
        public static string Get(object jsonObject)
        {
            var jSetting = new JsonSerializerSettings();
            jSetting.DefaultValueHandling = DefaultValueHandling.Ignore;
            jSetting.NullValueHandling = NullValueHandling.Ignore;
            jSetting.MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead;
            //jSetting.
            return Format(JsonConvert.SerializeObject(jsonObject, jSetting));
        }

        private static string Format(string json)
        {
            JsonSerializer serializer = new JsonSerializer();
            TextReader tr = new StringReader(json);
            JsonTextReader jtr = new JsonTextReader(tr);
            object obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }
            else
            {
                return json;
            }         

        }
    }
}
