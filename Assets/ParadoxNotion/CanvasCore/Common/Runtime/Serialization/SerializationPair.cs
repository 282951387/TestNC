using System.Collections.Generic;

namespace ParadoxNotion.Serialization
{
    [System.Serializable]
    ///A pair of JSON and UnityObject references
    public sealed class SerializationPair
    {
        public string _json;
        public List<UnityEngine.Object> _references;
        public SerializationPair() { _references = new List<UnityEngine.Object>(); }
    }
}