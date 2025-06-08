# AlpoLib UI

## UIRoot
- 앱 전반적으로 살아있는 Global UI Root 입니다.
- Unity Scene 이 전환되더라도 항상 살아 있습니다.
- Global Popup, Transition, Transparent Blocker, Loading UI 등을 표시합니다.

## Scene Manager
- 유니티 씬과 클래스를 1:1로 매칭하여 관리하는 방식입니다.
- 게임을 시작하는 최초 씬 클래스를 SceneBase를 상속 받아 선언합니다.
- 클래스 속성으로 SceneDefine 을 추가합니다.
```cs
[SceneDefine]
public class StartUpScene : SceneBase
{
    private void Awake()
    {
        // Awake 에서 SceneManager 를 초기화 합니다.
        // 처음 씬을 알리기 위해 this 를 인자로 보내야 합니다.
        SceneManager.Initialize(this);
    }
}
```
- 해당 클래스를 **같은 이름의 씬** 에 부착합니다. : StartUpScene.unity 내 임의의 게임 오브젝트에 add component
- SceneDefine 생성자는 여러가지가 있습니다. 다른 이름의 씬으로 쓰고 싶으면, 클래스를 참조하세요.
- 이렇게 또 다른 씬을 생성하고, 아래와 같이 씬을 로드할 수 있습니다.
```cs
SceneManager.Instance.OpenSceneAsync<MyScene>();
```

## SceneUI System
- SceneBase는 UI가 아닌 부분에 대한 로직을 담당하고, UI 부분은 SceneUIBase 에 분리하자는 취지로 만들었습니다.
 - UI 관련 로직이 없을 수도 있으므로, 선택적입니다.
- 위의 StartUpScene에서 사용할 SceneUI를 생성한다고 가정하고, SceneUIBase 를 상속받아 클래스를 생성합니다.
```cs
public class StartUpSceneUI : SceneUIBase
{
        public void Hello()
		{
		}
}
```
- 해당 클래스를 StartUpScene의 UI로 사용할 Canvas 가 붙어있는 게임 오브젝트에 부착합니다.
- StartUpScene 클래스 정의시, SceneBase 가 아닌 SceneBaseWithUI<StartUpSceneUI> 를 상속받아 SceneUIBase 를 알 수 있게 합니다.
```cs
[SceneDefine]
public class StartUpScene : SceneBaseWithUI<StartUpSceneUI>
{
    protected override void OnAwake()
    {
        // 최초 진입 씬입니다.
        // Awake 에서 SceneManager 를 초기화 합니다.
        // 처음 씬을 알리기 위해 this 를 인자로 보내야 합니다.
        // 다음 씬 부터는 OnOpen 이 호출됩니다.
        SceneManager.Initialize(this);
    }

    public override void OnOpen()
    {
        SceneUI.Hello();
    }
}
```
- 해당 씬 클래스 내부에서 SceneUI 멤버를 통해 UI 에 접근할 수 있습니다.

## Popup System
- 팝업을 관리하는 시스템 전반적인 내용입니다.

### PopupBase, DataPopup
- PopupBase : 기본 팝업
```cs
[PrefabPath(ADDR_PATH)]
public class TestPopupBase : PopupBase
{
}

var popup = await PopupBase.CreatePopupAsync<TestPopupBase>();
popup.Open();
```
- PopupData : 데이터를 가공하여 전달하는 팝업
	- PopupParam
 	- PopupInitData
  	- PopupLoadingBlock<Param, InitData>
  	- LoadingBlockDefinitionAttribute
```cs
[PrefabPath(ADDR_PATH)]
[LoadingBlockDefinition(typeof(TestPopupLoadingBlock)]
public class TestDataPopup : DataPopup<TestPopupParam, TestPopupInitData>
{
    protected override void OnOpen()
    {
        base.OnOpen();
        Debug.Log(InitData.IntInitValue1);
    }
}

public class TestPopupParam : PopupParam
{
    public int IntParamValue1;
}

public class TestPopupInitData : PopupInitData
{
    public int IntInitValue1;
}

public class TestPopupLoadingBlock : PopupLoadingBlock<TestPopupParam, TestPopupInitData>
{
    public override TestPopupInitData MakeInitData(TestPopupParam param)
    {
        return new TestPopupInitData
        {
            IntInitValue1 = param.IntParamValue1
        };
    }
}
```
```cs
var popup = await PopupBase.CreatePopupAsync<TestDataPopup>();
popup.Initialize(new TestPopupParam { IntParamValue1 = 123 });
popup.Open();
```
- CreatePopupAsync : 비동기 로드
- CreatePopup : 동기 로드

### PopupTrack
- 팝업이 열린 순서를 저장하고, 전후 관계를 자동으로 관리해 줍니다.
- PopupBase.Open() 내에서 적절한 Track 을 찾아서 넣습니다.
- 최소한 SceneBaseWithUI를 상속받은 Scene 또는, UIRoot 가 필요합니다.

## Transition
- Unity Scene 전환간 UIRoot 에 표시할 Transition 입니다.
- In, Out 애니메이션을 가져야 합니다.
- SceneBase 에서 OnTransitionComplete 인터페이스를 통해 트랜지션 완료 여부를 알 수 있습니다.
- loading progress 를 표시할 수 있습니다.
