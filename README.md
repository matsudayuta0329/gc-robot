# gc-robot

掃除機ロボットを操作し、制限時間内にごみを集めるゲームです。移動、旋回、吸引、ダッシュを使い、回収数を競います。非同期処理にはUnityのAwaitable、文字表示にはTextMesh Proを使用します。

## ゲームルールと操作

当初のゲーム構想は、制限時間内にできるだけ多くのごみを回収し、**ごみの種類に応じた1〜3点の固有得点と回収個数**で最終スコアを競うものです。回収に必要な吸引量もごみごとに異なります。操作は前後移動・旋回、吸引、ダッシュが中心で、チュートリアルではこれらとゲームルールを順に体験します。

現在のゲームプレイ処理は次のとおりです。

- `GameManager`が開始演出後に操作を有効化し、`limit`秒の間、`trashObjects`からごみPrefabと`Size`（1〜3）を選んで、設定された範囲へ一定間隔で生成します。生成数は`maxGenCount`で制限します。
- 吸引中は`Garbage.Drain`が回収進行度を減らし、所定の割合まで減ると回収します。ダッシュ中は接触範囲のごみを直接回収します。回収時の`Garbage.Score`は設定された`Size`に対応します。
- `GarbageCounter`は得点用の値`count`と回収個数`CollectedCount`を別々に記録します。回収すると個数が1増え、`count`には1〜3点が加算されます。ダッシュ開始時には`count`から5を消費します。
- 制限時間終了後は操作を止め、`ResultUI`に**回収個数**を表示します。当初構想の「固有得点と回収個数を組み合わせた最終スコア」の算出・表示は現行コードにはありません。

### ダッシュと壁への衝突

ダッシュは約0.2秒のチャージ中に移動を止め、**モデルだけ**を通常の`(1, 1, 1)`から拡大率`(10.5, 1.7, 1.5)`へ徐々に変化させます。コライダーのあるルートは拡縮しません。移動終了後は通常最高速度より大きい初速から減速し、モデルも速度に合わせて通常スケールへ戻ります。値は`Player`のシリアライズフィールドから変更できます。

斜めに壁へ当たった場合、ダッシュの実移動は壁面へ投影して壁沿いに進めます。ダッシュの進行距離`dashedLength`には、投影後の実移動量ではなく、そのフレームに要求した進行距離を加算します。これにより壁沿いに進んでもダッシュ時間が不自然に延びません。ほぼ正面から当たり壁沿いの方向が取れない場合はダッシュを打ち切ります。斜めの衝突後は壁沿いの方向をダッシュ後の余速にも引き継ぎます。

## シーンの流れ

| シーン | Manager | 終了条件と遷移 |
| --- | --- | --- |
| Title | TitleManager | スタートボタン → タイトル演出のAnimation Event → Tutorial |
| Tutorial | TutorialManager | 登録された説明・操作体験が終了 → nextScene演出 → lobbyScene |
| Lobby | LobbyManager | ゲート接触またはNPCへの話しかけ → 確認ポップアップ → はいならnextScene演出 → 対象のmoveScene |
| Game | GameManager | 制限時間終了 → endGame演出 → ResultUIのClose待ち → nextScene演出 → lobbyScene |

各Managerがゲーム進行とシーン移動を担当し、UIParentは演出順だけを担当します。終了時はプレイヤーとゲーム用InputActionを停止してから演出を開始します。UIボタンの入力はEventSystemが処理します。

## 処理フローと設計

1. Titleでは`TitleManager`がクレジットと開始ボタンを扱い、開始演出のAnimation EventからTutorialを読み込みます。
2. Tutorialでは`UIParent.Enable`の開始演出が終わるまで操作を止めます。`TutorialManager`は登録順に会話と操作体験を進めます。会話中は入力と操作ガイドを止め、操作体験中は対象の`ActionType`だけを有効にします。すべて終わると`UIParent.FinishAsync`の`nextScene`演出を経てLobbyへ進みます。
3. LobbyではNPCへの話しかけ、またはゲートへの接触で同じ確認処理を呼びます。ポップアップ表示から非表示完了までプレイヤー入力を止め、「いいえ」なら復帰、「はい」なら`nextScene`演出後に対象シーンへ移ります。同時に複数の確認は開きません。
4. Gameでは開始演出後に操作を解放し、制限時間中にごみを生成します。吸引またはダッシュで回収した数を記録し、時間切れで操作を止めます。`endGame`演出でResultUIを表示し、Closeを待ってから`nextScene`演出でLobbyへ戻ります。

入力は`InputReader`が`ActionType`ごとに有効化します。`Player`はUnityの更新処理と入力イベントを受け持ち、移動・ダッシュを`PlayerMovement`、吸引・回収を`PickupGarbage`へ委譲します。`GarbageCounter`は回収個数と得点用の値を別々に保持します。現在のResultUIが表示する値は回収個数です。

