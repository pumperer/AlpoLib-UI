using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using alpoLib.Data;
using alpoLib.Res;
using alpoLib.Util;
#if USE_ASSETBUNDLE
using AssetBundles;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using USM = UnityEngine.SceneManagement.SceneManager;

namespace alpoLib.UI.Scene
{
    public sealed class SceneManager : SingletonMonoBehaviour<SceneManager>
    {
        private bool isOpening = false;

        private static Stack<SceneBase> sceneStack = new();

        public static SceneBase CurrentScene => sceneStack.TryPeek(out var result) ? result : null;
        public static TransitionBase CurrentTransition { get; private set; }

        public static async Awaitable Initialize(SceneBase startScene)
        {
            Init(true);
            sceneStack.Push(startScene);
            while (Instance == null)
                await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();
            startScene.OnOpen();
        }

        private void BlockInput(bool block)
        {
            if (UIRoot.Instance == null)
                return;
            UIRoot.Instance.ActiveTransparentLoadingUI = block;
        }

        public async Awaitable<bool> OpenSceneAsync<T>(string path = "", SceneParam param = null) where T : ISceneBase
        {
            if (isOpening)
                return false;

            isOpening = true;
            BlockInput(true);
            var success = false;
            var type = typeof(T);
            if (type.GetCustomAttribute<SceneDefineAttribute>() is { } attr)
            {
                var sceneName = type.Name;
                var scenePath = attr.ScenePath;
                var sceneSubPath = attr.SubPath;
                var loadSceneMode = attr.LoadSceneMode;
                var resourceType = attr.ResourceType;

                if (string.IsNullOrEmpty(scenePath))
                    scenePath = type.Name;
                if(string.IsNullOrEmpty(sceneSubPath))
                    sceneSubPath = "ab/scenes";

                // if (resourceType == SceneResourceType.Addressable)
                // {
                //     if (!string.IsNullOrEmpty(path))
                //         scenePath = path;
                //     else if (type.GetCustomAttribute<PrefabPathAttribute>() is { } pathAttr)
                //         scenePath = pathAttr.Path;
                //     else
                //     {
                //         Debug.LogError($"어드레서블로 씬을 로드할 때는 path 파라메터를 전달하거나, PrefabPath 에 AddressablePath 가 지정되어야 합니다.");
                //         return false;
                //     }
                // }

                success = await OpenSceneAsync(type, scenePath, sceneSubPath, sceneName, loadSceneMode, resourceType, param);
            }

            isOpening = false;
            BlockInput(false);
            return success;
        }

