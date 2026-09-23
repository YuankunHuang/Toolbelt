using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Toolbelt.ObjectFavorites
{
    /// <summary>
    /// Favorite pages of the current user in the current project. Stored in UserSettings/, so they stay out of
    /// Assets and out of version control. Objects are kept as GlobalObjectId strings, which also covers scene objects.
    /// </summary>
    [FilePath("UserSettings/Toolbelt/ObjectFavorites.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ObjectFavoritesStore : ScriptableSingleton<ObjectFavoritesStore>
    {
        [System.Serializable]
        public class PageData
        {
            public string name;
            public List<string> globalObjIds = new List<string>();
        }

        public List<PageData> pages = new List<PageData>();

        public void Save()
        {
            Save(true);
        }

        public static Object ConvertGlobalIdToObject(string globalId)
        {
            if (string.IsNullOrEmpty(globalId) || !GlobalObjectId.TryParse(globalId, out var gid))
            {
                return null;
            }
            return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
        }

        public static string ConvertObjectToGlobalId(Object obj)
        {
            if (obj == null)
            {
                return null;
            }
            return GlobalObjectId.GetGlobalObjectIdSlow(obj).ToString();
        }

        /// <summary>
        /// Fills <paramref name="result"/> with the indices of <paramref name="objects"/> to show for a search.
        /// Empty keyword: every entry, empty slots included. Otherwise case-insensitive on the name: plain text
        /// matches any part, * and ? match the whole name.
        /// </summary>
        public static void GetVisibleIndices(IList<Object> objects, string keyword, List<int> result)
        {
            result.Clear();
            keyword = keyword?.Trim();
            if (string.IsNullOrEmpty(keyword) || keyword == "*")
            {
                for (var i = 0; i < objects.Count; ++i)
                {
                    result.Add(i);
                }
                return;
            }

            var escaped = Regex.Escape(keyword);
            var hasWildcard = keyword.IndexOf('*') >= 0 || keyword.IndexOf('?') >= 0;
            var regex = new Regex(
                hasWildcard ? "^" + escaped.Replace("\\*", ".*").Replace("\\?", ".") + "$" : escaped,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            for (var i = 0; i < objects.Count; ++i)
            {
                var obj = objects[i];
                if (obj != null && regex.IsMatch(obj.name))
                {
                    result.Add(i);
                }
            }
        }
    }
}
