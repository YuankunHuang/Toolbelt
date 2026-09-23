using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Toolbelt.QuickSelector
{
    public class QuickSelectorWindow : EditorWindow
    {
        private const string Any = "Any";

        [MenuItem("Tools/Toolbelt/Quick Selector #%&q")]
        private static void ShowWindow()
        {
            var window = GetWindow<QuickSelectorWindow>();
            window.titleContent = new GUIContent("Quick Selector");
            window.minSize = new Vector2(700, 200);
            window.Show();
        }

        private readonly QuickSelectorFilter _filter = new QuickSelectorFilter();
        private readonly Dictionary<string, Type> _componentTypeDict = new Dictionary<string, Type>();
        private List<QuickSelectorResult> _results = new List<QuickSelectorResult>();

        private ToolbarSearchField _nameField;
        private PopupField<string> _tagField;
        private PopupField<string> _layerField;
        private PopupField<string> _componentField;
        private EnumField _activeStateField;
        private ListView _listView;
        private Label _countLabel;

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = root.style.paddingRight = 6;
            root.style.paddingTop = root.style.paddingBottom = 6;
            root.style.flexDirection = FlexDirection.Column;

            var toolbar = new Toolbar();

            _nameField = new ToolbarSearchField() { value = "" };
            _nameField.tooltip = "Filter by name (case-insensitive). Plain text matches any part of the name; * and ? match the whole name.";
            _nameField.style.flexGrow = 1;
            _nameField.style.marginRight = 6;
            toolbar.Add(_nameField);

            var btnRefresh = new ToolbarButton(Refresh) { text = "Refresh" };
            btnRefresh.tooltip = "Refresh results";
            toolbar.Add(btnRefresh);

            root.Add(toolbar);

            /*
             * Filters
             */
            var filters = new VisualElement();
            filters.style.flexDirection = FlexDirection.Row;
            filters.style.flexWrap = Wrap.NoWrap;

            // tag
            var tags = new List<string>() { Any };
            tags.AddRange(UnityEditorInternal.InternalEditorUtility.tags);
            _tagField = new PopupField<string>(tags, 0);
            AddFilter(filters, "Tag: ", _tagField);

            // layer
            var layers = new List<string>() { Any };
            layers.AddRange(Enumerable.Range(0, 32).Select(LayerMask.LayerToName).Where(x => !string.IsNullOrEmpty(x)));
            _layerField = new PopupField<string>(layers, 0);
            AddFilter(filters, "Layer: ", _layerField);

            // active state
            _activeStateField = new EnumField(ActiveState.Any);
            AddFilter(filters, "Active State: ", _activeStateField);

            // component
            _componentTypeDict.Clear();
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(Component)))
            {
                if (type.FullName != null)
                {
                    _componentTypeDict[type.FullName] = type;
                }
            }
            var components = new List<string>() { Any };
            components.AddRange(_componentTypeDict.Keys.OrderBy(x => x, StringComparer.Ordinal));
            _componentField = new PopupField<string>(components, 0);
            AddFilter(filters, "Component: ", _componentField);

            // count
            _countLabel = new Label("0 results, 0 selected") { style = { unityTextAlign = TextAnchor.MiddleRight, flexGrow = 1 } };
            filters.Add(_countLabel);

            root.Add(filters);

            /*
             * Listview
             */
            _listView = new ListView()
            {
                selectionType = SelectionType.Multiple,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                style =
                {
                    flexGrow = 1,
                    marginTop = 6,
                },
            };

            _listView.makeItem = () =>
            {
                var ve = new VisualElement();
                ve.style.flexDirection = FlexDirection.Column;
                ve.style.paddingTop = ve.style.paddingBottom = 3;

                var nameLine = new VisualElement();
                var nameLabel = new Label() { name = "name", style = { unityFontStyleAndWeight = FontStyle.Bold, flexGrow = 1 } };
                var metaLabel = new Label() { name = "meta", style = { opacity = 0.8f } };
                nameLine.Add(nameLabel);
                nameLine.Add(metaLabel);

                var pathLabel = new Label() { name = "path", style = { fontSize = 10, opacity = 0.7f } };

                ve.Add(nameLine);
                ve.Add(pathLabel);

                return ve;
            };

            _listView.bindItem = (ve, i) =>
            {
                var result = _results[i];
                var go = result.gameObject;
                ve.Q<Label>("name").text = go != null ? go.name : "<missing>";
                ve.Q<Label>("meta").text = go != null ? $"Tag: {go.tag} | Layer: {LayerMask.LayerToName(go.layer)}" : "";
                ve.Q<Label>("path").text = result.path;
            };

            _listView.itemsSource = _results;

            _listView.selectionChanged += items =>
            {
                var gos = new List<GameObject>();
                foreach (var item in items)
                {
                    if (item is QuickSelectorResult result && result.gameObject != null)
                    {
                        gos.Add(result.gameObject);
                    }
                }

                Selection.objects = gos.ToArray();
                UpdateCountLabel();
            };

            root.Add(_listView);

            _nameField.RegisterValueChangedCallback(_ => Refresh());
            _tagField.RegisterValueChangedCallback(_ => Refresh());
            _layerField.RegisterValueChangedCallback(_ => Refresh());
            _activeStateField.RegisterValueChangedCallback(_ => Refresh());
            _componentField.RegisterValueChangedCallback(_ => Refresh());
        }

        private static void AddFilter(VisualElement parent, string label, VisualElement field)
        {
            parent.Add(new Label(label) { style = { unityTextAlign = TextAnchor.MiddleCenter } });
            var wrap = new VisualElement() { style = { minWidth = 80 } };
            wrap.Add(field);
            parent.Add(wrap);
        }

        /// <summary>
        /// Called when any filter changes
        /// </summary>
        private void Refresh()
        {
            _filter.Name = _nameField?.value;
            _filter.Tag = _tagField?.value == Any ? null : _tagField?.value;
            _filter.Layer = _layerField?.value == Any || _layerField == null ? -1 : LayerMask.NameToLayer(_layerField.value);
            _filter.ActiveState = _activeStateField?.value is ActiveState state ? state : ActiveState.Any;
            _filter.ComponentType = _componentField != null && _componentTypeDict.TryGetValue(_componentField.value, out var type) ? type : null;

            _results = _filter.Find();
            _listView.itemsSource = _results;
            _listView.ClearSelection();
            Selection.objects = new UnityEngine.Object[0];
            _listView.Rebuild();

            UpdateCountLabel();
        }

        private void UpdateCountLabel()
        {
            _countLabel.text = $"{_results.Count} results, {_listView.selectedIndices.Count()} selected";
        }
    }
}
