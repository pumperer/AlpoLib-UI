using System;
using alpoLib.Util;

namespace alpoLib.UI
{
    public abstract class SlotDataBase
    {
        public int Index = 0;
        public bool Invalidate = false;
    }

    public abstract class UISlotBase<TData, TSlot> : CachedUIBehaviour
        where TData : SlotDataBase
        where TSlot : UISlotBase<TData, TSlot>
    {
        protected TData Data;
        protected Action<TSlot, TData> ClickAction;
        protected Action<TSlot, TData> LongPressAction;
        protected bool Invalidate;
        
        public TData GetData()
        {
            return Data;
        }
        
        protected abstract void OnSetData(TData data);
        
        public void SetData(TData data, Action<TSlot, TData> clickAction = null, Action<TSlot, TData> longPressAction = null)
        {
            SetClickAction(clickAction);
            SetLongPressAction(longPressAction);
            OnSetData(data);
        }

        public void UpdateData(TData data, bool immediate = false)
        {
            Invalidate = Data != data;
            Data = data;
            immediate |= data.Invalidate;
            if (Invalidate || immediate)
                OnSetData(data);
            Invalidate = false;
            data.Invalidate = false;
        }
        
        public void SetClickAction(Action<TSlot, TData> clickAction)
        {
            ClickAction = clickAction;
        }
        
        public void SetLongPressAction(Action<TSlot, TData> longPressAction)
        {
            LongPressAction = longPressAction;
        }
    }
}