﻿using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Converters;
using System.Globalization;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Microsoft.ClearScript.V8
{
    /// <summary>
    /// stuff
    /// </summary>
    public interface IV8ScriptItem
    {
        //V8ScriptEngine Engine { get; }
        /// <summary>
        /// is array
        /// </summary>
        /// <returns>if array</returns>
        JsTypes GetJsType();

    }
    /// <summary>
    /// enum of javascript types
    /// </summary>
    public enum JsTypes
    {
        /// <summary>
        /// obj
        /// </summary>
        JsObject=0,
        /// <summary>
        /// arr
        /// </summary>
        JsArray=1,
        /// <summary>
        /// date
        /// </summary>
        JsDate=2,
        /// <summary>
        /// fn
        /// </summary>
        jsFunction=3,
        /// <summary>
        /// fn
        /// </summary>
        jsNull=4,
        /// <summary>
        /// fn
        /// </summary>
        jsUndefined = 5,
        /// <summary>
        /// fn
        /// </summary>
        jsArguments = 6,
        /// <summary>
        /// fn
        /// </summary>
        jsError = 7,
    }
    /// <summary>
    /// blurp
    /// </summary>
    public class V8ContractResolver : DefaultContractResolver
    {
        /// <summary>
        /// blasdf
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(IV8ScriptItem).IsAssignableFrom(objectType))
                return new V8ScriptItemConverter(); // pretend converter is not specified
            var jc = base.ResolveContractConverter(objectType);
            return jc;
        }
    }

    //public class V8ScriptItemConverter : JsonConverter
    //{
    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {

    //    }
    //}

    /// <summary>
    /// good
    /// </summary>
    public class V8ScriptItemConverter : JsonConverter
    {

        bool ShouleSerialize(object item, out object ret)
        {
            ret = item;
            if (item == null)
            {
                return true;
            }
            if (item is V8ScriptItem && ((V8ScriptItem)item).GetJsType() == JsTypes.jsFunction)
            {
                ret = null;
                return false;
            }
            if (item is Microsoft.ClearScript.HostMethod)
            {
                ret = null;
                return false;
            }
            if (item is Task)
            {
                ret = null;
                return false;
            }
            if (item is Undefined)
            {
                ret = null;
            }
            if (item is Delegate)
            {
                ret = null;
                return false;
            }
            return true;
        }
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var v8Item = value as V8ScriptItem;
            var jsType = v8Item.GetJsType();
            object outObj;
            if (jsType == JsTypes.jsError)
            {
                Dictionary<string, object> obj = new Dictionary<string, object>();
                obj["message"] = v8Item.GetProperty("message") as string;
                obj["stack"] = v8Item.GetProperty("stack") as string;
                foreach (var pname in v8Item.GetPropertyNames())
                {
                    var item = v8Item.GetProperty(pname);
                    if (ShouleSerialize(item, out outObj))
                    {
                        obj[pname] = outObj;
                    }
                   
                }
         
                serializer.Serialize(writer, obj, obj.GetType());
            }
            else if (jsType == JsTypes.JsObject || jsType == JsTypes.JsArray)
            {
                
                try
                {
                    var json = v8Item.Engine.Script.JSON.stringify(v8Item);
                    writer.WriteRaw(json);


                    if (writer is JTokenWriter)
                    {
                        var tw = new StringReader(json);
                        var jsonReader = new JsonTextReader(tw);

                        writer.WriteToken(jsonReader);

                    }
                    else
                    {
                        writer.WriteRaw(json);
                    }
                }
                catch (Exception)
                {
                    
                    throw;
                }
                
                
            }
            else if (jsType == JsTypes.JsDate)
            {
                var dateString = (string)v8Item.InvokeMethod("toISOString", new object[0]);
                DateTime dt = DateTime.Parse(dateString);
                serializer.Serialize(writer, dt, dt.GetType());
            }
            else
            {
                writer.WriteNull();
            }
        }

        

        /// <summary>
        /// Gets a value indicating whether this <see cref="JsonConverter"/> can write JSON.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this <see cref="JsonConverter"/> can write JSON; otherwise, <c>false</c>.
        /// </value>
        public override bool CanWrite
        {
            get { return true; }
        }


        /// <summary>
        /// adsf
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType)
        {
            return false;
        }
        /// <summary>
        /// adsf
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }


}
