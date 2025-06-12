using alpoLib.Util;
using UnityEngine;
using UnityEngine.UI;

namespace alpoLib.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaTopMargin : CachedUIBehaviour
    {
        [SerializeField] private bool extend;

        private static float? _marginTop;
        public static float MarginTop => _marginTop ?? 0;

        public bool IsExtend => extend;

        protected override void Awake()
        {
            base.Awake();
            var rectTr = CachedRectTransform;
            var originSizeDelta = rectTr.sizeDelta;
            var originAnchoredPosition = rectTr.anchoredPosition;
            var canvasScaler = GetComponentInParent<CanvasScaler>();
            var canvasScalerRectTr = canvasScaler.GetComponent<RectTransform>();

            if (_marginTop == null)
            {
                var safeArea = Screen.safeArea;
                var resolution = Screen.fullScreen ? new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) : new Vector2(Screen.width, Screen.height);

                _marginTop = resolution.y - (safeArea.y + safeArea.height);
                float resolutionRatio;
                if (Mathf.Approximately(canvasScalerRectTr.rect.size.x, canvasScaler.referenceResolution.x))
                {
                    resolutionRatio = canvasScaler.referenceResolution.x / resolution.x;
                }
                else
                {
                    resolutionRatio = canvasScaler.referenceResolution.y / resolution.y;
                }

                _marginTop = _marginTop * resolutionRatio * SafeArea.MarginRatio.top;
            }

            if (extend)
            {
                rectTr.sizeDelta = originSizeDelta + new Vector2(0f, MarginTop);
                rectTr.anchoredPosition = originAnchoredPosition - new Vector2(0f, MarginTop * (1f - rectTr.pivot.y));
            }
            else
            {
                rectTr.anchoredPosition = originAnchoredPosition - new Vector2(0f, MarginTop);
            }
        }
    }
}
