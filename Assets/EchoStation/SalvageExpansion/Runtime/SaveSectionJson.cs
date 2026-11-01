using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace EchoStation.SalvageExpansion
{
    public static class SaveSectionJson
    {
        public static JObject Fields(JToken value,params string[] names){var obj=value as JObject;if(obj==null||obj.Count!=names.Length||names.Any(n=>obj.Property(n,StringComparison.Ordinal)==null))throw new ArgumentException("Save section fields mismatch.");return obj;}
        public static bool Boolean(JToken value){if(value==null||value.Type!=JTokenType.Boolean)throw new ArgumentException("Boolean required.");return value.Value<bool>();}
        public static float Number(JToken value,float min,float max){if(value==null||value.Type!=JTokenType.Float&&value.Type!=JTokenType.Integer)throw new ArgumentException("Number required.");float f=value.Value<float>();if(!float.IsFinite(f)||f<min||f>max)throw new ArgumentException("Number outside section range.");return f;}
        public static int Integer(JToken value,int min,int max){if(value==null||value.Type!=JTokenType.Integer)throw new ArgumentException("Integer required.");int i=value.Value<int>();if(i<min||i>max)throw new ArgumentException("Integer outside section range.");return i;}
    }
}