UIは進行役のManager、演出順を待機する`UIParent`、表示とボタンを持つView、値の更新や回答待機を担うPresenterに分けています。開始・終了の見た目はAnimationClipとAnimatorが担当し、`UIAnimatorOverrides`は起動時にシリアライズされたClipを共通Controllerへ割り当てます。数値表示の更新は`UIParent`を通しません。非同期の待機にはUnityの`Awaitable`を使用します。

## ディレクトリと主要プログラム

以下は実装に関わる主なファイルです。Unityが生成する`.meta`、素材の個別ファイル、`Library`と`Temp`は省略しています。

```text
gc-robot/
├─ README.md                         ゲーム概要、構成、設定・検証手順
├─ Assets/
│  ├─ Scenes/
│  │  ├─ Title.unity                  メニューとクレジット
│  │  ├─ Tutorial.unity               会話と操作練習
│  │  ├─ Lobby.unity                  NPCとゲートから遷移する拠点
│  │  └─ Game.unity                   制限時間内にごみを回収するゲーム本編
│  ├─ Scripts/
│  │  ├─ TitleManager.cs             開始ボタン・クレジット・Tutorial遷移
│  │  ├─ TutorialManager.cs          会話／操作体験の順序とLobby遷移
│  │  ├─ LobbyManager.cs             確認中の入力停止・排他制御・シーン遷移
│  │  ├─ GameManager.cs              ごみ生成・時間管理・結果表示・Lobby遷移
│  │  ├─ InputReader.cs              InputActionをActionType別に管理
│  │  ├─ ActionType.cs               Move、Dash、Vacuumなどの操作種別
│  │  ├─ NPC.cs                      会話ガイドと確認処理の起動
│  │  ├─ Gate.cs                     接触判定から確認処理を起動
│  │  ├─ Garbage.cs                  ごみの吸引進行・回収演出・固有点
│  │  ├─ PlayerCamera.cs             プレイヤーを追従し範囲内に収めるカメラ
│  │  ├─ Player/
│  │  │  ├─ Player.cs                 Unity更新処理、入力登録、NPCとの対話
│  │  │  ├─ PlayerMovement.cs         移動・旋回・ダッシュ・壁沿い移動・余速
│  │  │  ├─ PickupGarbage.cs          吸引／ダッシュ回収と記録
│  │  │  └─ GarbageCounter.cs         回収個数と得点用の値、更新通知
│  │  ├─ IUIElement.cs               UI共通のEnable／Disable契約
│  │  ├─ AnimatedUIElement.cs        通常UIのAnimator制御と完了待機
│  │  ├─ UIParent.cs                 シーン単位の開始・終了演出とResult待機
│  │  ├─ UIAnimatorOverrides.cs      共通ControllerのClipを起動時に差し替え
│  │  ├─ GameUI.cs / GameUIPresenter.cs       時間・得点用の値の表示
│  │  ├─ TutorialUI.cs / TutorialUIPresenter.cs  会話・ガイド・回収表示の統括
│  │  ├─ MessageWindow.cs / MessageWindowPresenter.cs  文字送りと回答待機
│  │  ├─ OperationGuideUI.cs / OperationGuidePresenter.cs  操作ガイド
│  │  └─ ResultUI.cs / ResultUIPresenter.cs   結果の回収数とClose待機
│  ├─ UIAnimations/
│  │  ├─ UIElement.controller         通常UIのBedoreEnable／Enable／Idle／Disable
│  │  ├─ UIParent.controller          startGame／endGame／nextSceneの状態遷移
│  │  ├─ Enable.anim / Disable.anim   通常UIの表示・非表示テンプレート
│  │  ├─ StartGame.anim / EndGame.anim / NextScene.anim  シーン演出
│  │  └─ IdleTransition.anim          待機中のTransitionを非表示に保つClip
│  ├─ Editor/
│  │  ├─ UIAnimationSetup.cs          Clip／Controllerの生成と選択UIへの割当て
│  │  └─ UIFlowSceneSetup.cs          シーンのUI・参照・ゲートなどの初期配置
│  ├─ SRC/                            プレイヤー、ごみ、マップのPrefabと素材
│  ├─ PlayerInputAction.inputactions  Input Systemのアクション定義
│  └─ PlayerInputAction.cs            上記アクション定義から生成されたC#コード
└─ Tests/
   ├─ GameAreaTests.cs               移動・回収などのPlayModeテスト
   ├─ UIFlowTests.cs                  Animator・UI・入力制御のPlayModeテスト
   ├─ SceneFlowTests.cs               実シーン間の結合テスト
   └─ Run-*.ps1                       テスト用Unityプロジェクトの準備と実行
```

## UIParent

