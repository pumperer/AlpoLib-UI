using System;
using System.Collections.Generic;

namespace alpoLib.UI.Hud
{
    public class HudItemController
    {
        private readonly HudItemBase[] _hudItems;
        private readonly Dictionary<Type, HudItemBase> _hudItemsByType = new();
        private readonly Dictionary<HudItemPosition, HashSet<HudItemBase>> _hudItemsByPosition = new();

        public HudItemController(HudItemBase[] hudItems)
        {
            _hudItems = hudItems;
            
            foreach (var item in _hudItems)
            {
                item.Initialize(this);
                var type = item.GetType();
                if (!_hudItemsByType.TryAdd(type, item))
                {
                    Debug.LogWarning($"HudItemController: Duplicate HudItem of type {type} found. Only one will be used.");
                    continue;
                }

                if (!_hudItemsByPosition.ContainsKey(item.HudItemPosition))
                    _hudItemsByPosition[item.HudItemPosition] = new HashSet<HudItemBase>();
                _hudItemsByPosition[item.HudItemPosition].Add(item);
            }
        }
        
        public T GetHudItem<T>() where T : HudItemBase
        {
            if (_hudItemsByType.TryGetValue(typeof(T), out var item))
                return item as T;
            return null;
        }
        
        public HashSet<HudItemBase> GetHudItemsByPosition(HudItemPosition position)
        {
            if (_hudItemsByPosition.TryGetValue(position, out var items))
                return items;
            return new HashSet<HudItemBase>();
        }
        
        public T FindFirstHudItem<T>()
        {
            foreach (var hudItem in _hudItems)
            {
                if (hudItem is T item)
                    return item;
            }
            return default;
        }
        
        public void UpdateHudItem(Action<HudItemBase> updateAction)
        {
            foreach (var hudItem in _hudItems)
            {
                updateAction?.Invoke(hudItem);
            }
        }
    }
}