using UnityEditor;
using UnityEngine;

namespace YuankunHuang.Tools.Core
{
    public class ExampleToolWindow : EditorWindow
    {
        [MenuItem("Tools/Example Tool")]
        private static void Open()
        {
            var win = GetWindow<ExampleToolWindow>("Example Tool");
            win.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Hello Tools!", EditorStyles.boldLabel);
            if (GUILayout.Button("Ping ProjectSettings"))
            {
                EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
            }
        }
    }
}