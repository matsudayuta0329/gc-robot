# gc-robot

##　ゲーム概要

掃除機のロボットを動かして制限時間内にごみをできる限り回収
回収したごみの種類による固有得点(回収にかかる回数によって1-3まで)と回収した個数で決まる最終スコアを競う

### ゲームシーン
ゲームプレイとチュートリアル、ロビーシーンの3つがあり、
ゲームメニュー⇒チュートリアル⇒ゲームロビー(チュートリアルとゲームどちらでも移動可能)のロビーシーンと
ゲームプレイシーンがある

- #### ゲームメニュー
クレジットとゲームスタートボタンがある
- #### チュートリアル
移動、ごみ回収、ダッシュ、ゲームルールについてのチュートリアル
操作説明やセリフが入る

- #### ゲームロビー
店員のNPCがおり、ゲームの開始と、チュートリアルの再受講ができる

- #### ゲーム
上記のゲームの実行　開始演出、リザルト画面、終了演出がある

## 実装内容
非同期はAwaitble、UIでのテキストはTMPを使用する

### 型など
- #### Enum ActionType
Move,Dash,Vacuumなど操作の種類

### 全般
- #### IUIElemnt
Enable,Disableの公開関数を要求

- #### MessageWindow: IUIElement
EnableDisableにはアニメーターの表示、非表示アニメーション用のトリガーを有効化+表示切替(SetActiveなどで)
Awaitable SetText(string) 入れられたテキストをAwaitbleを使用して頭から一文字ずつ表示する
DisplayFullText() SetTextで入れられたテキストの表示処理を即時に完了する(非同期処理を終了し渡されていたテキストをそのまま設定する)

- #### InputReader
List<ActionType, InputAction>のシリアライズ変数を提供
開始時にディクショナリに変換
SetEnableAction(ActionType[]) 引数にあるアクションタイプに対応するInputActionのみを有効化しそれ以外は無効化
nullの場合はすべて有効、から配列の時はすべて無効化


- #### TrashCount: IUIElement
EnableDisableにはアニメーターの表示、非表示アニメーション用のトリガーを有効化+表示切替(SetActiveなどで)
SetTrashNum(int) 空関数

### タイトル
- #### TitleManager
クレジットのクローズ、オープン用のボタンハンドラ関数
ゲーム開始の公開関数+ゲーム開始ボタンクリック時にアニメーターのトリガーをオンにするボタンハンドラ関数

### チュートリアル
- #### TutorialManager
会話とチュートリアルデータの設定用のシリアライズ変数(bool isTutorial, string message, ActionType tutorialAction)
チュートリアルデータを基にTutorialUIの公開ゲッターを使用し、
isTutorialがfalseの時はメッセージ表示、trueの時は移動の有効化と操作ガイドの表示


- #### OperationGuideUI;
List<ActionType, GameObject>のシリアライズ変数を提供　開始時にディクショナリに変換
EnableDisableには表示切替(SetActiveなどで)
SetGuideEnable(ActionType[]) ActionTypeに対応する画像を表示　それ以外は非表示
nullの場合はすべて有効、から配列の時はすべて無効化

- #### TutorialUI: IUIElement
EnableDisableにはアニメーターの表示、非表示アニメーション用のトリガーを有効化+表示切替(SetActiveなどで)
MessageWindow、TrashCount、OperationGuideをシリアライズ変数として提供
上記変数のゲッターを提供


### ロビー


### ゲームエリア

- #### Player
Unityのライフサイクルに合わせて入力イベントを登録し、移動・ダッシュ・ごみ回収・NPC操作を実行する。`PlayerMovement`と`PickupGarbage`の生成およびパラメータの受け渡しも担当する。

- #### PlayerMovement
通常移動、旋回、ダッシュ、壁に接触したときのスライド、ダッシュ後の余速とモデルの拡縮を担当する。

- #### PickupGarbage
吸引中またはダッシュ中に範囲内のごみを検出して回収し、回収数と得点を`GarbageCounter`へ記録する。吸引中のマテリアル演出も担当する。
