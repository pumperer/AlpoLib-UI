using System;
using System.Collections;
using System.Threading.Tasks;
using alpoLib.Res;
using alpoLib.UI.Scene;
using alpoLib.Util;
using UnityEngine;

namespace alpoLib.UI
{
	public enum PopupCloseResult
	{
		None = 0,
		Ok = 1,
		Cancel = 2,
		Yes = Ok,
		No = Cancel,
	}

	[RequireComponent(typeof(CanvasGroup))]
	public abstract class PopupBase : MonoBehaviour
    {
	    protected class AnimationPlayer
	    {
		    private Transform trans;
		    private CanvasGroup canvasGroup;
		    private Animation comp;
		    private string clipKey;
		    private AnimationClip clip;

		    public bool IsPlaying => comp != null && comp.isPlaying;

		    private bool useDefaultTween;

		    private Coroutine currentCoroutine;
		    
		    public AnimationPlayer(Transform trans, CanvasGroup canvasGroup, Animation comp, string clipKey, AnimationClip clip)
		    {
			    this.trans = trans;
			    this.canvasGroup = canvasGroup;
			    this.comp = comp;
			    this.clipKey = clipKey;
			    this.clip = clip;

			    if (this.comp != null && this.clip != null)
			    {
				    useDefaultTween = false;
				    comp.AddClip(this.clip, clipKey);
			    }
			    else if (canvasGroup != null)
				    useDefaultTween = true;
			    else
				    useDefaultTween = false;
		    }

		    public void Play(Action complete = null)
		    {
			    if (comp == null || clip == null)
			    {
				    if (useDefaultTween)
					    PlayDefaultTween(complete);
				    else
					    complete?.Invoke();
				    return;
			    }
			    
			    comp.Rewind();
			    comp.Play(clipKey);
			    
			    if (!clip.isLooping)
				    CoroutineTaskManager.RunDeferred(new WaitForSeconds(clip.length), complete);
		    }

		    public void PlayDefaultTween(Action complete = null)
		    {
			    if (clipKey == "IDLE_CLIP")
				    return;
			    
			    if (clipKey == "OPEN_CLIP")
			    {
				    if (currentCoroutine == null)
					    currentCoroutine = CoroutineTaskManager.AddTask(PlayDefaultOpenTween(0.1f, complete));
			    }
			    else if (clipKey == "CLOSE_CLIP")
			    {
				    if (currentCoroutine == null)
					    currentCoroutine = CoroutineTaskManager.AddTask(PlayDefaultCloseTween(0.1f, complete));
			    }
		    }

		    private IEnumerator PlayDefaultOpenTween(float duration, Action complete = null)
		    {
			    var startScale = new Vector3(1.1f, 1.1f, 1f);
			    var targetScale = Vector3.one;
			    const float startAlpha = 0.1f;
			    const float targetAlpha = 1f;

			    var currentTime = 0f;
			    while (currentTime <= duration)
			    {
				    var rate = currentTime / duration;
				    var currentScale = Vector3.Lerp(startScale, targetScale, rate);
				    var currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, rate);
				    trans.localScale = currentScale;
				    canvasGroup.alpha = currentAlpha;
				    currentTime += Time.deltaTime;
				    yield return null;
			    }

			    trans.localScale = targetScale;
			    canvasGroup.alpha = targetAlpha;
			    
			    complete?.Invoke();
			    currentCoroutine = null;
		    }

		    private IEnumerator PlayDefaultCloseTween(float duration, Action complete = null)
		    {
			    var startScale = trans.localScale;
			    var targetScale = new Vector3(1.1f, 1.1f, 1f);
			    var startAlpha = canvasGroup.alpha;
			    var targetAlpha = 0.1f;

			    var currentTime = 0f;
			    while (currentTime <= duration)
			    {
				    var rate = currentTime / duration;
				    var currentScale = Vector3.Lerp(startScale, targetScale, rate);
				    var currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, rate);
				    trans.localScale = currentScale;
				    canvasGroup.alpha = currentAlpha;
				    currentTime += Time.deltaTime;
				    yield return null;
			    }
			    
			    trans.localScale = targetScale;
			    canvasGroup.alpha = targetAlpha;
			    
