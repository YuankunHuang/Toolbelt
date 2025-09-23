using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace YuankunHuang.Tools.Core
{
    public static class ProjectSetup
    {
        [InitializeOnLoadMethod]
        static void OnProjectLoadedInEditor()
        {
            // first setup
            if (!EditorPrefs.GetBool("CoreTooltip_Initialized", false))
            {
                EditorPrefs.SetBool("CoreTooltip_Initialized", true);
                Setup();
            }
        }

        [MenuItem("Tools/Setup/Optimize For Tool Development")]
        private static void Setup()
        {
            Debug.Log($"Optimizing project for tools development...");

            try
            {
                AssetDatabase.StartAssetEditing();

                // 1. Clean up unused default assets
                CleanUpDefaultAssets();

                // 2. Optimize Editor settings
                OptimizeEditorSettings();

                // 3. Set up fast compilation
                SetFastCompilation();

                // 4. Set up project structure
                InitializeProjectStructure();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private static void CleanUpDefaultAssets()
        {
            // delete sample scene
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene.unity"))
            {
                AssetDatabase.DeleteAsset("Assets/Scenes");
            }

            // clean unnecessary folders
            string[] unnecessaryFolders = new string[]
            {
                "Assets/TutorialInfo",
                "Assets/ExampleAssets",
                "Assets/Samples",
            };

            foreach (var folder in unnecessaryFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    AssetDatabase.DeleteAsset(folder);
                }
            }
        }

        private static void OptimizeEditorSettings()
        {
            EditorPrefs.SetBool("kAutoRefresh", false);
            EditorPrefs.SetBool("AutoRunPlayer", false);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

            EditorSettings.serializationMode = SerializationMode.ForceText;
#pragma warning disable CS0618 // Type or member is obsolete
            EditorSettings.externalVersionControl = "Visible Meta Files";
#pragma warning restore CS0618 // Type or member is obsolete

#if UNITY_2019_3_OR_NEWER
            EditorSettings.asyncShaderCompilation = true;
#endif

#if UNITY_2020_2_OR_NEWER
            PlayerSettings.gcIncremental = true;
#elif UNITY_2019_3_OR_NEWER
            EditorSettings.gcIncremental = true;
#endif
        }

        private static void SetFastCompilation()
        {
#if UNITY_2019_3_OR_NEWER
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions =
                EnterPlayModeOptions.DisableDomainReload |
                EnterPlayModeOptions.DisableSceneReload;
#endif
        }

        private static void InitializeProjectStructure()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Tests");

            EnsureFolder("Assets/Tools");
            EnsureFolder("Assets/Tools/Runtime");
            EnsureFolder("Assets/Tools/Editor");
            EnsureFolder("Assets/Plugins");
            EnsureFolder("Assets/Plugins/Editor");

            CreateAsmdefIfNotExists(
                "Assets/Tools/Runtime/YuankunHuang.Tools.Runtime.asmdef",
                AsmdefJson(
                    name: "YuankunHuang.Tools.Runtime",
                    includePlatforms: null,
                    references: new string[] { }));

            CreateAsmdefIfNotExists(
                "Assets/Tools/Editor/YuankunHuang.Tools.Editor.asmdef",
                AsmdefJson(
                    name: "YuankunHuang.Tools.Editor",
                    includePlatforms: new[] { "Editor" },
                    references: new[]
                    {
                        "UnityEditor",
                        "YuankunHuang.Tools.Runtime"
                    }));

            CreateAsmdefIfNotExists(
                "Assets/Plugins/Editor/Plugins.Editor.asmdef",
                AsmdefJson(
                    name: "Plugins.Editor",
                    includePlatforms: new[] { "Editor" },
                    references: new string[] { }));

            CreateTextAssetIfNotExists("Assets/Settings/README.txt",
@"Contains all ProjectSettings, Serializable Assets");

            const string exampleTool = "Assets/Tools/Editor/ExampleToolWindow.cs";
            if (!File.Exists(exampleTool))
            {
                File.WriteAllText(exampleTool, ExampleEditorWindowCode(), new UTF8Encoding(false));
                AssetDatabase.ImportAsset(exampleTool);
            }
        }

        #region Helpers
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            { 
                return; 
            }

            var parts = path.Replace("\\", "/").Split('/');
            var accum = parts[0];
            for (var i = 1; i < parts.Length; ++i)
            {
                var next = $"{accum}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(accum, parts[i]);
                }
                accum = next;
            }
        }

        private static void CreateAsmdefIfNotExists(string path, string json)
        {
            if (File.Exists(path))
            {
                return;
            }

            File.WriteAllText(path, json, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
        }

        private static void CreateTextAssetIfNotExists(string path, string content)
        {
            if (File.Exists(path))
            {
                return;
            }

            File.WriteAllText(path, content, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path);
        }

        private static string AsmdefJson(string name, string[] includePlatforms, string[] references)
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.AppendFormat("  \"name\": \"{0}\",\n", name);

            // references
            sb.Append("  \"references\": [");
            if (references != null && references.Length > 0)
            {
                for (int i = 0; i < references.Length; i++)
                {
                    sb.Append("\"").Append(references[i]).Append("\"");
                    if (i < references.Length - 1) sb.Append(", ");
                }
            }
            sb.Append("],\n");

            // includePlatforms£¨null => all-platforms£©
            if (includePlatforms != null && includePlatforms.Length > 0)
            {
                sb.Append("  \"includePlatforms\": [");
                for (int i = 0; i < includePlatforms.Length; i++)
                {
                    sb.Append("\"").Append(includePlatforms[i]).Append("\"");
                    if (i < includePlatforms.Length - 1) sb.Append(", ");
                }
                sb.Append("],\n");
            }

            sb.Append("  \"allowUnsafeCode\": false,\n");
            sb.Append("  \"overrideReferences\": false,\n");
            sb.Append("  \"precompiledReferences\": [],\n");
            sb.Append("  \"autoReferenced\": true,\n");
            sb.Append("  \"defineConstraints\": [],\n");
            sb.Append("  \"noEngineReferences\": false\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        private static string ExampleEditorWindowCode()
        {
            return
@"using UnityEditor;
using UnityEngine;

namespace YuankunHuang.Tools.Core
{
    public class ExampleToolWindow : EditorWindow
    {
        [MenuItem(""Tools/Example Tool"")]
        private static void Open()
        {
            var win = GetWindow<ExampleToolWindow>(""Example Tool"");
            win.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label(""Hello Tools!"", EditorStyles.boldLabel);
            if (GUILayout.Button(""Ping ProjectSettings""))
            {
                EditorApplication.ExecuteMenuItem(""Edit/Project Settings..."");
            }
        }
    }
}";
        }
        #endregion
    }
}