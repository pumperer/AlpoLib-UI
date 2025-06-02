using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using alpoLib.Res;
using alpoLib.UI.Scene;
using alpoLib.Util;
using UnityEngine;

namespace alpoLib.UI
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DoNotUsePopupDimAttribute : Attribute
    {
    }
    
	public interface IPopupEventListener
    {
        void OnPopupClosed(PopupBase popup);
    }

    public enum PopupDimPosition
    {
        Nothing,
        FirstOnHierarchy,
        LastOnHierarchy,
    }

	public sealed class PopupTrack : IPopupEventListener
	{
        public delegate void OnAddedPopupDelegate(PopupTrack track, PopupBase addedPopup);
        public delegate void OnRemovedPopupDelegate(PopupTrack track, PopupBase removedPopup);
        public delegate void OnStartTrackDelegate(PopupTrack track, PopupBase firstPopup);
        public delegate void OnStopTrackDelegate(PopupTrack track);

        public event OnAddedPopupDelegate OnAddedPopupEvent;
        public event OnRemovedPopupDelegate OnRemovedPopupEvent;
        public event OnStartTrackDelegate OnStartTrackEvent;
        public event OnStopTrackDelegate OnStopTrackEvent;

        private bool isRunning = false;
        private LinkedList<PopupBase> popupList = new();
        private PopupWaiter popupWaiter = new();
        private PopupBase currentOpenedPopup = null;
        private Coroutine chainCoroutine = null;

        public PopupBase CurrentPopup => currentOpenedPopup;
        public bool IsRunning => isRunning;

        public bool WaitForNextPopup { get; set; } = false;

        private PopupDimPosition _dimPosition;
        private PopupDim popupDim;
        private Action popupDimClickAction;

        public Transform PopupParent { get; }

        public static int DimAlpha { private get; set; } = 240;

        public PopupTrack(Transform popupParent, Action popupDimClickAction = null)
        {
            PopupParent = popupParent;
            _dimPosition = PopupDimPosition.LastOnHierarchy;
            this.popupDimClickAction = popupDimClickAction;
        }

        public void Show(PopupBase popup)
        {
            Add(popup);
            StartTrack();
        }

        public void Show(PopupBase popup, float waitTime, Action cbFinished = null)
        {
            if (waitTime > 0)
                popupWaiter.Add(popup, waitTime, cbFinished);

            Show(popup);
        }

        public void Add(PopupBase popup)
        {
            popupList.AddLast(popup);
            popup.AttachPopupTrack(this);
        }

        public void InsertFirst(PopupBase popup)
        {
            if (popup == null)
                return;
            
            popupList.AddFirst(popup);
            popup.AttachPopupTrack(this);
            StartTrack();
        }

        public void Insert(PopupBase popup, PopupBase targetAfter = null)
        {
            if (popup == null)
                return;

            if (targetAfter != null)
            {
                var target = popupList.Find(targetAfter);
                if (target != null)
                {
                    popupList.AddAfter(target, popup);
                    return;
                }
            }

            popupList.AddLast(popup);
        }

        private void StartTrack()
        {
            if (popupList.Count == 0 || isRunning)
                return;

            isRunning = true;
            // SetActivePopupDim(true);

            OnStartTrackEvent?.Invoke(this, popupList.First.Value);
            chainCoroutine = CoroutineTaskManager.AddTask(CR_Check());
        }

        private void SetActivePopupDim(bool active)
        {
            if (active && popupDim == null)
                CreatePopupDim(PopupParent, popupDimClickAction);

            if (popupDim == null)
                return;
            
            if (currentOpenedPopup != null)
            {
                var popupIndex = currentOpenedPopup.transform.GetSiblingIndex();
                popupDim.transform.SetSiblingIndex(Mathf.Max(popupIndex - 1, 0));
            }
            
            // switch (dimPosition)
            // {
            //     case PopupDimPosition.FirstOnHierarchy:
            //         popupDim.transform.SetAsFirstSibling();
            //         break;
            //     case PopupDimPosition.LastOnHierarchy:
            //         popupDim.transform.SetAsLastSibling();
            //         break;
            // }
            popupDim.SetPopupDim(active);
        }

        private void ChainNext()
        {
            if (!isRunning)
                return;

            if (WaitForNextPopup && popupList.Count == 0)
            {
                return;
            }
            
            if (popupList.Last.Value == null)
            {
                currentOpenedPopup = null;
                popupList.RemoveLast();
                return;
            }

            if (currentOpenedPopup != null)
                currentOpenedPopup.gameObject.SetActive(false);

            WaitForNextPopup = false;
            currentOpenedPopup = popupList.Last.Value;

            var attr = currentOpenedPopup.GetType().GetCustomAttribute(typeof(DoNotUsePopupDimAttribute));
            SetActivePopupDim(attr == null);

            currentOpenedPopup.Open();
            OnAddedPopupEvent?.Invoke(this, currentOpenedPopup);
            popupWaiter.StartTimer(currentOpenedPopup);
        }

        public bool OnBack()
        {
            return currentOpenedPopup != null && currentOpenedPopup.OnBack();
        }

        private IEnumerator CR_Check()
        {
            if (!isRunning)
                yield break;

            var waiter = new WaitForEndOfFrame();

            while (isRunning)
            {
                if (popupList.Count == 0 && !WaitForNextPopup)
                {
                    OnEndTrack();
                    break;
                }

                if (currentOpenedPopup == null || popupList.Last.Value != currentOpenedPopup)
                    ChainNext();

                yield return waiter;
            }
        }

        public void CloseAll()
        {
            isRunning = false;

            if (chainCoroutine != null)
            {
                CoroutineTaskManager.RemoveTask(chainCoroutine);
                chainCoroutine = null;
			}

			while (popupList.Count > 0)
			{
				var p = popupList.Last.Value;
				popupList.RemoveLast();
				if (p != null)
					p.Close();
			}

            OnEndTrack();
		}

		private void OnEndTrack()
        {
            WaitForNextPopup = false;
			isRunning = false;
			currentOpenedPopup = null;
			popupList.Clear();
			popupWaiter.Close();
            if (popupDim != null)
                popupDim.SetPopupDim(false);
			OnStopTrackEvent?.Invoke(this);
		}

		public void OnPopupClosed(PopupBase popup)
		{
			popupList.Remove(popup);
			if (currentOpenedPopup == popup)
				currentOpenedPopup = null;
		}

        public void CreatePopupDim(Transform parent, Action clickEvent = null)
        {
            popupDimClickAction = clickEvent;
            
            if (popupDim != null)
                return;

            if (SceneManager.Instance == null)
                return;

            // if (SceneManager.CurrentScene is not ISceneBaseWithUI scene)
            //     return;

            parent ??= SceneManager.CurrentScene is ISceneBaseWithUI scene
                ? scene.SceneUI.Canvas.transform
                : UIRoot.Instance.PopupCanvas;
            
            popupDim = GenericPrefab.InstantiatePrefab<PopupDim>(parent: parent);
            popupDim.Initialize(this, DimAlpha);
            popupDim.OnClickEvent = popupDimClickAction;
            SetActivePopupDim(false);
        }

        public void DestroyPopupDim()
        {
            if (popupDim == null)
                return;
            
            UnityEngine.Object.Destroy(popupDim.gameObject);
            popupDim = null;
        }
	}
}