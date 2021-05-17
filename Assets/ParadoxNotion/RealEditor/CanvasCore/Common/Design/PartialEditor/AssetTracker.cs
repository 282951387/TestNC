#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace ParadoxNotion.Design
{

    ///Can track assets of specific type when required. This is faster than requesting AssetDabase all the time and can also be used in separate thread.
    public class AssetTracker : AssetPostprocessor
    {
        public static event System.Action<string[]> onAssetsImported;
        public static event System.Action<string[]> onAssetsDeleted;
        public static event System.Action<string[], string[]> onAssetsMoved;

        public static Dictionary<string, UnityEngine.Object> trackedAssets { get; private set; }
        public static List<System.Type> trackedTypes { get; private set; }

        ///Call this to start tracking assets of specified type (and assignables to that)
        public static void BeginTrackingAssetsOfType(System.Type type)
        {
            return;

            if (trackedAssets == null) { trackedAssets = new Dictionary<string, UnityEngine.Object>(); }
            if (trackedTypes == null) { trackedTypes = new List<System.Type>(); }

            if (trackedTypes.Contains(type))
            {
                UnityEngine.Debug.LogError("Asset type is already tracked: " + type);
                return;
            }

            trackedTypes.Add(type);

            //we need to immediately fetch them here now
            string[] assetGUIDS = AssetDatabase.FindAssets(string.Format("t:{0}", type.Name));
            foreach (string guid in assetGUIDS)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, type);
                trackedAssets[path] = asset;
            }
        }

        //unity callback
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            return;

            AssetsImported(importedAssets);
            if (onAssetsImported != null) { onAssetsImported(importedAssets); }

            AssetsDeleted(deletedAssets);
            if (onAssetsDeleted != null) { onAssetsDeleted(deletedAssets); }

            AssetsMoved(movedAssets, movedFromAssetPaths);
            if (onAssetsMoved != null) { onAssetsMoved(movedAssets, movedFromAssetPaths); }

        }

        //..
        private static void AssetsImported(string[] paths)
        {
            if (trackedTypes == null) { return; }
            foreach (string path in paths)
            {
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
                if (asset != null && trackedTypes.Any(t => t.IsAssignableFrom(asset.GetType())))
                {
                    trackedAssets[path] = asset;
                }
            }
        }

        //..
        private static void AssetsDeleted(string[] paths)
        {
            if (trackedTypes == null) { return; }
            foreach (string path in paths)
            {
                if (trackedAssets.ContainsKey(path))
                {
                    trackedAssets.Remove(path);
                }
            }
        }

        //..
        private static void AssetsMoved(string[] moveToPaths, string[] moveFromPaths)
        {
            if (trackedTypes == null) { return; }
            AssetsDeleted(moveFromPaths);
            AssetsImported(moveToPaths);
        }


    }
}

#endif