CanvasにUIParentとAnimatorを付けます。elementsへ通常UIを登録し、結果表示が必要なGameだけresultUIも設定します。ResultUIはelementsへ重複登録する必要はありません。各UIElementはParentとは別のGameObjectに配置します。UIParentのstartGameAnimation、endGameAnimation、nextSceneAnimationへ使用するAnimationClipを設定します。

- Enable(token)：登録UIを先頭姿勢に準備し、startGame演出と子UIのEnable完了まで待機します。
- FinishAsync(token)：ResultUIがあればendGame演出とCloseを待ち、その後nextScene演出を待ちます。ResultUI未設定なら直接nextSceneへ進みます。
- UIParentはシーン名や残り時間、ごみ数を保持しません。

AnimatorのTrigger名は大文字・小文字を含めてstartGame、endGame、nextSceneです。ParentのAnimation Eventは次を使用します。

| アニメーション | 途中のEvent | 終端のEvent |
| --- | --- | --- |
| StartGame | EnableElements | CompleteStart |
| EndGame | DisableElements、EnableResult | CompleteEnd |
| NextScene | DisableElements、DisableResult | CompleteNextScene |

ResultUIのEnableResultは一度だけ有効です。結果画面の回答はEnable時にリセットされるため、WaitForCloseより先にCloseが押されても取りこぼしません。Parentの完了後も子UIに進行中の遷移があればその完了を待ちます。

Animatorなしでは演出を即時完了します。Animatorありで完了イベントが設定されていない場合はanimationTimeout（既定30秒）後に例外を通知し、勝手に次のシーンへ進みません。ユーザーの回答待ちにはタイムアウトを設けていません。

## 通常UIのAnimator

AnimatedUIElementを共通基底とし、GameUI、TutorialUI、OperationGuideUI、MessageWindow、ResultUIで同じ構成を使用します。各コンポーネントのenableAnimation、disableAnimationへAnimationClipを設定します。共通のUIElement.controllerをAnimatorへ割り当てておけば、起動時に各コンポーネントがメモリ上のAnimatorOverrideControllerを作成し、自身のAnimatorへ設定します。UIParentも共通のUIParent.controllerから同様に作成します。OverrideアセットをUIごとに作る必要はありません。Clipを未設定にした箇所は共通Controllerの元Clipを使用します。

| State | Motion | Speed |
| --- | --- | --- |
| BedoreEnable | Enableと同じClip | 0 |
| Enable | 表示Clip | 1 |
| Idle | なし | 1 |
| Disable | 非表示Clip | 1 |

BedoreEnableは指定の綴りを使用しています。Triggerはenableとdisableです。Enable終了時にCompleteEnable、Disable終了時にCompleteDisableを呼ぶAnimation Eventを配置します。AnimatorとUIElementは同じGameObjectに置き、rootが別の場合もAnimatorを含むオブジェクトを演出中に無効化しないでください。

遷移はBedoreEnable → Enable → Idle → Disable → BedoreEnableで、遷移時間は0です。Enable途中からDisableへの割り込みにも対応します。IdleのMotionは空にし、全StateのWrite DefaultsをOFFにして表示姿勢を保持します。変更するプロパティはEnable/Disable双方でキーを設定してください。

PrepareEnableはオブジェクトを有効化し、BedoreEnableを時刻0で即時評価します。Enable前とDisable中はCanvasGroupのinteractable/blocksRaycastsをOFFにします。通常UIのEnable完了後に操作を許可し、Disable完了後にrootを非アクティブにします。AnimatorはUnscaledTimeで動作します。

Assets/UIAnimationsにフェードの動作テンプレートを同梱しています。ParentのClipは直下のTransitionという名前のCanvasGroupをアニメーションします。
Transitionはシーン上で非アクティブに設定します。StartGameは先頭で有効化してフェードし、終了直前にアニメーションのキーで非アクティブへ戻します。ParentのIdleにはTransitionの非アクティブと透明だけを記録したIdleTransitionを割り当て、状態遷移後も黒い画像を表示させません。

Editorメニュー：

- GC Robot / UI / Create Animation Templates：未作成のClipとControllerを生成します。既存アセットは上書きしません。
- GC Robot / UI / Assign Templates To Selected Hierarchy：選択階層のUIElementとUIParentへAnimatorを割り当てます。通常UIにはCanvasGroupも付与します。
- GC Robot / UI / Connect Scene Flows：Gameの既存UIを接続し、未設定のLobby/Tutorialに基本構成を作成します。ゲーム側のUIParent登録は既存GameUI/ResultUIへ再設定されます。

演出を変更するときは各UIのClipを差し替え、完了イベントを実際の終端へ設定してください。差し替えたClipには元ClipのAnimation Eventが自動継承されません。待機Stateに流用するEnableの時刻0には、処理を実行するAnimation Eventを置かないでください。

## ViewとPresenter

