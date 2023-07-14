using Core.Scripts.Tools;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Core.Scripts.Editor
{
    public class GridToolEditor : OdinMenuEditorWindow
    {
        [MenuItem("Window/GridTool/GridToolEditor")]
        public static GridToolEditor GetOrCreateWindow()
        {
            var window = EditorWindow.GetWindow<GridToolEditor>();
            window.titleContent = new GUIContent("GridToolEditor");
            return window;
        }
        
        protected override OdinMenuTree BuildMenuTree()
        {
            OdinMenuTree tree = new OdinMenuTree(supportsMultiSelect: true);
            var prefabsGuids = AssetDatabase.FindAssets("t:ScriptableObject", new [] { "Assets/Core/Scripts/Tools" });
            Debug.Log(prefabsGuids.Length);
        
            string guid = prefabsGuids[0];
            var path = AssetDatabase.GUIDToAssetPath(guid);
            GridCreatorTool go = AssetDatabase.LoadAssetAtPath<GridCreatorTool>(path);
            tree.Add("Assets/Core/Scripts/Tools/GridTool.asset", go);
            return tree;
        }
    }
}
