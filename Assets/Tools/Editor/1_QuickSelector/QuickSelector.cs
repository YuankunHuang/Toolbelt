using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace YuankunHuang.Tools.Editor
{
    public class QuickSelector : EditorWindow
    {
        [System.Serializable]
        private class Row
        {
            public string name;
            public string tag;
            public string layer;
            public Component component;
        }

        public string Identifier => GetType().FullName;
        private string NameKey => $"{Identifier}_Name";
        private string ComponentKey => $"{Identifier}_Component";
        private string TagKey => $"{Identifier}_Tag";
        private string LayerKey => $"{Identifier}_Layer";
        private string ActiveKey => $"{Identifier}_Active";
        private string InactiveKey => $"{Identifier}_Inactive";

        [MenuItem("Tools/1_QuickSelector #%&q")]
        static void Open()
        {
            var window = GetWindow<QuickSelector>();
            window.titleContent = new GUIContent("Quick Selector");
            window.Show();
        }

        // filters
        private string _nameFilter = DefaultNameFilter;
        private const string DefaultNameFilter = "";
        private string _componentFilter = DefaultComponentFilter;
        private const string DefaultComponentFilter = "";
        private string _tagFilter = DefaultTagFilter;
        private const string DefaultTagFilter = "";
        private string _layerFilter = DefaultLayerFilter;
        private const string DefaultLayerFilter = "";
        private bool _activeFilter = DefaultActiveFilter;
        private const bool DefaultActiveFilter = true;
        private bool _inactiveFilter = DefaultInactiveFilter;
        private const bool DefaultInactiveFilter = false;
        private Vector2 _scrollPos;
        private List<Row> _rows = new List<Row>(); // complete row data list
        private List<Row> _view = new List<Row>(); // for optimized view

        private List<Type> _allComponentTypes;
        private List<string> _allLayers;
        private bool _isDirty = true;

        #region Lifecycle
        private void OnEnable()
        {
            _nameFilter = EditorPrefs.HasKey(NameKey)
                ? EditorPrefs.GetString(NameKey)
                : DefaultNameFilter;

            _componentFilter = EditorPrefs.HasKey(ComponentKey)
                ? EditorPrefs.GetString(ComponentKey)
                : DefaultComponentFilter;

            _tagFilter = EditorPrefs.HasKey(TagKey)
                ? EditorPrefs.GetString(TagKey)
                : DefaultTagFilter;

            _layerFilter = EditorPrefs.HasKey(LayerKey)
                ? EditorPrefs.GetString(LayerKey)
                : DefaultLayerFilter;

            _activeFilter = EditorPrefs.HasKey(ActiveKey)
                ? EditorPrefs.GetBool(ActiveKey)
                : DefaultActiveFilter;

            _inactiveFilter = EditorPrefs.HasKey(InactiveKey)
                ? EditorPrefs.GetBool(InactiveKey)
                : DefaultInactiveFilter;

            _allComponentTypes = GetAllComponentTypesInProject();
            _allLayers = GetAllLayers();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            DrawFilters();

            EditorGUILayout.Space(10);

            DrawList();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Find"))
            {
                Find();
            }

            if (GUILayout.Button("Select"))
            {
                Select();
            }

            if (GUILayout.Button("Clear"))
            {
                Clear();
            }

            if (GUILayout.Button("Reset"))
            {
                Reset();
            }

            EditorGUILayout.EndVertical();
        }
        #endregion

        private void DrawFilters()
        {
            EditorGUILayout.LabelField("Filter Conditions", EditorStyles.boldLabel);

            // name
            GUILayout.BeginHorizontal();

            GUILayout.Label("Name: ", GUILayout.Width(80));
            var name = GUILayout.TextField(_nameFilter, GUILayout.ExpandWidth(true));
            if (name != _nameFilter)
            {
                _nameFilter = name;
                EditorPrefs.SetString(NameKey, _nameFilter);
                _isDirty = true;
            }

            GUILayout.EndHorizontal();

            // components
            GUILayout.BeginHorizontal();

            GUILayout.Label("Component: ", GUILayout.Width(80));

            if (EditorGUILayout.DropdownButton(new GUIContent(string.IsNullOrEmpty(_componentFilter) ? "(Any)" : _componentFilter), FocusType.Passive, GUILayout.ExpandWidth(true)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("(Any)"), false, () =>
                {
                    if (_componentFilter != "")
                    {
                        EditorPrefs.SetString(ComponentKey, "");
                        _isDirty = true;
                    }
                    _componentFilter = "";
                });
                foreach (var componentType in _allComponentTypes)
                {
                    var t = componentType;
                    menu.AddItem(new GUIContent($"{componentType.FullName}"), false, () =>
                    {
                        if (t.FullName != _componentFilter)
                        {
                            EditorPrefs.SetString(ComponentKey, t.FullName);
                            _isDirty = true;
                        }
                        _componentFilter = componentType.FullName;
                    });
                }
                menu.ShowAsContext();
            }
            GUILayout.EndHorizontal();

            // tag
            GUILayout.BeginHorizontal();

            GUILayout.Label("Tag: ", GUILayout.Width(80));
            if (EditorGUILayout.DropdownButton(new GUIContent(string.IsNullOrEmpty(_tagFilter) ? "(Any)" : _tagFilter), FocusType.Passive, GUILayout.ExpandWidth(true)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("(Any)"), false, () =>
                {
                    if (_tagFilter != "")
                    {
                        EditorPrefs.SetString(TagKey, "");
                        _isDirty = true;
                    }
                    _tagFilter = "";
                });
                foreach (var tag in InternalEditorUtility.tags)
                {
                    menu.AddItem(new GUIContent(tag), false, () =>
                    {
                        if (tag != _tagFilter)
                        {
                            EditorPrefs.SetString(TagKey, tag);
                            _isDirty = true;
                        }

                        _tagFilter = tag;
                    });
                }
                menu.ShowAsContext();
            }

            GUILayout.EndHorizontal();

            // layer
            GUILayout.BeginHorizontal();

            GUILayout.Label("Layer: ", GUILayout.Width(100));
            if (EditorGUILayout.DropdownButton(new GUIContent(string.IsNullOrEmpty(_layerFilter) ? "(Any)" : _layerFilter), FocusType.Passive, GUILayout.ExpandWidth(true)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("(Any)"), false, () =>
                {
                    if (_layerFilter != "")
                    {
                        EditorPrefs.SetString(LayerKey, "");
                        _isDirty = true;
                    }
                    _layerFilter = "";
                });
                foreach (var layer in _allLayers)
                {
                    menu.AddItem(new GUIContent(layer), false, () =>
                    {
                        if (layer != _layerFilter)
                        {
                            EditorPrefs.SetString(LayerKey, layer);
                            _isDirty = true;
                        }

                        _layerFilter = layer;
                    });
                }
                menu.ShowAsContext();
            }

            GUILayout.EndHorizontal();

            // active
            GUILayout.BeginHorizontal();

            //GUILayout.Label("Active: ", GUILayout.Width(80));
            GUILayout.Label("ActiveInHierarchy:", GUILayout.Width(120));
            var activeFilter = GUILayout.Toggle(_activeFilter, new GUIContent("Active"), GUILayout.Width(position.width * .3f));
            if (activeFilter != _activeFilter)
            {
                _activeFilter = activeFilter;
                EditorPrefs.SetBool(ActiveKey, _activeFilter);
                _isDirty = true;
            }
            var inactiveFilter = GUILayout.Toggle(_inactiveFilter, new GUIContent("Inactive"), GUILayout.Width(position.width * 3f));
            if (inactiveFilter != _inactiveFilter)
            {
                _inactiveFilter = inactiveFilter;
                EditorPrefs.SetBool(InactiveKey, _inactiveFilter);
                _isDirty = true;
            }
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

        private void Find()
        {
            if (!_isDirty)
            {
                return;
            }

            _isDirty = false;

            var componentType = string.IsNullOrEmpty(_componentFilter) || _componentFilter == DefaultComponentFilter
                ? null
                : GetTypeWithFullName(_componentFilter);

            _rows.Clear();
            var objects = componentType != null
                ? GameObject.FindObjectsOfType(componentType, _inactiveFilter)
                : GameObject.FindObjectsOfType(typeof(Component), _inactiveFilter);

            var addedObjs = new HashSet<int>();

            foreach (var obj in objects)
            {
                var comp = obj as Component;

                // name
                if (!string.IsNullOrEmpty(_nameFilter) && !MatchesWildcard(comp.name, _nameFilter))
                {
                    continue;
                }

                // tag
                if (!string.IsNullOrEmpty(_tagFilter) && !comp.CompareTag(_tagFilter))
                {
                    continue;
                }

                // layer
                if (!string.IsNullOrEmpty(_layerFilter) && comp.gameObject.layer != LayerMask.NameToLayer(_layerFilter))
                {
                    continue;
                }

                // active & inactive
                if (comp.gameObject.activeInHierarchy ^ _activeFilter &&
                    !comp.gameObject.activeInHierarchy ^ _inactiveFilter)
                {
                    continue;
                }

                // avoid duplicate
                if (addedObjs.Add(comp.gameObject.GetInstanceID()))
                {
                    _rows.Add(new Row
                    {
                        name = comp.name,
                        component = comp,
                        tag = comp.tag,
                        layer = LayerMask.LayerToName(comp.gameObject.layer),
                    });
                }
            }
        }

        private void Select()
        {
            if (_rows.Count == 0)
            {
                Selection.objects = new UnityEngine.Object[0];
            }
            else
            {
                var objs = new UnityEngine.Object[_rows.Count];
                for (var i = 0; i < objs.Length; ++i)
                {
                    objs[i] = _rows[i].component.gameObject;
                }

                Selection.objects = objs;
            }
        }

        private void Clear()
        {
            _isDirty = true;
            _rows.Clear();
            _view.Clear();
            Selection.objects = new UnityEngine.Object[0];
        }

        private void Reset()
        {
            Clear();

            _nameFilter = DefaultNameFilter;
            EditorPrefs.SetString(NameKey, _nameFilter);

            _componentFilter = DefaultComponentFilter;
            EditorPrefs.SetString(ComponentKey, _componentFilter);

            _tagFilter = DefaultTagFilter;
            EditorPrefs.SetString(TagKey, _tagFilter);

            _layerFilter = DefaultLayerFilter;
            EditorPrefs.SetString(LayerKey, _layerFilter);

            _activeFilter = DefaultActiveFilter;
            EditorPrefs.SetBool(ActiveKey, _activeFilter);

            _inactiveFilter = DefaultInactiveFilter;
            EditorPrefs.SetBool(InactiveKey, _inactiveFilter);
        }

        private void DrawList()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // items
            if (_rows.Count > 0)
            {
                EditorGUILayout.LabelField($"Found Objects ({_rows.Count})", EditorStyles.boldLabel);
            }
            foreach (var row in _rows)
            {
                if (GUILayout.Button($"{row.name} - {row.tag} - {row.layer}"))
                {
                    EditorGUIUtility.PingObject(row.component.gameObject);
                    Selection.activeObject = row.component.gameObject;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        #region Helper
        private Type GetTypeWithFullName(string fullName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var t = assembly.GetType(fullName);
                if (t != null)
                {
                    return t;
                }
            }

            return null;
        }

        private List<Type> GetAllComponentTypesInProject()
        {
            var componentTypes = new List<Type>();
            componentTypes.AddRange(TypeCache.GetTypesDerivedFrom<Component>());
            return componentTypes;
        }

        private List<string> GetAllLayers()
        {
            var layers = new List<string>();
            for (var i = 0; i < 32; ++i)
            {
                var layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    layers.Add(layerName);
                }
            }

            return layers;
        }

        private bool MatchesWildcard(string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern)) 
                return true;

            if (pattern == "*")
                return true;

            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            var isMatch = System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return isMatch;
        }
        #endregion
    }
}