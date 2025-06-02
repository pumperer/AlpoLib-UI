using System;
using System.Reflection;
using alpoLib.Data;

namespace alpoLib.UI
{
    public abstract class DataPopup<TInitData> : PopupBase
        where TInitData : PopupInitData
    {
        protected TInitData InitData { get; private set; }
        
        public void Initialize()
        {
            var attr = GetType().GetCustomAttribute<LoadingBlockDefinitionAttribute>();
            if (attr == null)
                return;

            if (Activator.CreateInstance(attr.LoadingBlock) is LoadingBlockBase<TInitData> lb)
                InitData = lb.MakeInitData();

            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }
    }
    
    public abstract class DataPopup<TParam, TInitData> : PopupBase
        where TParam : PopupParam
        where TInitData : PopupInitData
    {
        protected TInitData InitData { get; private set; }

        public void Initialize(TParam param = null)
        {
            var attr = GetType().GetCustomAttribute<LoadingBlockDefinitionAttribute>();
            if (attr == null)
                return;

            if (Activator.CreateInstance(attr.LoadingBlock) is LoadingBlockBase<TParam, TInitData> lb)
                InitData = lb.MakeInitData(param);

            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }
    }

    public abstract class PopupParam : ParamBase
    {
    }

    public abstract class PopupInitData : InitDataBase
    {
    }

    public abstract class PopupLoadingBlock<TInitData> : LoadingBlockBase<TInitData>
        where TInitData : InitDataBase
    {
    }

    public abstract class PopupLoadingBlock<TParam, TInitData> : LoadingBlockBase<TParam, TInitData>
        where TParam : ParamBase
        where TInitData : InitDataBase
    {
    }
}