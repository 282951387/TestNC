﻿using ParadoxNotion.Serialization.FullSerializer;
using ParadoxNotion.Serialization.FullSerializer.Internal;
using ParadoxNotion.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ParadoxNotion.Serialization
{

    ///High-Level API. Serializes/Deserializes to/from JSON with a heavily modified 'FullSerializer'
    public static class JSONSerializer
    {

        private static readonly object serializerLock;
        private static fsSerializer serializer;
        private static Dictionary<string, fsData> dataCache;

        static JSONSerializer()
        {
            serializerLock = new object();
            FlushMem();
        }

        public static void FlushMem()
        {
            serializer = new fsSerializer();
            dataCache = new Dictionary<string, fsData>();
            fsMetaType.FlushMem();
        }

#if UNITY_2019_3_OR_NEWER
        //for "no domain reload"
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void __FlushDataCache() { dataCache = new Dictionary<string, fsData>(); }
#endif

        ///----------------------------------------------------------------------------------------------

        ///Serialize to json
        public static string Serialize(Type type, object instance, List<UnityEngine.Object> references = null, bool pretyJson = false)
        {

            lock (serializerLock)
            {

                serializer.PurgeTemporaryData();
                serializer.ReferencesDatabase = references;

                fsData data;
                //We override the UnityObject converter if we serialize a UnityObject directly.
                //UnityObject converter will still be used for every serialized property found within the object though.
                Type overrideConverterType = typeof(UnityEngine.Object).RTIsAssignableFrom(type) ? typeof(fsReflectedConverter) : null;
                fsResult r = serializer.TrySerialize(type, instance, out data, overrideConverterType).AssertSuccess();
                if (r.HasWarnings) { Logger.LogWarning(r.ToString(), "Serialization"); }

                serializer.ReferencesDatabase = null;

                string json = fsJsonPrinter.ToJson(data, pretyJson);

                if (Threader.applicationIsPlaying || UnityEngine.Application.isPlaying)
                {
                    dataCache[json] = data;
                }

                return json;
            }
        }

        ///----------------------------------------------------------------------------------------------

        ///Deserialize from json
        public static T Deserialize<T>(string json, List<UnityEngine.Object> references = null)
        {
            return (T)Internal_Deserialize(typeof(T), json, references, null);
        }

        ///Deserialize from json
        public static object Deserialize(Type type, string json, List<UnityEngine.Object> references = null)
        {
            return Internal_Deserialize(type, json, references, null);
        }

        ///Deserialize overwrite from json
        public static T TryDeserializeOverwrite<T>(T instance, string json, List<UnityEngine.Object> references = null) where T : class
        {
            return (T)Internal_Deserialize(typeof(T), json, references, instance);
        }

        ///Deserialize overwrite from json
        public static object TryDeserializeOverwrite(object instance, string json, List<UnityEngine.Object> references = null)
        {
            return Internal_Deserialize(instance.GetType(), json, references, instance);
        }

        ///Deserialize from json
        private static object Internal_Deserialize(Type type, string json, List<UnityEngine.Object> references, object instance)
        {

            lock (serializerLock)
            {

                serializer.PurgeTemporaryData();

                fsData data = null;

                if (Threader.applicationIsPlaying)
                {
                    //caching is useful only in playmode realy since editing is finalized
                    if (!dataCache.TryGetValue(json, out data))
                    {
                        dataCache[json] = data = fsJsonParser.Parse(json);
                    }
                }
                else
                {
                    //in editor we just parse it
                    data = fsJsonParser.Parse(json);
                }

                serializer.ReferencesDatabase = references;
                //We use Reflected converter if we deserialize overwrite a UnityObject directly.
                //UnityObject converter will still be used for every serialized property found within the object though.
                Type overrideConverterType = instance is UnityEngine.Object ? typeof(fsReflectedConverter) : null;
                fsResult r = serializer.TryDeserialize(data, type, ref instance, overrideConverterType).AssertSuccess();
                if (r.HasWarnings) { Logger.LogWarning(r.ToString(), "Serialization"); }

                serializer.ReferencesDatabase = null;

                return instance;
            }
        }

        ///Serialize instance without cycle refs support and execute call per object serialized within along with it's serialization data
        public static void SerializeAndExecuteNoCycles(Type type, object instance, Action<object, fsData> call)
        {
            lock (serializerLock)
            {
                serializer.IgnoreSerializeCycleReferences = true;
                serializer.onAfterObjectSerialized += call;
                try { Serialize(type, instance); }
                finally
                {
                    serializer.IgnoreSerializeCycleReferences = false;
                    serializer.onAfterObjectSerialized -= call;
                }
            }
        }

        ///Serialize instance without cycle refs support and execute before/after call per object serialized within along with it's serialization data
        public static void SerializeAndExecuteNoCycles(Type type, object instance, Action<object> beforeCall, Action<object, fsData> afterCall)
        {
            lock (serializerLock)
            {
                serializer.IgnoreSerializeCycleReferences = true;
                serializer.onBeforeObjectSerialized += beforeCall;
                serializer.onAfterObjectSerialized += afterCall;
                try { Serialize(type, instance); }
                finally
                {
                    serializer.IgnoreSerializeCycleReferences = false;
                    serializer.onBeforeObjectSerialized -= beforeCall;
                    serializer.onAfterObjectSerialized -= afterCall;
                }
            }
        }

        ///Deep clone an object
        public static T Clone<T>(T original)
        {
            return (T)Clone((object)original);
        }

        ///Deep clone an object
        public static object Clone(object original)
        {
            Type type = original.GetType();
            List<UnityEngine.Object> references = new List<UnityEngine.Object>();
            string json = Serialize(type, original, references);
            return Deserialize(type, json, references);
        }

        ///Serialize source and overwrites target
        public static void CopySerialized(object source, object target)
        {
            Type type = source.GetType();
            List<UnityEngine.Object> references = new List<UnityEngine.Object>();
            string json = Serialize(type, source, references);
            TryDeserializeOverwrite(target, json, references);
        }

        ///Writes json to prety json in a temp file and opens it
        public static void ShowData(string json, string fileName = "")
        {
            string prettyJson = PrettifyJson(json);
            string dataPath = Path.GetTempPath() + (string.IsNullOrEmpty(fileName) ? Guid.NewGuid().ToString() : fileName) + ".json";
            File.WriteAllText(dataPath, prettyJson);
            Process.Start(dataPath);
        }

        ///Prettify existing json string
        public static string PrettifyJson(string json)
        {
            return fsJsonPrinter.PrettyJson(fsJsonParser.Parse(json));
        }
    }
}