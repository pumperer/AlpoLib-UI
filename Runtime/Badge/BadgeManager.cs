using System;
using System.Text;
using System.Collections.Generic;
using alpoLib.Util;

namespace alpoLib.UI
{
    internal class BadgeState : PersistentGameState
    {
        public Dictionary<BadgeType, string> BadgePathList { get; set; } = new();

        public void Set(BadgeType type, string path)
        {
            BadgePathList.TryAdd(type, path);
            BadgePathList[type] = path;
        }

        public string Get(BadgeType type)
        {
            return BadgePathList.GetValueOrDefault(type, string.Empty);
        }
    }
    
    internal class BadgeNode
    {
        public BadgeNode(string[] route, int index, BadgeNode parent, bool withSave)
        {
            if (route != null)
            {
                var builder = new StringBuilder();
                for (int i = 0; i < index + 1; i++)
                {
                    builder.Append(route[i]);
                    if (i != index)
                    {
                        builder.Append("/");
                    }
                }
                this.route = builder.ToString();
            }
            else
            {
                this.route = "";
            }

            this.withSave = withSave;
            this.parent = parent;
        }

        public string route;
        public string Key
        {
            get
            {
                var index = route.LastIndexOf('/');
                return route.Substring(index + 1);
            }
        }

        public bool withSave;
        public BadgeNode parent;
        public Dictionary<string, BadgeNode> children = new Dictionary<string, BadgeNode>();
    }

    public enum BadgeType
    {
        None,
        RedDot,
        AdDot,
        NewDot,
        Max
    }

    public class BadgeManager : Singleton<BadgeManager>
    {
        private Dictionary<BadgeType, BadgeNode> rootBadgeNodes = new ();
        private List<Badge> managedBadges = new ();
        private ThrottleAction refreshDelegate = new ThrottleAction(1/60f);

        private BadgeState state;
        
        private void ClearRootBadgeNodes()
        {
            rootBadgeNodes.Clear();
            var badgeTypes = Enum.GetValues(typeof(BadgeType));
            foreach (var type in badgeTypes)
            {
                var badgeType = (BadgeType)type;
                if (badgeType == BadgeType.None || badgeType == BadgeType.Max)
                    continue;
                rootBadgeNodes.Add(badgeType, new BadgeNode(null, 0, null, false));
            }
        }

        public void Clear()
        {
            managedBadges.Clear();
            ClearRootBadgeNodes();
            refreshDelegate.RemoveListener(UpdateAllBadges);
        }

        public void Initialize()
        {
            state = GameStateManager.Instance.GetState<BadgeState>();
            ClearRootBadgeNodes();
            refreshDelegate.AddListener(UpdateAllBadges);
            Load();
        }

        public void AddToManagedBadgeList(Badge badge)
        {
            if (!managedBadges.Contains(badge))
                managedBadges.Add(badge);
        }

        public void RemoveFromManagedBadgeList(Badge badge)
        {
            managedBadges.Remove(badge);
        }

        public void AddBadge(BadgeType type, string path, bool withSave = false)
        {
            if (type == BadgeType.None)
                return;

            var route = path.Split('/');
            // 검증
            for (int i = 0; i < route.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(route[i]))
                    return;
            }

            var badgeNode = rootBadgeNodes[type];
            for (int i = 0; i < route.Length; i++)
            {
                if (!badgeNode.children.ContainsKey(route[i]))
                {
                    badgeNode.children.Add(route[i], new BadgeNode(route, i, badgeNode, withSave));
                }
                badgeNode = badgeNode.children[route[i]];
            }

            if (withSave)
            {
                Save();
            }
            refreshDelegate.ThrottleInvoke();
        }

        public void RemoveBadge(BadgeType type, string path)
        {
            if (type == BadgeType.None)
                return;

            var route = path.Split('/');
            var badgeNode = rootBadgeNodes[type];
            for (int i = 0; i < route.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(route[i]))
                    return;
                if (!badgeNode.children.ContainsKey(route[i]))
                    return;
                badgeNode = badgeNode.children[route[i]];
            }

