using System;
using alpoLib.Res;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace alpoLib.UI
{
    [PrefabPath("Prefabs/UI/Popup/PopupDim", PrefabPathSource.Resources)]
    public class PopupDim : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image image;
        [SerializeField] private CanvasGroup canvasGroup;

        private PopupTrack owner;

        public Action OnClickEvent;
        
        public void Initialize(PopupTrack pt, int alpha)
        {
            image.color = new Color(0, 0, 0, alpha / 255f);
            owner = pt;
        }
        
        public void SetPopupDim(bool active)
        {
            if (active)
            {
                canvasGroup.alpha = 1;
                canvasGroup.blocksRaycasts = true;
            }
            else
            {
                canvasGroup.alpha = 0;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClickEvent?.Invoke();
            owner.OnBack();
        }
    }
}