			    complete?.Invoke();
			    currentCoroutine = null;
		    }
	    }

	    public delegate void OnPopupAppEventDelegate(PopupBase popup);

	    public static event OnPopupAppEventDelegate OnPopupOpenAppEvent;
	    
		public delegate void OnPopupOpenDelegate(PopupBase popup);
		public event OnPopupOpenDelegate OnPopupOpenEvent;
		public delegate void OnPopupOpenedDelegate(PopupBase popup);
		public event OnPopupOpenedDelegate OnPopupOpenedEvent;
		public delegate bool OnPopupCloseDelegate(PopupBase popup, PopupCloseResult result);
		public event OnPopupCloseDelegate OnPopupCloseEvent;
		public delegate void OnPopupClosedDelegate(PopupBase popup, PopupCloseResult result);
		public event OnPopupClosedDelegate OnPopupClosedEvent;

		private bool isAlive = true;
		private bool isOpened = false;
		private bool isClosing = false;
		private bool shouldDestroy = false;
		
		protected PopupTrack parentPopupTrack = null;
		protected PopupTrack popupTrack = null;
        
        public PopupTrack NestedPopupTrack => popupTrack;
		public PopupCloseResult Result { get; protected set; }
		private TaskCompletionSource<PopupCloseResult> tcs;
		
		[Header("Common")]
		[SerializeField] protected bool closeableByBackButton = true;
		[SerializeField] protected bool useGlobalTrack = false;

		// [Header("이 팝업에서 보여줄 Hud Group")]
		// [SerializeField] protected bool changeHudGroup = true;
		// [SerializeField] protected HudItemPosition hudPosition = HudItemPosition.None;
		//
		// public bool ChangeHudGroup => changeHudGroup;
		// public HudItemPosition HudPosition => hudPosition;
		
		[Header("Default Animation")]
		[SerializeField] protected bool useDefaultAnimation = true;

		[Header("Animation Clip")]
		protected Animation animationComp = null;
		protected const string OpenClipName = "OPEN_CLIP";
		protected const string CloseClipName = "CLOSE_CLIP";
		protected const string IdleClipName = "IDLE_CLIP";
		[SerializeField] protected AnimationClip openClip = null;
		[SerializeField] protected AnimationClip idleClip = null;
		[SerializeField] protected AnimationClip closeClip = null;

		protected AnimationPlayer openAnimationPlayer = null;
		protected AnimationPlayer idleAnimationPlayer = null;
		protected AnimationPlayer closeAnimationPlayer = null;

		protected virtual void OnOpen() { }
		protected virtual void OnOpened() { }
		protected virtual void OnIdle() { }
		protected virtual void OnClose() { }
		protected virtual void OnClosed() { }

		public static PopupBase CreatePopup(Type popupType, string prefabKey = "", Transform parent = null)
		{
			if (popupType == null)
				return null;
			
			var obj = GenericPrefab.InstantiatePrefab(popupType, prefabKey) as PopupBase;
			if (obj != null)
				obj = SetParent(obj, parent);
			return obj;
		}
		
		public static T CreatePopup<T>(string prefabKey = "", Transform parent = null) where T : PopupBase
		{
			var popup = GenericPrefab.InstantiatePrefab<T>(prefabKey);

			if (popup != null)
				popup = SetParent(popup, parent);

			return popup;
		}

		public static async Task<T> CreatePopupAsync<T>(string prefabKey = "", Transform parent = null)
			where T : PopupBase
		{
			var popup = await GenericPrefab.InstantiatePrefabAsync<T>(prefabKey);
			
			if (popup != null)
				popup = SetParent(popup, parent);
		
			return popup;
		}

		// public static void CreatePopupAsync<T>(Action<T> completeAction, string prefabKey = "", Transform parent = null)
		// 	where T : PopupBase
		// {
		// 	void popupLoaded(T popup)
		// 	{
		// 		if (popup != null)
		// 			SetParent(popup, parent);
		// 		completeAction?.Invoke(popup);
		// 	}
		//
		// 	if (string.IsNullOrEmpty(prefabKey))
		// 		GenericPrefab.LoadPrefabAsync<T>(popupLoaded);
		// 	else
		// 		GenericPrefab.LoadPrefabAsync<T>(prefabKey, popupLoaded);
		// }

		private static T SetParent<T>(T popup, Transform parent) where T : PopupBase
        {
	        var pt = popup.transform as RectTransform;
	        if (pt == null)
		        return null;

	        if (parent == null)
	        {
		        if (popup.useGlobalTrack)
			        parent = UIRoot.Instance.PopupCanvas;
		        else if (SceneManager.CurrentScene is ISceneBaseWithUI currentScene)
			        parent = currentScene.SceneUI.Canvas.transform;
	        }

	        pt.SetParent(parent == null ? UIRoot.Instance.PopupCanvas : parent);
	        pt.localPosition = Vector3.zero;
	        pt.localRotation = Quaternion.identity;
	        pt.localScale = Vector3.one;
	        pt.sizeDelta = Vector2.zero;
	        pt.anchorMin = Vector2.zero;
	        pt.anchorMax = Vector2.one;
	        pt.anchoredPosition3D = Vector3.zero;
            popup.gameObject.SetActive(false);
            return popup;
        }

		protected virtual void Awake()
		{
			tcs = new TaskCompletionSource<PopupCloseResult>();
            popupTrack = new PopupTrack(GetPreferPopupTrack().PopupParent);
            var tr = transform;
            var cg = GetComponent<CanvasGroup>();
            
            if (useDefaultAnimation)
            {
	            openAnimationPlayer = new AnimationPlayer(tr, cg, null, OpenClipName, null);
	            idleAnimationPlayer = new AnimationPlayer(tr, cg, null, IdleClipName, null);
	            closeAnimationPlayer = new AnimationPlayer(tr, cg, null, CloseClipName, null);
            }
            else
            {
	            if (animationComp == null)
		            animationComp = GetComponent<Animation>();
	            if (animationComp == null)
		            animationComp = gameObject.AddComponent<Animation>();
	            animationComp.playAutomatically = false;
	            
	            openAnimationPlayer = new AnimationPlayer(tr, null, animationComp, OpenClipName, openClip);
	            idleAnimationPlayer = new AnimationPlayer(tr, null, animationComp, IdleClipName, idleClip);
	            closeAnimationPlayer = new AnimationPlayer(tr, null, animationComp, CloseClipName, closeClip);
            }
        }

        private PopupTrack GetPreferPopupTrack()
        {
	        var track = (useGlobalTrack
		        ? UIRoot.Instance.GlobalPopupTrack
		        : (SceneManager.CurrentScene as ISceneUIBase)?.ScenePopupTrack) ?? UIRoot.Instance.GlobalPopupTrack;
	        return track;
        }

        public void Open(bool useMainTrack = true)
        {
	        // if (isOpened)
		       //  return;

	        if (parentPopupTrack == null)
	        {
		        var terminalTrack = GetPreferPopupTrack();
		        if (!useMainTrack)
		        {
			        while (true)
			        {
				        if (terminalTrack.CurrentPopup != null)
					        terminalTrack = terminalTrack.CurrentPopup.NestedPopupTrack;
				        else
					        break;
			        }
		        }

		        parentPopupTrack = terminalTrack;
		        parentPopupTrack.Show(this);

		        return;
	        }
	        
			gameObject.SetActive(true);
			if (!isOpened)
				OnOpen();
			PlayOpenAnimation();
			isOpened = true;
			OnPopupOpenAppEvent?.Invoke(this);
			OnPopupOpenEvent?.Invoke(this);
        }

        public void Close()
        {
	        if (isClosing)
		        return;
	        
	        if (openAnimationPlayer.IsPlaying || closeAnimationPlayer.IsPlaying)
		        return;

	        isClosing = true;
	        var cancelClose = OnPopupCloseEvent?.Invoke(this, Result);
	        if (cancelClose == true)
		        return;
	        
	        OnClose();

	        shouldDestroy = true;
	        
	        if (gameObject.activeInHierarchy)
		        PlayCloseAnimation();
	        else
		        CloseAndDestroy();
        }

        public virtual bool OnBack()
        {
            if (NestedPopupTrack.OnBack())
                return true;

            if (closeableByBackButton)
            {
                Close();
                return true;
            }

            return true;
        }

        private void CloseAndDestroy()
        {
	        if (!isAlive)
		        return;
	        if (!shouldDestroy)
		        return;
	        
	        OnClosed();
	        OnPopupClosedEvent?.Invoke(this, Result);
	        popupTrack?.CloseAll();
	        popupTrack?.DestroyPopupDim();
	        if (gameObject != null)
		        gameObject.SetActive(false);
	        isOpened = false;
	        isAlive = false;
	        parentPopupTrack?.OnPopupClosed(this);
	        Destroy(gameObject);
	        tcs?.SetResult(Result);
        }
        
        public void AttachPopupTrack(PopupTrack track)
        {
            parentPopupTrack = track;
		}
        
        public async Awaitable<PopupCloseResult> WaitForClose()
		{
	        return await tcs.Task;
		}

        #region Animations
        
        protected void PlayOpenAnimation()
        {
	        openAnimationPlayer.Play(() => OnAnimationEvent("open_ani_end"));
        }

        protected void PlayIdleAnimation()
        {
	        idleAnimationPlayer.Play();
        }

        protected void PlayCloseAnimation()
        {
	        closeAnimationPlayer.Play(() => OnAnimationEvent("close_ani_end"));
        }
        
        public virtual void OnAnimationEvent(string arg)
        {
	        switch (arg)
	        {
		        case "open_ani_end":
			        OnOpened();
			        OnPopupOpenedEvent?.Invoke(this);
			        OnIdle();
			        PlayIdleAnimation();
			        break;
		        
		        case "close_ani_end":
			        gameObject.SetActive(false);
			        CloseAndDestroy();
			        break;
	        }
        }
        
        #endregion
    }
}