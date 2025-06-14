using System;
using System.Collections.Generic;
using System.Linq;

namespace alpoLib.UI
{
    public class ListSlotController<TData, TSlot>
        where TData : SlotDataBase
        where TSlot : UISlotBase<TData, TSlot>
    {
        private class ListUpdater
        {
            private int _index;
            private readonly List<TSlot> _target;
            private readonly Func<TData, TSlot> _createSlotFunc;
            private readonly Action<TSlot> _deleteSlotFunc;
            private readonly Func<IEnumerator<TData>, int, TSlot[]> _createBulkSlotFunc;
            
            public ListUpdater(List<TSlot> target,
                Func<TData, TSlot> createSlotFunc,
                Action<TSlot> deleteSlotFunc,
                Func<IEnumerator<TData>, int, TSlot[]> createBulkSlotFunc)
            {
                _target = target;
                _createSlotFunc = createSlotFunc;
                _deleteSlotFunc = deleteSlotFunc;
                _createBulkSlotFunc = createBulkSlotFunc;
            }

            private void Update(TData data)
            {
                if (_index < _target.Count)
                {
                    _target[_index].UpdateData(data);
                }
                else
                {
                    if (_createBulkSlotFunc != null)
                    {
                        var arrData = new List<TData> { data };
                        var newSlots = _createBulkSlotFunc(arrData.GetEnumerator(), 1);
                        _target.AddRange(newSlots);
                    }
                    else
                    {
                        _target.Add(_createSlotFunc(data));
                    }
                }
                _index++;
            }

            public void Update(IEnumerable<TData> collection)
            {
                if (_createBulkSlotFunc != null)
                {
                    var targetCount = _target.Count;
                    var slotDataBases = collection as TData[] ?? collection.ToArray();
                    var collectionCount = slotDataBases.Length;
                    var minCount = Math.Min(targetCount, collectionCount);
                    
                    for (var i = 0; i < minCount; i++)
                    {
                        _target[i].UpdateData(slotDataBases.ElementAt(i));
                    }
                    
                    if (targetCount < collectionCount)
                    {
                        var newSlots = _createBulkSlotFunc(slotDataBases.Skip(minCount).GetEnumerator(), collectionCount - targetCount);
                        _target.AddRange(newSlots);
                    }
                    
                    _index = collectionCount;
                }
                else
                {
                    foreach (var data in collection)
                        Update(data);
                }
            }

            public void RemoveInvalidSlot()
            {
                if (_target.Count <= _index)
                    return;
                
                for(var i = _index; i< _target.Count; i++)
                {
                    var slot = _target[i];
                    if (slot)
                        _deleteSlotFunc(slot);
                }

                _target.RemoveRange(_index, _target.Count - _index);
            }
        }

        private readonly List<TSlot> _slots = new();
        private readonly Func<TData, TSlot> _createSlotFunc;
        private readonly Action<TSlot> _deleteSlotFunc;
        private readonly Func<IEnumerator<TData>, int, TSlot[]> _createBulkSlotFunc;
        
        public int Count => _slots.Count;
        
        public ListSlotController(Func<TData, TSlot> createSlotFunc,
            Action<TSlot> deleteSlotFunc,
            Func<IEnumerator<TData>, int, TSlot[]> createBulkSlotFunc = null)
        {
            _createSlotFunc = createSlotFunc;
            _deleteSlotFunc = deleteSlotFunc;
            _createBulkSlotFunc = createBulkSlotFunc;
        }
        
        public void ApplyData(IEnumerable<TData> collection)
        {
            var updater = new ListUpdater(_slots, _createSlotFunc, _deleteSlotFunc, _createBulkSlotFunc);
            updater.Update(collection);
            updater.RemoveInvalidSlot();
        }
        
        public void ForEach(Action<TSlot> action)
        {
            foreach (var slot in _slots)
                action?.Invoke(slot);
        }
        
        public TSlot[] ToArray()
        {
            return _slots.ToArray();
        }
        
        public TSlot FindSlot(Predicate<TSlot> predicate)
        {
            return _slots.Find(predicate);
        }

        public int FindIndex(Predicate<TSlot> predicate)
        {
            return _slots.FindIndex(predicate);
        }
        
        public TSlot GetSlotAt(int index)
        {
            if (index < 0 || index >= _slots.Count)
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
            return _slots[index];
        }
    }
}