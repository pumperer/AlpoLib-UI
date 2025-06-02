using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using alpoLib.Util;
using UnityEngine;

namespace alpoLib.UI
{
    public sealed class PopupWaiter
    {
        class PopupData
        {
            public PopupBase Popup;
            public bool Open;
            public float OpenTime;
            public float WaitTime;
            public Action FinishedCallback;
        }

        private List<PopupData> popupDataList = new();
        private bool isRunning = false;
        private Coroutine corWait = null;

        public void Add(PopupBase popup, float waitTime, Action cbFinished)
        {
            if (waitTime < 0)
                return;

            var d = new PopupData
            {
                Popup = popup,
                WaitTime = waitTime,
                Open = false,
                FinishedCallback = cbFinished
            };

            popupDataList.Add(d);

            if (!isRunning)
                corWait = CoroutineTaskManager.AddTask(CR_WaitClose());
        }

        private IEnumerator CR_WaitClose()
        {
            var wfef = new WaitForEndOfFrame();
			isRunning = true;

			while (popupDataList.Count > 0)
            {
                for (int i = popupDataList.Count - 1; i >= 0; i--)
                {
                    var data = popupDataList[i];
                    if (data.Open && data.OpenTime + data.WaitTime <= Time.fixedTime)
                    {
                        popupDataList.Remove(data);
                        if (data.Popup != null && data.Popup.gameObject != null)
                        {
                            data.Popup.Close();
                            data.FinishedCallback?.Invoke();
                        }
                    }
                }
                yield return wfef;
            }

            isRunning = false;
        }

        public void StartTimer(PopupBase popup)
        {
            foreach (var d in popupDataList.Where(d => d.Popup == popup))
            {
                d.Open = true;
                d.OpenTime = Time.fixedTime;
            }
        }

        public void Close()
        {
            isRunning = false;
            popupDataList.Clear();

            if (corWait != null)
            {
                CoroutineTaskManager.RemoveTask(corWait);
                corWait = null;
            }
        }

        public void ClearTimer()
        {
            popupDataList.Clear();
        }
    }
}