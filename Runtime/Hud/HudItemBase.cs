using alpoLib.Util;
using UnityEngine;

namespace alpoLib.UI.Hud
{
    public abstract class HudItemBase : CachedUIBehaviour
    {
        [SerializeField] protected bool isActive = true;
        [SerializeField] protected Transform animationRoot;
        [SerializeField] protected HudItemPosition hudItemPosition = HudItemPosition.None;

        public HudItemPosition HudItemPosition => hudItemPosition;
        
        protected HudItemController HUDItemController;

        protected override void Awake()
        {
            base.Awake();
            if (!animationRoot)
                animationRoot = transform;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnAddEvent();
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();
            OnRemoveEvent();
        }

        public void Initialize(HudItemController controller)
        {
            HUDItemController = controller;
            OnInitialize();
        }

        public void OnClickHudItem()
        {
            OnClickHudItemEvent();
        }

        protected virtual void OnInitialize()
        {
        }

        protected virtual void OnClickHudItemEvent()
        {
        }
        
        protected virtual void OnAddEvent()
        {
        }

        protected virtual void OnRemoveEvent()
        {
        }

        protected virtual bool CheckCanOpen()
        {
            return true;
        }
    }
}
