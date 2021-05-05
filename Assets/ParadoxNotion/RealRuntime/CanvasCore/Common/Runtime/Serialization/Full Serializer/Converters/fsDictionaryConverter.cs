using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace ParadoxNotion.Serialization.FullSerializer.Internal
{
    // While the generic IEnumerable converter can handle dictionaries, we process them separately here because
    // we support a few more advanced use-cases with dictionaries, such as inline strings. Further, dictionary
    // processing in general is a bit more advanced because a few of the collection implementations are buggy.
    public class fsDictionaryConverter : fsConverter
    {
        public override bool CanProcess(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        public override object CreateInstance(fsData data, Type storageType)
        {
            return fsMetaType.Get(storageType).CreateInstance();
        }

        public override fsResult TrySerialize(object instance_, out fsData serialized, Type storageType)
        {
            serialized = fsData.Null;
            fsResult result = fsResult.Success;
            IDictionary instance = (IDictionary)instance_;

            Type[] args = instance.GetType().RTGetGenericArguments();
            Type keyStorageType = args[0];
            Type valueStorageType = args[1];

            bool allStringKeys = true;
            List<fsData> serializedKeys = new List<fsData>(instance.Count);
            List<fsData> serializedValues = new List<fsData>(instance.Count);

            // No other way to iterate dictionaries and still have access to the key/value info
            IDictionaryEnumerator enumerator = instance.GetEnumerator();

            while (enumerator.MoveNext())
            {
                fsData keyData, valueData;
                if ((result += Serializer.TrySerialize(keyStorageType, enumerator.Key, out keyData)).Failed)
                {
                    return result;
                }

                if ((result += Serializer.TrySerialize(valueStorageType, enumerator.Value, out valueData)).Failed)
                {
                    return result;
                }

                serializedKeys.Add(keyData);
                serializedValues.Add(valueData);

                allStringKeys &= keyData.IsString;
            }

            if (allStringKeys)
            {

                serialized = fsData.CreateDictionary();
                Dictionary<string, fsData> serializedDictionary = serialized.AsDictionary;

                for (int i = 0; i < serializedKeys.Count; ++i)
                {
                    fsData key = serializedKeys[i];
                    fsData value = serializedValues[i];
                    serializedDictionary[key.AsString] = value;
                }

            }
            else
            {

                serialized = fsData.CreateList(serializedKeys.Count);
                List<fsData> serializedList = serialized.AsList;

                for (int i = 0; i < serializedKeys.Count; ++i)
                {
                    fsData key = serializedKeys[i];
                    fsData value = serializedValues[i];

                    Dictionary<string, fsData> container = new Dictionary<string, fsData>();
                    container["Key"] = key;
                    container["Value"] = value;
                    serializedList.Add(new fsData(container));
                }
            }

            return result;
        }

        public override fsResult TryDeserialize(fsData data, ref object instance_, Type storageType)
        {
            IDictionary instance = (IDictionary)instance_;
            fsResult result = fsResult.Success;

            Type[] args = instance.GetType().RTGetGenericArguments();
            Type keyStorageType = args[0];
            Type valueStorageType = args[1];

            instance.Clear();

            //(string, T)
            if (data.IsDictionary)
            {
                foreach (KeyValuePair<string, fsData> entry in data.AsDictionary)
                {
                    if (fsSerializer.IsReservedKeyword(entry.Key))
                    {
                        continue;
                    }

                    fsData keyData = new fsData(entry.Key);
                    fsData valueData = entry.Value;
                    object keyInstance = null;
                    object valueInstance = null;

                    if ((result += Serializer.TryDeserialize(keyData, keyStorageType, ref keyInstance)).Failed)
                    {
                        return result;
                    }

                    if ((result += Serializer.TryDeserialize(valueData, valueStorageType, ref valueInstance)).Failed)
                    {
                        return result;
                    }

                    instance.Add(keyInstance, valueInstance);
                }

                return result;
            }

            //(T, T)
            if (data.IsList)
            {
                List<fsData> list = data.AsList;
                for (int i = 0; i < list.Count; ++i)
                {
                    fsData item = list[i];
                    fsData keyData;
                    fsData valueData;
                    if ((result += CheckType(item, fsDataType.Object)).Failed)
                    {
                        return result;
                    }

                    if ((result += CheckKey(item, "Key", out keyData)).Failed)
                    {
                        return result;
                    }

                    if ((result += CheckKey(item, "Value", out valueData)).Failed)
                    {
                        return result;
                    }

                    object keyInstance = null;
                    object valueInstance = null;
                    if ((result += Serializer.TryDeserialize(keyData, keyStorageType, ref keyInstance)).Failed)
                    {
                        return result;
                    }

                    if ((result += Serializer.TryDeserialize(valueData, valueStorageType, ref valueInstance)).Failed)
                    {
                        return result;
                    }

                    instance.Add(keyInstance, valueInstance);
                }

                return result;
            }

            return FailExpectedType(data, fsDataType.Array, fsDataType.Object);
        }
    }
}