using System;
using alpoLib.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace alpoLib.UI.Scene
{
    public enum SceneResourceType
    {
        Default,
        Addressable,
#if USE_ASSETBUNDLE
        AssetBundle,
#endif
    }
    
    public class SceneDefineAttribute : Attribute
    {
        public string ScenePath { get; private set; }

        public LoadSceneMode LoadSceneMode { get; }
        
        public SceneResourceType ResourceType { get; }

        public string SubPath { get; private set; }

        public SceneDefineAttribute(string scenePath = "", SceneResourceType resourceType = SceneResourceType.Addressable)
        {
            ScenePath = scenePath;
            LoadSceneMode = LoadSceneMode.Single;
            ResourceType = resourceType;
            CheckSubPath();
        }

        public SceneDefineAttribute(string scenePath, LoadSceneMode loadSceneMode = LoadSceneMode.Single, SceneResourceType resourceType = SceneResourceType.Addressable)
        {
            ScenePath = scenePath;
            LoadSceneMode = loadSceneMode;
            ResourceType = resourceType;
            CheckSubPath();
        }

        public SceneDefineAttribute(LoadSceneMode loadSceneMode, SceneResourceType resourceType = SceneResourceType.Addressable)
        {
            ScenePath = string.Empty;
            LoadSceneMode = loadSceneMode;
            ResourceType = resourceType;
            CheckSubPath();
        }

        private void CheckSubPath()
        {
#if USE_ASSETBUNDLE
            if (ResourceType != SceneResourceType.AssetBundle)
                return;

            if (string.IsNullOrEmpty(ScenePath))
                return;
            var path = ScenePath;
            var lastIndexOfSeparator = path.LastIndexOf('/');
            if (lastIndexOfSeparator <= -1)
                return;
            ScenePath = path[(lastIndexOfSeparator + 1)..].Replace(".unity", string.Empty);
            SubPath = $"ab/{path[..lastIndexOfSeparator].ToLowerInvariant()}";
#endif
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SceneTransitionAttribute : Attribute
    {
        public string Name { get; }
        public Type TransitionType { get; }

        public SceneTransitionAttribute(Type transitionType)
        {
            Name = "Default";
            TransitionType = transitionType;
        }

        public SceneTransitionAttribute(string name, Type transitionType)
        {
            Name = name;
            TransitionType = transitionType;
        }
    }

    public interface ISceneBase
    {
        void SetSceneInitData(SceneInitData data);
        void OnClose();
    }

    public interface ISceneUIBase
    {
        PopupTrack ScenePopupTrack { get; }
    }
    
    public interface ISceneBaseWithUI : ISceneUIBase
    {
        SceneUIBase SceneUI { get; }
    }

    public abstract class SceneBase : MonoBehaviour, ISceneBase
    {
        protected SceneInitData sceneInitData = null;

        public bool IsLoadingComplete { get; protected set; } = true;
        
        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }
        protected virtual void OnUpdate() { }
        protected virtual void OnInitialize() { }

        protected ILoadingProgress LoadingProgress => SceneManager.CurrentTransition;
        
        public virtual void OnTransitionComplete(TransitionState state)
        {
        }

        public virtual void OnOpen()
        {
        }
        
        public virtual void OnOpened()
        {
        }
        
        public virtual void OnClose()
        {
        }

        public virtual void OnClosed()
        {
        }

        public virtual void SetSceneInitData(SceneInitData data)
        {
            sceneInitData = data;
            OnInitialize();
        }

        private void Awake()
        {
            OnAwake();
        }

        private void Start()
        {
            OnStart();
        }

        private void Update()
        {
            OnUpdate();
        }
    }
    
    public abstract class SceneBaseWithUI<T> : SceneBase, ISceneBaseWithUI where T : SceneUIBase
    {
        public T SceneUI { get; private set; }

        SceneUIBase ISceneBaseWithUI.SceneUI => SceneUI;

        public PopupTrack ScenePopupTrack => SceneUI.ScenePopupTrack;

        private void Awake()
        {
            SceneUI = FindFirstObjectByType<T>();
            SceneUI.CurrentScene = this;
            OnAwake();
            
        }

        public override void OnOpen()
        {
            if (EventSystem.current && EventSystem.current.currentInputModule is InputSystemUIInputModule m)
                m.cancel.action.performed += OnCancel;
        }

        public override void OnClose()
        {
            SceneUI.OnClose();
            if (EventSystem.current && EventSystem.current.currentInputModule is InputSystemUIInputModule m)
                m.cancel.action.performed -= OnCancel;
        }

        public override void SetSceneInitData(SceneInitData data)
		{
			base.SetSceneInitData(data);
            SceneUI.OnInitialize();
		}

		private void Update()
        {
            OnUpdate();
        }

        public void OnCancel(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Performed)
                return;
            
            Debug.Log("OnCancel performed!");
            SceneUI?.OnPressedBackButton();
        }
    }

    public abstract class SceneParam : ParamBase
    {
        public string TransitionName = "Default";
    }

    public abstract class SceneInitData : InitDataBase
    {
    }

    public abstract class SceneLoadingBlock : LoadingBlockBase<SceneParam, SceneInitData>
    {
    }

    public abstract class SceneLoadingBlock<Param, InitData> : LoadingBlockBase<Param, InitData>
        where Param : SceneParam
        where InitData : SceneInitData
    {
    }
}