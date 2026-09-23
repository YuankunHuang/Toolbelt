using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Toolbelt.ObjectFavorites
{
    public class ObjectFavoritesWindow : EditorWindow
    {
        [MenuItem("Tools/Toolbelt/Object Favorites #%&w")]
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
            public List<Object> objects;

            public Page(string name)
            {
                this.name = name;
                objects = new List<Object>();
            }
        }

        #region Fields
        private List<Page> _pages = new List<Page>();
        private int _selectedPageIdx = -1;
        private ListView _pageList;
        private ListView _objectList;
        private ToolbarSearchField _search;
        private VisualElement _rightWrap;
        // Rows of the object list: indices into the current page's objects (a search shows a subset).
        private readonly List<int> _visibleIndices = new List<int>();
        #endregion

        #region Lifecycle
        private void CreateGUI()
        {
            LoadFromStore();

            // top toolbar
            var toolbar = new Toolbar();
            toolbar.style.alignItems = Align.Center;
            var btnAddPage = new ToolbarButton(() =>
            {
                _pages.Add(new Page("New Page"));
                _pageList.Rebuild();
                _pageList.SetSelection(_pages.Count - 1);
                SaveToStore();
            })
            { text = "+", style = { paddingLeft = 10, paddingRight = 10 } };
            var btnRemovePage = new ToolbarButton(() =>
            {
                if (_selectedPageIdx >= 0 && _selectedPageIdx < _pages.Count)
                {
                    _pages.RemoveAt(_selectedPageIdx);
                    _selectedPageIdx = -1;
                    _pageList.Rebuild();
                    RefreshObjectList();
                    SaveToStore();
                }
            })
            { text = "-", style = { paddingLeft = 10, paddingRight = 10 } };

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
                    SaveToStore();
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
            _pageList.selectionChanged += _ =>
            {
                _selectedPageIdx = _pageList.selectedIndex;
                RefreshObjectList();
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
                SaveToStore();
            });

            split.Add(_pageList);

            // objects
            _objectList = new ListView()
            {
                itemsSource = _visibleIndices,
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
                        var p = ValidPage();
                        var objIdx = (int)row.userData;
                        if (p != null && objIdx >= 0 && objIdx < p.objects.Count)
                        {
                            p.objects[objIdx] = ev?.newValue;
                            SaveToStore();
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
                        var p = ValidPage();
                        var objIdx = (int)row.userData;
                        if (p != null && objIdx >= 0 && objIdx < p.objects.Count)
                        {
                            p.objects.RemoveAt(objIdx);
                            RefreshObjectList();
                            SaveToStore();
                        }
                    };

                    return row;
                },
                bindItem = (ve, i) =>
                {
                    // Rows address the page's list, not the (possibly filtered) row index.
                    var objIdx = _visibleIndices[i];
                    ve.userData = objIdx;
                    var page = ValidPage();
                    var objField = ve.Q<ObjectField>();
                    objField.SetValueWithoutNotify(page != null && objIdx < page.objects.Count ? page.objects[objIdx] : null);
                }
            };

            _objectList.pickingMode = PickingMode.Position; // ensure trigger

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

            // drag&drop onto the right side adds to the current page
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
                if (!_rightWrap.worldBound.Contains(e.mousePosition))
                    return;

                var p = ValidPage();
                if (p == null)
                    return;

                DragAndDrop.AcceptDrag();
                var set = new HashSet<Object>(p.objects);
                var added = false;
                foreach (var o in DragAndDrop.objectReferences)
                {
                    // avoid duplicate
                    if (set.Add(o))
                    {
                        p.objects.Add(o);
                        added = true;
                    }
                }

                if (added)
                {
                    RefreshObjectList();
                    _objectList.ScrollToItem(_visibleIndices.Count - 1);
                    SaveToStore();
                }
                e.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);

            if (_pages.Count > 0)
            {
                _pageList.SetSelection(0); // select the 1st page on created by default
            }
        }
        #endregion

        private void LoadFromStore()
        {
            _pages.Clear();
            foreach (var p in ObjectFavoritesStore.instance.pages)
            {
                var page = new Page(p.name);
                foreach (var id in p.globalObjIds)
                {
                    page.objects.Add(ObjectFavoritesStore.ConvertGlobalIdToObject(id));
                }
                _pages.Add(page);
            }
        }

        private void SaveToStore()
        {
            var store = ObjectFavoritesStore.instance;
            store.pages.Clear();
            foreach (var p in _pages)
            {
                var pd = new ObjectFavoritesStore.PageData() { name = p.name };
                foreach (var obj in p.objects)
                {
                    pd.globalObjIds.Add(ObjectFavoritesStore.ConvertObjectToGlobalId(obj));
                }
                store.pages.Add(pd);
            }
            store.Save();
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
                _visibleIndices.Clear();
            }
            else
            {
                ObjectFavoritesStore.GetVisibleIndices(p.objects, _search?.value, _visibleIndices);
            }

            _objectList.Rebuild();
        }
    }
}
