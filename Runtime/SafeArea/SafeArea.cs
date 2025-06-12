using alpoLib.Util;
using UnityEngine;

namespace alpoLib.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : CachedUIBehaviour
    {
        [SerializeField] private bool isControlTop = true;
        [SerializeField] private bool isControlBottom = true;
        [SerializeField] private bool isControlLeft = true;
        [SerializeField] private bool isControlRight = true;

        private Rect _processedSafeArea;
        private Vector2 _processedResolution;

        private static (float left, float right, float top, float bottom) _marginRatio;
        public static (float left, float right, float top, float bottom) MarginRatio
        {
            get
            {
                if (!Mathf.Approximately(_marginRatio.left, 0f))
                    return _marginRatio;

                _marginRatio.left = 1f;
                _marginRatio.right = 1f;
                _marginRatio.top = 1f;
                _marginRatio.bottom = 1f;

                return _marginRatio;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            var rectTr = CachedRectTransform;
            var safeArea = Screen.safeArea;
            var resolution = Screen.fullScreen ? new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) : new Vector2(Screen.width, Screen.height);

            if (_processedSafeArea == safeArea && _processedResolution == resolution)
                return;
            _processedSafeArea = safeArea;
            _processedResolution = resolution;

            var leftAnchorDiff = safeArea.x / resolution.x * MarginRatio.left;
            var bottomAnchorDiff = safeArea.y / resolution.y * MarginRatio.bottom;

            rectTr.anchorMin = new Vector2(
                isControlLeft ? leftAnchorDiff: rectTr.anchorMin.x,
                isControlBottom ? bottomAnchorDiff : rectTr.anchorMin.y
            );

            var rightAnchorDiff = (1f - ((safeArea.x + safeArea.width) / resolution.x)) * MarginRatio.right;
            var topAnchorDiff = (1f - ((safeArea.y + safeArea.height) / resolution.y)) * MarginRatio.top;

            rectTr.anchorMax = new Vector2(
                isControlRight ? 1f - rightAnchorDiff : rectTr.anchorMax.x,
                isControlTop ? 1f - topAnchorDiff : rectTr.anchorMax.y
            );
        }
    }
}
