using alpoLib.Util;
using UnityEngine;
using UnityEngine.UI;

namespace alpoLib.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaBottomMargin : CachedUIBehaviour
    {
        [SerializeField] private bool extend;

        public float MarginBottom { get; private set; }
        public bool IsExtend => extend;

        protected override void Awake()
        {
            base.Awake();
            var rectTr = CachedRectTransform;
            var originSizeDelta = rectTr.sizeDelta;
            var originAnchoredPosition = rectTr.anchoredPosition;
            var canvasScaler = GetComponentInParent<CanvasScaler>();
            var canvasScalerRectTr = canvasScaler.GetComponent<RectTransform>();

            var safeArea = Screen.safeArea;
            var resolution = Screen.fullScreen ? new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) : new Vector2(Screen.width, Screen.height);

            var marginBottom = safeArea.y;
            float resolutionRatio;
            if (Mathf.Approximately(canvasScalerRectTr.rect.size.x, canvasScaler.referenceResolution.x))
            {
                resolutionRatio = canvasScaler.referenceResolution.x / resolution.x;
            }
            else
            {
                resolutionRatio = canvasScaler.referenceResolution.y / resolution.y;
            }

            MarginBottom = marginBottom * resolutionRatio * SafeArea.MarginRatio.bottom;
            
            if (extend)
            {
                rectTr.sizeDelta = originSizeDelta + new Vector2(0f, MarginBottom);
                rectTr.anchoredPosition = originAnchoredPosition + new Vector2(0f, MarginBottom * rectTr.pivot.y);
            }
            else
            {
                rectTr.anchoredPosition = originAnchoredPosition + new Vector2(0f, MarginBottom);
            }
        }
    }
}
