using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Toolbelt.QuickSelector.Tests
{
    public class QuickSelectorFilterTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _created.Clear();
        }

        private GameObject Create(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            else
            {
                _created.Add(go);
            }
            return go;
        }

        [Test]
        public void Name_PlainTextMatchesAnyPartIgnoringCase()
        {
            var filter = new QuickSelectorFilter { Name = "enemy" };
            Assert.IsTrue(filter.Matches(Create("BigEnemy_01")));
            Assert.IsTrue(filter.Matches(Create("Enemy")));
            Assert.IsFalse(filter.Matches(Create("Player")));
        }

        [Test]
        public void Name_WildcardsMatchTheWholeName()
        {
            var filter = new QuickSelectorFilter { Name = "Enemy_*" };
            Assert.IsTrue(filter.Matches(Create("enemy_01")));
            Assert.IsFalse(filter.Matches(Create("BigEnemy_01")));

            filter.Name = "?nemy";
            Assert.IsTrue(filter.Matches(Create("Enemy")));
            Assert.IsFalse(filter.Matches(Create("Enemy2")));
        }

        [Test]
        public void Name_RegexCharactersAreLiteral()
        {
            var filter = new QuickSelectorFilter { Name = "Cube (1)" };
            Assert.IsTrue(filter.Matches(Create("Cube (1)")));
            Assert.IsFalse(filter.Matches(Create("Cube 1")));
        }

        [Test]
        public void EmptyFilter_MatchesEverything()
        {
            var filter = new QuickSelectorFilter();
            Assert.IsTrue(filter.Matches(Create("Anything")));
            filter.Name = "*";
            Assert.IsTrue(filter.Matches(Create("Anything")));
            Assert.IsFalse(filter.Matches(null));
        }

        [Test]
        public void TagAndLayer()
        {
            var player = Create("A");
            player.tag = "Player";
            player.layer = 5;
            var plain = Create("B");

            Assert.IsTrue(new QuickSelectorFilter { Tag = "Player" }.Matches(player));
            Assert.IsFalse(new QuickSelectorFilter { Tag = "Player" }.Matches(plain));
            Assert.IsTrue(new QuickSelectorFilter { Layer = 5 }.Matches(player));
            Assert.IsFalse(new QuickSelectorFilter { Layer = 5 }.Matches(plain));
        }

        [Test]
        public void ActiveState_UsesActiveInHierarchy()
        {
            var parent = Create("Parent");
            var child = Create("Child", parent.transform);
            parent.SetActive(false);

            Assert.IsTrue(new QuickSelectorFilter { ActiveState = ActiveState.Inactive }.Matches(child), "active self, inactive parent");
            Assert.IsFalse(new QuickSelectorFilter { ActiveState = ActiveState.Active }.Matches(child));

            parent.SetActive(true);
            Assert.IsTrue(new QuickSelectorFilter { ActiveState = ActiveState.Active }.Matches(child));
        }

        [Test]
        public void Component_MatchesSubclasses()
        {
            var box = Create("Box");
            box.AddComponent<BoxCollider>();
            var empty = Create("Empty");

            Assert.IsTrue(new QuickSelectorFilter { ComponentType = typeof(Collider) }.Matches(box));
            Assert.IsTrue(new QuickSelectorFilter { ComponentType = typeof(BoxCollider) }.Matches(box));
            Assert.IsFalse(new QuickSelectorFilter { ComponentType = typeof(Collider) }.Matches(empty));
        }

        [Test]
        public void Find_SearchesInactiveChildrenAndSortsByPath()
        {
            var root = Create("QS_Test_Root");
            var b = Create("QS_Test_B", root.transform);
            var a = Create("QS_Test_A", root.transform);
            b.SetActive(false);

            var results = new QuickSelectorFilter { Name = "QS_Test_?" }.Find();

            Assert.AreEqual(2, results.Count);
            Assert.AreSame(a, results[0].gameObject);
            Assert.AreSame(b, results[1].gameObject);
            StringAssert.EndsWith("/QS_Test_Root/QS_Test_A", results[0].path);
        }
    }
}
