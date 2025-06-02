using System;
using System.Collections;
using alpoLib.Res;
using UnityEngine;

namespace alpoLib.UI
{
    [PrefabPath("Prefabs/UI/Transition/DefaultTransition", PrefabPathSource.Resources)]
    public class DefaultTransition : TransitionBase
    {
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected float duration = 0.5f;
    
        public override void In(Action complete)
        {
            enabled = true;
            StartCoroutine(CR_Play(0f, 1f, duration, complete));
        }

        public override void Out(Action complete)
        {
            enabled = true;
            StartCoroutine(CR_Play(1f, 0f, duration, complete));
        }

        public override float SetProgress(float progress, float duration = 0)
        {
            return 0f;
        }

        public override float SetProgressWithMessage(float progress, string message, float duration = 0)
        {
            return 0f;
        }

        public override float AddProgress(float progress)
        {
            return 0f;
        }

        public override void SetMessage(string message)
        {
        }

        public override float AddProgressWithMessage(float progress, string message)
        {
            return 0f;
        }

        private IEnumerator CR_Play(float fromAlpha, float toAlpha, float durationSec, Action complete)
        {
            // var c = blackImage.color;
            var currentTime = 0f;
            while (true)
            {
                var value = Mathf.Lerp(fromAlpha, toAlpha, currentTime / durationSec);
                // c.a = value;
                canvasGroup.alpha = value;
                // blackImage.color = c;

                if (currentTime >= durationSec)
                    break;
            
                currentTime += Time.deltaTime;
                yield return null;
            }
        
            enabled = false;
            complete?.Invoke();
        }
    }
}