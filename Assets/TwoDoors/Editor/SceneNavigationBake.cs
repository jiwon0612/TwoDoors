using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace TwoDoors.Editor
{
    public static class SceneNavigationBake
    {
        // NavMeshSurface.OnValidate clears data shared by separate scene surfaces.
        // Always persist a separate asset when a scene is copied or rebuilt.
        public static void Save(NavMeshSurface surface, string path)
        {
            surface.BuildNavMesh();
            var baked = surface.navMeshData;
            if (!baked) throw new System.InvalidOperationException("Navigation bake failed: " + surface.name);
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if (existing)
            {
                surface.RemoveData();
                EditorUtility.CopySerialized(baked, existing);
                EditorUtility.SetDirty(existing);
                surface.navMeshData = existing;
                surface.AddData();
                Object.DestroyImmediate(baked);
            }
            else AssetDatabase.CreateAsset(baked, path);
            EditorUtility.SetDirty(surface);
        }
    }
}
