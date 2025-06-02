using alpoLib.UI.Scene;
using UnityEngine;

namespace alpoLib.UI
{
    public abstract class SceneUIBase : MonoBehaviour
    {
        [SerializeField] protected Camera uiCamera;
        [SerializeField] protected bool useScenePopupTrack;
        protected PopupTrack popupTrack = null;
        
        public ISceneBase CurrentScene { get; set; }

        public Camera UICamera => uiCamera;
        public PopupTrack ScenePopupTrack => popupTrack;
        
        private Canvas _canvas;
        public Canvas Canvas => _canvas;

        public virtual void OnInitialize()
        {
            _canvas ??= GetComponentInChildren<Canvas>();
            if (_canvas != null)
                uiCamera ??= _canvas.worldCamera;

            if (useScenePopupTrack)
            {
                popupTrack ??= new PopupTrack(transform);
                popupTrack.CreatePopupDim(_canvas.transform, OnClickPopupDim);
            }
        }

        public virtual void OnClose()
        {
            popupTrack?.CloseAll();
            popupTrack?.DestroyPopupDim();
        }

        public virtual bool OnPressedBackButton()
        {
            if (popupTrack is { IsRunning: true })
                return popupTrack.OnBack();
                
            return false;
        }

        protected virtual void OnClickPopupDim()
        {
            
        }
    }
}