            var hasWithSave = CheckHasWithSave(badgeNode);
            badgeNode.children.Clear();

            while (badgeNode.children.Count == 0)
            {
                if (badgeNode.parent == null)
                    break;
                var key = badgeNode.Key;
                badgeNode = badgeNode.parent;
                badgeNode.children.Remove(key);
            }

            if (hasWithSave)
            {
                Save();
            }
            refreshDelegate.ThrottleInvoke();
        }
        
        private bool CheckHasWithSave(BadgeNode node)
        {
            if (node.withSave)
                return true;
            foreach (var child in node.children)
            {
                if (CheckHasWithSave(child.Value))
                    return true;
            }
            return false;
        }

        public void ClearAllBadge(BadgeType type, string path)
        {
            if (type == BadgeType.None)
                return;

            var route = path.Split('/');
            var badgeNode = rootBadgeNodes[type];
            for (int i = 0; i < route.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(route[i]))
                    return;
                if (!badgeNode.children.ContainsKey(route[i]))
                    return;
                badgeNode = badgeNode.children[route[i]];
            }

            badgeNode.children.Clear();
            badgeNode.parent.children.Remove(badgeNode.Key);
            refreshDelegate.ThrottleInvoke();
        }

        public bool HasBadge(BadgeType type, string path)
        {
            var route = path.Split('/');
            var badgeNode = rootBadgeNodes[type];
            for (int i = 0; i < route.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(route[i]))
                    return false;
                if (!badgeNode.children.ContainsKey(route[i]))
                    return false;
                badgeNode = badgeNode.children[route[i]];
            }
            return true;
        }

        public int GetBadgeCount(BadgeType type, string path)
        {
            var route = path.Split('/');
            var badgeNode = rootBadgeNodes[type];
            for (int i = 0; i < route.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(route[i]))
                    return 0;
                if (!badgeNode.children.ContainsKey(route[i]))
                    return 0;
                badgeNode = badgeNode.children[route[i]];
            }

            var resList = new List<BadgeNode>();
            GetLastNodes(badgeNode, ref resList);
            return resList.Count;
        }

        private void UpdateAllBadges()
        {
            for (int i = 0; i < managedBadges.Count; i++)
            {
                if (managedBadges[i])
                    managedBadges[i].UpdateBadge();
            }
        }

        #region Save & Load
        public void Save()
        {
            var builder = new StringBuilder();
            for (int i = 1; i < (int)BadgeType.Max; i++)
            {
                var parent = rootBadgeNodes[(BadgeType)i];
                var resList = new List<BadgeNode>();
                GetLastNodes(parent, ref resList);
                builder.Clear();
                for (int j = 0; j < resList.Count; j++)
                {
                    if (!resList[j].withSave)
                        continue;
                    builder.Append(resList[j].route);
                    if (j < resList.Count - 1)
                        builder.Append("|");
                }

                state.Set((BadgeType)i, builder.ToString());
            }
        }

        private void Load()
        {
            foreach (var pair in rootBadgeNodes)
            {
                pair.Value.children.Clear();
            }

            for (int i = 1; i < (int)BadgeType.Max; i++)
            {
                var data = state.Get((BadgeType)i);
                if (string.IsNullOrWhiteSpace(data))
                    continue;
                var paths = data.Split('|');
                for (int j = 0; j < paths.Length; j++)
                {
                    AddBadge((BadgeType)i, paths[j]);
                }
            }
        }

        private void GetLastNodes(BadgeNode parent, ref List<BadgeNode> resList)
        {
            if (parent.children.Count == 0)
            {
                resList.Add(parent);
                return;
            }

            foreach(var child in parent.children)
            {
                GetLastNodes(child.Value, ref resList);
            }
        }
        #endregion
    }
}