各ViewはTMPやButtonなどの参照を保持し、Presenterが表示更新・購読・確認の進行を担当します。Presenterは通常のC#クラスで、ViewのPresenterプロパティから取得します。

- GameUIPresenter：GarbageCounterを購読して数値を更新し、Managerから受け取った残り時間を表示します。
- TutorialUIPresenter：ごみ数表示、MessageWindowPresenter、OperationGuidePresenterをまとめます。
- MessageWindowPresenter：会話の文字送り、確認の表示・回答待機・非表示を行います。
- OperationGuidePresenter：操作説明と表示対象ActionTypeを設定します。
- ResultUIPresenter：結果の回収数を設定します。

ゲーム中の数値はUIParentを経由しません。GameManagerはgameUI.Presenter.SetTime、resultUI.Presenter.SetResultを使用します。GarbageCounterへの購読はManager終了時に解除します。

## 確認ポップアップとResultUI

ResultUIにはEnable、Disable、WaitForClose(token)、Closeがあります。ボタンはCloseを呼びます。既存ボタンのConfirm呼び出しも互換用に残しています。

ロビーの確認は以下の一回の呼び出しで、表示から回答、非表示アニメーション完了まで待機します。

```csharp
bool accepted = await popup.Presenter.ShowAndWaitAsync(message, cancellationToken);
```

C#のasyncメソッドではout/ref引数を使えないため、boolはAwaitable<bool>の戻り値で受け取ります。「はい」はtrue、「いいえ」はfalseです。キャンセル・破棄はOperationCanceledExceptionとして扱い、回答と混同しません。

## ロビーのNPCとゲート

LobbyManagerへPlayer、InputReader、UIParent、MessageWindowを設定します。ロビーのUIParentはResultUIを空にします。

- NPC：message、moveScene、guide、lobbyManagerを設定します。Playerは付近のNPCへInteract入力で話しかけます。PlayerのnpcLayerにNPCのレイヤーを含めます。
- Gate：Is Triggerを有効にしたColliderとGateを同じオブジェクトに設定し、lobbyManager、message、moveSceneを指定します。CharacterControllerとの検出用にKinematic Rigidbodyを付けた配置を同梱しています。
- Gateは登録Playerだけに反応します。複数Colliderからの重複入場は抑制し、「いいえ」の直後に接触中のまま再表示しません。再表示には一度外へ出て再入場します。
- NPCとGateは同じLobbyManagerの排他制御を使用します。確認中の別リクエストは受け付けません。
- 確認表示から非表示完了まで操作を停止します。「はい」の場合は遷移演出中も停止を維持し、「いいえ」やキャンセルなら元の入力マスクと一時停止状態に復帰します。

同梱Lobbyでは既存ゲートの外観に接触領域を追加し、チュートリアルへ移動するNPCを基本形状で配置しています。Tutorialは動作確認用の説明・移動・ダッシュ・吸引のデータを含みます。文言、操作ガイドのActionType別オブジェクト、NPCの外観、ゲートの範囲はInspectorで調整できます。Game/Lobby/TutorialはBuild Settingsに登録済みです。

## Player関連

- Player：Unityライフサイクル、入力登録、PlayerMovement/PickupGarbageへの値の注入と呼び出し、NPCへの話しかけ。
- PlayerMovement：移動、旋回、ダッシュ、壁沿い移動、ダッシュ後の余速とモデル拡縮。
- PickupGarbage：吸引・ダッシュ中のごみ検出と回収、GarbageCounterへの記録、吸引マテリアル演出。

## 検証

```powershell
powershell -ExecutionPolicy Bypass -File Tests/Run-GameAreaTests.ps1
```

Temp/GameAreaValidationへコードとAnimatorをコピーし、Unity 6000.3.9f1のバッチモードでPlayModeテストを実行します。元シーンを開いたEditorとは別プロジェクトです。結果はTemp/game-area-results.xml、ログはTemp/game-area-tests.logです。

既存のダッシュ・壁沿い余速・ごみ回収に加え、先頭姿勢の保持、実Animatorの完了待機、ResultUIなしの終了演出、結果の早期Closeと再表示、ポップアップ回答・キャンセル、ロビーの入力停止と排他制御を確認します。

実シーンの結合テストはTests/Run-SceneFlowTests.ps1で実行します。Temp/SceneFlowValidationにプロジェクトをコピーし、Gameの結果画面からLobby、ゲートの物理接触・「はい」からGame、Tutorialの終了からLobbyまで検証します。結果はTemp/scene-flow-results.xmlです。テスト内ではゲーム時間を短縮し、チュートリアルのAdvanceを自動実行します。

新しい日本語UIにはUIAnimations/UIJapanese.assetを使用します。同梱のDotGothic16を元にした動的TMPフォントで、Inspectorで文言を変更した場合も必要な文字を追加できます。
