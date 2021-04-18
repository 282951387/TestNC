#pragma warning disable 612, 618

#if !NO_UNITY
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ParadoxNotion.Serialization.FullSerializer.Internal.DirectConverters
{
    public class Keyframe_DirectConverter : fsDirectConverter<Keyframe>
    {
        protected override fsResult DoSerialize(Keyframe model, Dictionary<string, fsData> serialized)
        {
            fsResult result = fsResult.Success;

            result += SerializeMember(serialized, null, "time", model.time);
            result += SerializeMember(serialized, null, "value", model.value);
            result += SerializeMember(serialized, null, "tangentMode", model.tangentMode);
            result += SerializeMember(serialized, null, "inTangent", model.inTangent);
            result += SerializeMember(serialized, null, "outTangent", model.outTangent);

            return result;
        }

        protected override fsResult DoDeserialize(Dictionary<string, fsData> data, ref Keyframe model)
        {
            fsResult result = fsResult.Success;

            float t0 = model.time;
            result += DeserializeMember(data, null, "time", out t0);
            model.time = t0;

            float t1 = model.value;
            result += DeserializeMember(data, null, "value", out t1);
            model.value = t1;

            int t2 = model.tangentMode;
            result += DeserializeMember(data, null, "tangentMode", out t2);
            model.tangentMode = t2;

            float t3 = model.inTangent;
            result += DeserializeMember(data, null, "inTangent", out t3);
            model.inTangent = t3;

            float t4 = model.outTangent;
            result += DeserializeMember(data, null, "outTangent", out t4);
            model.outTangent = t4;

            return result;
        }

        public override object CreateInstance(fsData data, Type storageType)
        {
            return new Keyframe();
        }
    }
}
#endif

#pragma warning restore 612, 618