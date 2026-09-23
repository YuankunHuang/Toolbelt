using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Toolbelt.QuickSelector
{
    public enum ActiveState
    {
        Any,
        Active,
        Inactive,
    }

    public class QuickSelectorResult
    {
        public GameObject gameObject;
        public string path;
    }

    /// <summary>
    /// What Quick Selector looks for. Unset criteria match everything.
    /// </summary>
    public class QuickSelectorFilter
    {
        private string _name = "";
        private Regex _nameRegex;

        /// <summary>
        /// Case-insensitive. With * or ? it is a wildcard pattern for the whole name; without, any part of the name.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value?.Trim() ?? "";
                _nameRegex = BuildNameRegex(_name);
            }
        }

        /// <summary> Null or empty: any tag. </summary>
        public string Tag { get; set; }

        /// <summary> -1: any layer. </summary>
        public int Layer { get; set; } = -1;

        public ActiveState ActiveState { get; set; } = ActiveState.Any;

        /// <summary> Null: any. Subclasses match too (Collider finds BoxCollider). </summary>
        public Type ComponentType { get; set; }

        public bool Matches(GameObject go)
        {
            if (go == null)
            {
                return false;
            }

            if (_nameRegex != null && !_nameRegex.IsMatch(go.name))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(Tag) && !go.CompareTag(Tag))
            {
                return false;
            }

            if (Layer >= 0 && go.layer != Layer)
            {
                return false;
            }

            if (ActiveState == ActiveState.Active && !go.activeInHierarchy ||
                ActiveState == ActiveState.Inactive && go.activeInHierarchy)
            {
                return false;
            }

            // TryGetComponent also copes with missing scripts, which GetComponents reports as null entries.
            if (ComponentType != null && !go.TryGetComponent(ComponentType, out _))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Matching objects in the open prefab (prefab mode) or in all loaded scenes, inactive ones included,
        /// sorted by hierarchy path.
        /// </summary>
        public List<QuickSelectorResult> Find()
        {
            var results = new List<QuickSelectorResult>();
            foreach (var go in EnumerateHierarchy())
            {
                if (Matches(go))
                {
                    results.Add(new QuickSelectorResult { gameObject = go, path = GetHierarchyPath(go) });
                }
            }

            results.Sort((a, b) => string.CompareOrdinal(a.path, b.path));
            return results;
        }

        public static IEnumerable<GameObject> EnumerateHierarchy()
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.prefabContentsRoot != null)
            {
                foreach (var go in Traverse(prefabStage.prefabContentsRoot))
                {
                    yield return go;
                }
                yield break;
            }

            for (var i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var go in Traverse(root))
                    {
                        yield return go;
                    }
                }
            }
        }

        public static string GetHierarchyPath(GameObject go)
        {
            if (go == null)
            {
                return "<null>";
            }

            var names = new Stack<string>();
            for (var t = go.transform; t != null; t = t.parent)
            {
                names.Push(t.name);
            }

            var sceneName = go.scene.IsValid() ? go.scene.name : "(NoScene)";
            return $"{sceneName}/{string.Join("/", names)}";
        }

        private static IEnumerable<GameObject> Traverse(GameObject root)
        {
            var stack = new Stack<Transform>();
            stack.Push(root.transform);
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                yield return t.gameObject;
                for (var i = t.childCount - 1; i >= 0; --i)
                {
                    stack.Push(t.GetChild(i));
                }
            }
        }

        private static Regex BuildNameRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern) || pattern == "*")
            {
                return null;
            }

            var escaped = Regex.Escape(pattern);
            var hasWildcard = pattern.IndexOf('*') >= 0 || pattern.IndexOf('?') >= 0;
            var regex = hasWildcard
                ? "^" + escaped.Replace("\\*", ".*").Replace("\\?", ".") + "$"
                : escaped;
            return new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }
    }
}
