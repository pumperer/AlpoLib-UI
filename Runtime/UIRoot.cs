using System;
using alpoLib.Core.Foundation;
using alpoLib.Util;
using UnityEngine;

namespace alpoLib.UI
{
    public class UIRoot : SingletonMonoBehaviour<UIRoot>
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Camera canvasCamera;
        [SerializeField] private Transform hudCanvas;
        [SerializeField] private Transform popupCanvas;
        [SerializeField] private Transform transitionCanvas;
        [SerializeField] private Transform forwardCanvas;
        [SerializeField] private Transform transparentLoadingCanvas;
        [SerializeField] private Transform loadingCanvas;

        private ReferenceCountBool transparentLoadingRefCount;
        private ReferenceCountBool loadingRefCount;

        public Canvas Canvas => canvas;
        public Camera CanvasCamera => canvasCamera;
        public Transform HudCanvas => hudCanvas;
        public Transform PopupCanvas => popupCanvas;
        public Transform TransitionCanvas => transitionCanvas;
        public Transform ForwardCanvas => forwardCanvas;
        public PopupTrack GlobalPopupTrack { get; private set; }

        public event Action OnClickPopupDim;

        protected override void OnAwakeEvent()
        {
            GlobalPopupTrack = new PopupTrack(popupCanvas);
            GlobalPopupTrack.CreatePopupDim(popupCanvas, () =>
            {
                OnClickPopupDim?.Invoke();
            });
            
            awakeComplete = true;
        }

        protected override void OnDestroyEvent()
        {
            GlobalPopupTrack.DestroyPopupDim();
        }

        public bool ActiveTransparentLoadingUI
        {
            get => transparentLoadingRefCount;
            set
            {
                if (!transparentLoadingCanvas)
                    return;
                
                var before = transparentLoadingRefCount;
                if (value)
                {
                    transparentLoadingRefCount++;
                    if (transparentLoadingRefCount)
                        transparentLoadingCanvas.gameObject.SetActive(true);
                    Debug.Log($"UIRoot.ActiveTransparentLoadingUI : {before} -> {transparentLoadingRefCount} (+)");
                }
                else
                {
                    transparentLoadingRefCount--;
                    if (!transparentLoadingRefCount)
                        transparentLoadingCanvas.gameObject.SetActive(false);
                    Debug.Log($"UIRoot.ActiveTransparentLoadingUI : {before} -> {transparentLoadingRefCount} (-)");
                }
            }
        }
        
        public void ForceResetTransparentLoadingUI()
        {
            if (!transparentLoadingCanvas)
                return;
            var before = transparentLoadingRefCount;
            transparentLoadingRefCount = 0;
            transparentLoadingCanvas.gameObject.SetActive(false);
            Debug.Log($"UIRoot.ForceResetTransparentLoadingUI : {before} -> {transparentLoadingRefCount}");
        }
        
        public bool ActiveLoadingUI
        {
            get => loadingRefCount;
            set
            {
                if (!loadingCanvas)
                    return;
                
                var before = loadingRefCount;
                if (value)
                {
                    loadingRefCount++;
                    if (loadingRefCount)
                        loadingCanvas.gameObject.SetActive(true);
                    Debug.Log($"UIRoot.ActiveLoadingUI : {before} -> {loadingRefCount} (+)");
                }
                else
                {
                    loadingRefCount--;
                    if (!loadingRefCount)
                        loadingCanvas.gameObject.SetActive(false);
                    Debug.Log($"UIRoot.ActiveLoadingUI : {before} -> {loadingRefCount} (-)");
                }
            }
        }

        public void ForceResetLoadingUI()
        {
            if (!loadingCanvas)
                return;
            var before = loadingRefCount;
            loadingRefCount = 0;
            loadingCanvas.gameObject.SetActive(false);
            Debug.Log($"UIRoot.ForceResetLoadingUI : {before} -> {loadingRefCount}");
        }
    }
}