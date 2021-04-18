#if UNITY_EDITOR

using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using UnityEngine;

namespace NodeCanvas.Editor
{

    public class BBParameterDrawer : ObjectDrawer<BBParameter>
    {
        public override BBParameter OnGUI(GUIContent content, BBParameter instance)
        {
            bool required = fieldInfo.RTIsDefined<RequiredFieldAttribute>(true);
            bool bbOnly = fieldInfo.RTIsDefined<BlackboardOnlyAttribute>(true);
            instance = BBParameterEditor.ParameterField(content, instance, bbOnly, required, info);
            return instance;
        }
    }
}

#endif