        public async Awaitable<bool> UnloadCurrentSceneAsync()
        {
            if (isOpening)
                return false;

            isOpening = true;
            BlockInput(true);

            var scene = CurrentScene.GetType();
            var attr = scene.GetCustomAttribute<SceneDefineAttribute>();
            var sceneName = attr.ScenePath;
            if (string.IsNullOrEmpty(sceneName))
                sceneName = scene.Name;
            if (attr.LoadSceneMode != LoadSceneMode.Additive)
                return false;

            if (CurrentScene != null)
                CurrentScene.OnClose();

            var currentTransition = await ProcessTransitionInAsync(scene);

            // UnityEngine.SceneManagement.Scene sceneNext = default;
            // void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene1)
            // {
            //     sceneNext = scene1;
            // }
            //
            // USM.sceneUnloaded += OnSceneUnloaded;
            var operation = USM.UnloadSceneAsync(sceneName, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
            await operation;
            // while (!operation.isDone)
            //     await Awaitable.NextFrameAsync();

            if (!sceneStack.TryPop(out var prevScene))
            {

            }

            // if (FindObjectOfType(scene) is not SceneBase sceneObject)
            //     return false;

            await ProcessTransitionOutAsync(currentTransition);
            // USM.sceneUnloaded -= OnSceneUnloaded;

            isOpening = false;
            BlockInput(false);
            return true;
        }

        private async Awaitable<bool> OpenSceneAsync(Type scene, string scenePath, string sceneSubPath, string sceneName,
            LoadSceneMode loadSceneMode, SceneResourceType resourceType, SceneParam param = null)
        {
            if (CurrentScene != null && loadSceneMode == LoadSceneMode.Single)
                CurrentScene.OnClose();

            UnityEngine.SceneManagement.Scene nextScene = default;

            void OnSceneLoaded(UnityEngine.SceneManagement.Scene loadedScene, LoadSceneMode mode)
            {
                nextScene = loadedScene;
                Debug.Log($"SceneManager.OnSceneLoaded : {loadedScene.name} : {mode}");
            }

            CurrentTransition = await ProcessTransitionInAsync(scene, param?.TransitionName);
            if (CurrentScene != null)
                CurrentScene.OnTransitionComplete(TransitionState.In);

            while (!CurrentScene.IsLoadingComplete)
                await Awaitable.NextFrameAsync();
            
            GC.Collect();
            
            USM.sceneLoaded += OnSceneLoaded;
            switch (resourceType)
            {
                case SceneResourceType.Addressable:
                {
                    var sceneInstance = await AddressableLoader.LoadSceneAsync(scenePath);
                    nextScene = sceneInstance;
                    break;
                }
                case SceneResourceType.Default:
                {
                    var operation = USM.LoadSceneAsync(sceneName, loadSceneMode);
                    await operation;
                    // while (!operation.isDone)
                    //     await Task.Yield();
                    break;
                }
#if USE_ASSETBUNDLE
                case SceneResourceType.AssetBundle:
                {
                    var isAdditive = loadSceneMode == LoadSceneMode.Additive;
                    var operation = AssetBundleManager.LoadLevelAsync(sceneSubPath, sceneName, isAdditive, true);
                    while (!operation.IsDone())
                        await Awaitable.NextFrameAsync();
                    break;
                }
#endif
            }

            if (loadSceneMode == LoadSceneMode.Single)
            {
                if (!sceneStack.TryPop(out var prevScene))
                {
                    Debug.LogError($"SceneManager.OpenSceneAsync : scene stack 에 {prevScene.name} 이 없습니다.");
                    return false;
                }
            }

            if (loadSceneMode == LoadSceneMode.Additive)
                USM.SetActiveScene(nextScene);

            USM.sceneLoaded -= OnSceneLoaded;
            var newScene = USM.GetActiveScene();
            if (!newScene.isLoaded)
            {
                Debug.LogError($"SceneManager.OpenSceneAsync : 신규 {newScene.name} 씬이 로드가 아직 진행중입니까??");
                return false;
            }

            if (FindFirstObjectByType(scene) is not SceneBase sceneObject)
            {
                Debug.LogError($"SceneManager.OpenSceneAsync : {newScene.name} 오브젝트가 로드된 씬에 없습니다.");
                return false;
            }

            sceneStack.Push(sceneObject);
            var loadingBlockAttr = sceneObject.GetType().GetCustomAttribute<LoadingBlockDefinitionAttribute>();
            if (loadingBlockAttr != null)
            {
                var loadingBlockObj = Activator.CreateInstance(loadingBlockAttr.LoadingBlock);
                if (loadingBlockObj is SceneLoadingBlock loadingBlock)
                {
                    var initData = loadingBlock.MakeInitData(param);
                    sceneObject.SetSceneInitData(initData);
                }
                else if (loadingBlockAttr.LoadingBlock.BaseType.IsGenericType &&
                         loadingBlockAttr.LoadingBlock.BaseType.GetGenericTypeDefinition() == typeof(SceneLoadingBlock<,>))
                {
                    var method = loadingBlockAttr.LoadingBlock.GetMethod("MakeInitData", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null)
                    {
                        var initData = method.Invoke(loadingBlockObj, new object[] { param });
                        sceneObject.SetSceneInitData((SceneInitData)initData);
                    }
                }

            }
            else
                sceneObject.SetSceneInitData(null);

            sceneObject.OnOpen();

            while (!sceneObject.IsLoadingComplete)
                await Awaitable.NextFrameAsync();

            await ProcessTransitionOutAsync(CurrentTransition);

            CurrentScene.OnTransitionComplete(TransitionState.Out);
            CurrentTransition = null;

            return true;
        }

        private async Awaitable<TransitionBase> ProcessTransitionInAsync(Type scene, string transitionName = null)
        {
            TransitionBase currentTransition = null;
            var attrs = scene.GetCustomAttributes<SceneTransitionAttribute>().ToList();
            if (string.IsNullOrEmpty(transitionName) && attrs.Count > 0)
            {
                var attr = attrs.GetRandom();
                currentTransition = await FindTransitionAsync(attr.TransitionType);
            }
            else if (!string.IsNullOrEmpty(transitionName))
            {
                var targetAttr = attrs.Find(attr => attr.Name == transitionName);
                if (targetAttr != null)
                    currentTransition = await FindTransitionAsync(targetAttr.TransitionType);
            }

            if (!currentTransition)
                currentTransition = FindTransition(typeof(DefaultTransition));

            // while (currentTransition == null)
            //     await Task.Yield();

            // yield return new WaitUntil(() => currentTransition != null);

            var transitionComplete = false;
            if (currentTransition)
            {
                currentTransition.transform.localPosition = Vector3.zero;
                currentTransition.gameObject.SetActive(true);
                currentTransition.In(() => transitionComplete = true);
            }
            else
                transitionComplete = true;

            while (!transitionComplete)
                await Awaitable.NextFrameAsync();

            return currentTransition;
        }

        private async Awaitable ProcessTransitionOutAsync(TransitionBase currentTransition)
        {
            var transitionComplete = false;
            if (currentTransition != null)
                currentTransition.Out(() =>
                {
                    transitionComplete = true;
                    currentTransition.gameObject.SetActive(false);
                });
            else
                transitionComplete = true;

            while (!transitionComplete)
                await Awaitable.NextFrameAsync();
        }

        private TransitionBase FindTransition(Type transitionType)
        {
            var transitions = UIRoot.Instance.TransitionCanvas.GetComponentsInChildren<TransitionBase>(true);
            if (transitions.Length == 0)
            {
                return GenericPrefab.InstantiatePrefab(transitionType, parent: UIRoot.Instance.TransitionCanvas) as
                    TransitionBase;
            }

            var target = transitions.FirstOrDefault(t => t.GetType() == transitionType);
            if (target == null)
            {
                return GenericPrefab.InstantiatePrefab(transitionType, parent: UIRoot.Instance.TransitionCanvas) as
                    TransitionBase;
            }
            else
                return target;
        }

        private async Awaitable<TransitionBase> FindTransitionAsync(Type transitionType)
        {
            var transitions = UIRoot.Instance.TransitionCanvas.GetComponentsInChildren<TransitionBase>(true);
            if (transitions.Length == 0)
            {
                return await GenericPrefab.InstantiatePrefabAsync(transitionType,
                    parent: UIRoot.Instance.TransitionCanvas) as TransitionBase;
            }
        
            var target = transitions.FirstOrDefault(t => t.GetType() == transitionType);
            if (target)
                return target;
            
            return await GenericPrefab.InstantiatePrefabAsync(transitionType,
                    parent: UIRoot.Instance.TransitionCanvas) as TransitionBase;
        }
    }
}