using ParadoxNotion.Serialization.FullSerializer;
using System;


namespace ParadoxNotion.Serialization
{

    ///Handles UnityObject references serialization
	public class fsUnityObjectConverter : fsConverter
    {

        public override bool CanProcess(Type type)
        {
            return typeof(UnityEngine.Object).RTIsAssignableFrom(type);
        }

        public override bool RequestCycleSupport(Type storageType)
        {
            return false;
        }

        public override bool RequestInheritanceSupport(Type storageType)
        {
            return false;
        }

        public override fsResult TrySerialize(object instance, out fsData serialized, Type storageType)
        {

            System.Collections.Generic.List<UnityEngine.Object> database = Serializer.ReferencesDatabase;
            if (database == null)
            {
                serialized = new fsData();
                return fsResult.Success;
                // return fsResult.Warn("No database references provided for serialization");
            }

            UnityEngine.Object o = instance as UnityEngine.Object;

            //for null store 0
            if (ReferenceEquals(o, null))
            {
                serialized = new fsData(0);
                return fsResult.Success;
            }

            //this is done to avoid serializing 0 because it's default value of int and will not be printed,
            //which is done for performance. Thus we always start from index 1. 0 is always null.
            if (database.Count == 0)
            {
                database.Add(null);
            }

            //search reference match
            int index = -1;
            for (int i = 0; i < database.Count; i++)
            {
                if (ReferenceEquals(database[i], o))
                {
                    index = i;
                    break;
                }
            }

            //if no match, add new
            if (index <= 0)
            {
                index = database.Count;
                database.Add(o);
            }

            serialized = new fsData(index);
            return fsResult.Success;
        }

        public override fsResult TryDeserialize(fsData data, ref object instance, Type storageType)
        {

            System.Collections.Generic.List<UnityEngine.Object> database = Serializer.ReferencesDatabase;
            if (database == null)
            {
                return fsResult.Warn("A Unity Object reference has not been deserialized because no database references was provided.");
            }

            int index = (int)data.AsInt64;

            if (index >= database.Count)
            {
                return fsResult.Warn("A Unity Object reference has not been deserialized because no database entry was found in provided database references.");
            }

            UnityEngine.Object reference = database[index];
            if (ReferenceEquals(reference as UnityEngine.Object, null) || storageType.RTIsAssignableFrom(reference.GetType()))
            {
                instance = reference;
            }
            return fsResult.Success;
        }

        public override object CreateInstance(fsData data, Type storageType)
        {
            return null;
        }
    }
}