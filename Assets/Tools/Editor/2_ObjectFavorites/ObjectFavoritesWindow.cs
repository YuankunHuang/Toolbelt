using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace YuankunHuang.Tools.Editor
{
    public class ObjectFavoritesWindow : EditorWindow
    {
        [MenuItem("Tools/2_ObjectFavorites #%&w")]
        public static void ShowWindow()
        {
            var window = GetWindow<ObjectFavoritesWindow>();
            window.titleContent = new GUIContent("Favorites");
            window.Show();
        }

        [System.Serializable]
        private class Page
        {
            public string name;
            public List<UnityEngine.Object> objects;

            public Page(string name)
            {
                this.name = name;
                objects = new List<UnityEngine.Object>();
            }
        }

        #region Fields
        private ObjectFavoritesConfig _config;
        private List<Page> _pages = new List<Page>();
        private int _selectedPageIdx = -1;
        private ListView _pageList;
        private ListView _objectList;
        private ToolbarSearchField _search;
        private VisualElement _rightWrap;
        #endregion

        #region Lifecycle
        private void CreateGUI()
        {
            _config = ObjectFavoritesConfig.LoadOrCreate();

            LoadFromConfig();

            // top toolbar
            var toolbar = new Toolbar();
            toolbar.style.alignItems = Align.Center;
            var btnAddPage = new ToolbarButton(() =>
            {
                _pages.Add(new Page("New Page"));
                _pageList.Rebuild();
                _pageList.SetSelection(_pages.Count - 1);
                SaveToConfig();
            })
            { text = "+", style = { paddingLeft = 10, paddingRight = 10 } };
            var btnRemovePage = new ToolbarButton(() =>
            {
                if (_selectedPageIdx >= 0 && _selectedPageIdx < _pages.Count)
                {
                    _pages.RemoveAt(_selectedPageIdx);
                    _selectedPageIdx = -1;
                    _pageList.Rebuild();
                    _objectList.itemsSource = null;
                    _objectList.Rebuild();
                    SaveToConfig();
                }
            })
            { text = "-", style = {paddingLeft = 10, paddingRight = 10 } };

            _search = new ToolbarSearchField();
            _search.style.alignSelf = Align.Center;
            _search.RegisterValueChangedCallback(_ => RefreshObjectList());

            toolbar.Add(btnAddPage);
            toolbar.Add(btnRemovePage);
            toolbar.Add(new ToolbarSpacer());
            toolbar.Add(new Label("Search:"));
            toolbar.Add(_search);

            toolbar.style.paddingLeft = toolbar.style.paddingRight = 6;
            toolbar.style.paddingTop = toolbar.style.paddingBottom = 2;
            foreach (var c in toolbar.Children())
            {
                c.style.marginLeft = c.style.marginRight = 4;
            }
            rootVisualElement.Add(toolbar);

            var split = new TwoPaneSplitView(0, position.width * .3f, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(split);

            // pages
            _pageList = new ListView(_pages, itemHeight: 24, makeItem: () =>
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                row.style.alignItems = Align.Center;

                var label = new Label();
                label.style.display = DisplayStyle.Flex;
                label.style.flexGrow = 1;
                label.style.unityTextAlign = TextAnchor.MiddleCenter;

                var textField = new TextField();
                textField.style.display = DisplayStyle.None;
                textField.style.flexGrow = 1;
                textField.RegisterCallback<AttachToPanelEvent>(_ =>
                {
                    var input = textField.Q(TextField.textInputUssName);
                    if (input != null)
                    {
                        input.style.unityTextAlign = TextAnchor.MiddleCenter;
                    }
                });

                row.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.clickCount == 2)
                    {
                        label.style.display = DisplayStyle.None;
                        textField.style.display = DisplayStyle.Flex;
                        textField.Focus();
                        textField.SelectAll();
                    }
                });

                textField.RegisterCallback<FocusOutEvent>(e =>
                {
                    var idx = (int)row.userData;
                    var p = _pages[idx];
                    p.name = textField.text;
                    label.text = p.name;

                    label.style.display = DisplayStyle.Flex;
                    textField.style.display = DisplayStyle.None;
                    SaveToConfig();
                });

                row.Add(label);
                row.Add(textField);
                return row;
            }, bindItem: (ve, i) =>
            {
                ve.userData = i;
                var page = _pages[i];
                var label = ve.Q<Label>();
                var tf = ve.Q<TextField>();
                label.text = page.name;
                tf.SetValueWithoutNotify(page.name);
                label.style.display = DisplayStyle.Flex;
                tf.style.display = DisplayStyle.None;
            });
            _pageList.selectionType = SelectionType.Single;
            _pageList.selectionChanged += sel =>
            {
                foreach (var s in sel)
                {
                    _selectedPageIdx = _pageList.selectedIndex;
                    RefreshObjectList();
                    break;
                }
            };
            _pageList.RegisterCallback<DragUpdatedEvent>(e =>
            {
                if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    e.StopPropagation();
                }
            });
            _pageList.RegisterCallback<DragPerformEvent>(e =>
            {
                var newPage = new Page("Dropped");
                newPage.objects.AddRange(DragAndDrop.objectReferences);
                _pages.Add(newPage);
                _pageList.Rebuild();
                _pageList.SetSelection(_pages.Count - 1);
                e.StopPropagation();
                SaveToConfig();
            });

            split.Add(_pageList);

            // objects
            _objectList = new ListView()
            {
                makeItem = () =>
                {
                    var row = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                    var objField = new ObjectField { allowSceneObjects = true };
                    objField.style.flexGrow = 1;
                    var btnRemove = new Button() { text = "X" };
                    row.Add(objField);
                    row.Add(btnRemove);

                    objField.RegisterValueChangedCallback(ev =>
                    {
                        var i = (int)row.userData;
                        var p = ValidPage();
                        if (p != null && i >= 0 && i < p.objects.Count)
                        {
                            p.objects[i] = ev?.newValue;
                            SaveToConfig();
                        }
                    });
                    objField.RegisterCallback<MouseDownEvent>(e =>
                    {
                        if (objField.value != null)
                        {
                            EditorGUIUtility.PingObject(objField.value);
                        }
                    });
                    btnRemove.clicked += () =>
                    {
                        var i = (int)row.userData;
                        var p = ValidPage();
                        if (p != null && i >= 0 && i < p.objects.Count)
                        {
                            p.objects.RemoveAt(i);
                            RefreshObjectList();
                            SaveToConfig();
                        }
                    };

                    return row;
                },
                bindItem = (ve, i) =>
                {
                    ve.userData = i;
                    var page = ValidPage();
                    var objField = ve.Q<ObjectField>();
                    objField.SetValueWithoutNotify(page != null && i < page.objects.Count ? page.objects[i] : null);
                }
            };

            _objectList.pickingMode = PickingMode.Position; // ensure trigger

            // drag&drop - add to current page (otherwise do nothing)
            _objectList.RegisterCallback<DragUpdatedEvent>(e =>
            {
                if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0 && ValidPage() != null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                }
            }, TrickleDown.TrickleDown);
            _objectList.RegisterCallback<DragPerformEvent>(e =>
            {
                var p = ValidPage();
                if (p != null)
                {
                    var set = new HashSet<Object>(p.objects);
                    // avoid duplicate
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (set.Add(obj))
                        {
                            p.objects.Add(obj);
                        }
                    }
                    RefreshObjectList();
                    SaveToConfig();
                }
            }, TrickleDown.TrickleDown);

            // add a tool bar to the right
            _rightWrap = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column } };
            var rightToolbar = new Toolbar();
            var btnAddRight = new ToolbarButton(() =>
            {
                var p = ValidPage();
                if (p != null)
                {
                    p.objects.Add(null);
                    RefreshObjectList();
                }
            })
            { text = "+" };
            rightToolbar.Add(btnAddRight);
            _rightWrap.Add(rightToolbar);
            _rightWrap.Add(_objectList);
            split.Add(_rightWrap);

            _rightWrap.pickingMode = PickingMode.Position;
            _rightWrap.RegisterCallback<DragUpdatedEvent>(e =>
            {
                if (ValidPage() != null &&
                    DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0 &&
                    _rightWrap.worldBound.Contains(e.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    e.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);
            _rightWrap.RegisterCallback<DragPerformEvent>(e =>
            {
                if (!(_rightWrap.worldBound.Contains(e.mousePosition)))
                    return;

                var p = ValidPage(); 
                if (p == null) 
                    return;

                DragAndDrop.AcceptDrag();
                var set = new HashSet<Object>(p.objects);
                var addedIndex = -1;
                foreach (var o in DragAndDrop.objectReferences)
                {
                    if (set.Add(o))
                    {
                        p.objects.Add(o);
                        addedIndex = p.objects.Count - 1;
                    }
                }

                if (addedIndex >= 0)
                {
                    RefreshObjectList();
                    _objectList.ScrollToItem(addedIndex);
                    SaveToConfig();
                }
                e.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);

            if (_pages.Count > 0)
            {
                _pageList.SetSelection(0); // select the 1st page on created by default
            }
        }

        private void LoadFromConfig()
        {
            _pages.Clear();
            if (_config == null || _config.pages == null || _config.pages.Count == 0)
            {
                return;
            }

            foreach (var p in _config.pages)
            {
                var page = new Page(p.name);
                foreach (var id in p.globalObjIds)
                {
                    var obj = ObjectFavoritesConfig.ConvertGlobalIdToObject(id);
                    page.objects.Add(obj);
                }
                _pages.Add(page);
            }
        }

        private void SaveToConfig()
        {
            if (_config == null)
            {
                return;
            }

            _config.pages.Clear();
            foreach (var p in _pages)
            {
                var pd = new ObjectFavoritesConfig.PageData() { name = p.name };
                foreach (var obj in p.objects)
                {
                    pd.globalObjIds.Add(ObjectFavoritesConfig.ConvertObjectToGlobalId(obj));
                }
                _config.pages.Add(pd);
            }

            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
        }

        private Page ValidPage()
        {
            if (_selectedPageIdx >= 0 && _selectedPageIdx < _pages.Count)
            {
                return _pages[_selectedPageIdx];
            }

            return null;
        }

        private void RefreshObjectList()
        {
            var p = ValidPage();
            if (p == null)
            {
                _objectList.itemsSource = null;
                _objectList.Rebuild();
                return;
            }

            var keyword = _search?.value?.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                _objectList.itemsSource = p.objects;
            }
            else
            {
                _objectList.itemsSource = p.objects.FindAll(o => o != null && MatchWildcard(o.name, keyword));
            }

            _objectList.Rebuild();
        }

        private bool MatchWildcard(string text, string pattern)
        {
            if (string.IsNullOrEmpty(pattern) || pattern == "*")
            {
                return true;
            }

            var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".")
                + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        #endregion
    }
}