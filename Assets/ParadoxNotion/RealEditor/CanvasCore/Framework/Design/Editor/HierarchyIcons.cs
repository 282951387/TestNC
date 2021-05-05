#if UNITY_EDITOR

using NodeCanvas.Framework;
using UnityEditor;
using UnityEngine;

namespace NodeCanvas.Editor
{

    [UnityEditor.InitializeOnLoad]
    internal static class HierarchyIcons
    {
        static HierarchyIcons()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= ShowIcon;
            EditorApplication.hierarchyWindowItemOnGUI += ShowIcon;
        }

        private static void ShowIcon(int ID, Rect r)
        {
            if (!Prefs.showHierarchyIcons)
            {
                return;
            }
            GameObject go = EditorUtility.InstanceIDToObject(ID) as GameObject;
            if (go == null)
            {
                return;
            }

            GraphOwner owner = go.GetComponent<GraphOwner>();
            if (owner == null)
            {
                return;
            }

            r.xMin = r.xMax - 16;
            GUI.DrawTexture(r, StyleSheet.canvasIcon);
        }
    }
}

#endif