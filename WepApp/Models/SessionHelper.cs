using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace WebApp.Models
{
    public static class SessionHelper
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            //session.SetString(key, JsonConvert.SerializeObject(value));
            session.SetString(key, JsonConvert.SerializeObject(value, Formatting.Indented,
            new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));

        }

        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            string value = session.GetString(key);
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }
        public static List<T> GetObjectFromJsonCollection<T>(this ISession session, string key)
        {
            string value = session.GetString(key);
            return value == null ? default(List<T>) : JsonConvert.DeserializeObject<List<T>>(value);

        }
    
    }
}
