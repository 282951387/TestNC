using System;
using System.Reflection;

namespace ParadoxNotion.Serialization.FullSerializer
{
    /// A field on a MetaType.
    public class fsMetaProperty
    {

        /// Internal handle to the reflected member.
        public FieldInfo Field { get; private set; }
        /// The serialized name of the property, as it should appear in JSON.
        public string JsonName { get; private set; }
        /// The type of value that is stored inside of the property.
        public Type StorageType { get { return Field.FieldType; } }
        /// The real name of the member info.
        public string MemberName { get { return Field.Name; } }
        /// Is the property read only?
        public bool ReadOnly { get; private set; }
        /// Is the property write only?
        public bool WriteOnly { get; private set; }
        /// Make instance automatically?
        public bool AutoInstance { get; private set; }
        /// Serialize as reference?
        public bool AsReference { get; private set; }

        internal fsMetaProperty(FieldInfo field)
        {
            Field = field;
            fsSerializeAsAttribute attr = Field.RTGetAttribute<fsSerializeAsAttribute>(true);
            JsonName = attr != null && !string.IsNullOrEmpty(attr.Name) ? attr.Name : field.Name;
            ReadOnly = Field.RTIsDefined<fsReadOnlyAttribute>(true);
            WriteOnly = Field.RTIsDefined<fsWriteOnlyAttribute>(true);
            fsAutoInstance autoInstanceAtt = StorageType.RTGetAttribute<fsAutoInstance>(true);
            AutoInstance = autoInstanceAtt != null && autoInstanceAtt.makeInstance && !StorageType.IsAbstract;
            AsReference = Field.RTIsDefined<fsSerializeAsReference>(true);
        }

        /// Reads a value from the property that this MetaProperty represents, using the given
        /// object instance as the context.
        public object Read(object context)
        {
            return Field.GetValue(context);
        }

        /// Writes a value to the property that this MetaProperty represents, using given object
        /// instance as the context.
        public void Write(object context, object value)
        {
            Field.SetValue(context, value);
        }
    }
}