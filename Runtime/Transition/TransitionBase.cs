using System;
using UnityEngine;

namespace alpoLib.UI
{
    public enum TransitionState
    {
        None,
        In,
        Out,
    }
    
    public interface ILoadingProgress
    {
        float SetProgress(float progress, float duration = 0);
        float SetProgressWithMessage(float progress, string message, float duration = 0);
        float AddProgress(float progress);
        float AddProgressWithMessage(float progress, string message);
        void SetMessage(string message);
    }

    public abstract class TransitionBase : MonoBehaviour, ILoadingProgress
    {
        public abstract void In(Action complete);
        public abstract void Out(Action complete);
        public abstract float SetProgress(float progress, float duration = 0);
        public abstract float SetProgressWithMessage(float progress, string message, float duration = 0);
        public abstract float AddProgress(float progress);
        public abstract float AddProgressWithMessage(float progress, string message);
        public abstract void SetMessage(string message);
    }
}