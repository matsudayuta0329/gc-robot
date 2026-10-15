# gc-robot

掃除機ロボットを操作し、制限時間内にごみを集めるゲームです。移動、旋回、吸引、ダッシュを使い、回収数を競います。非同期処理にはUnityのAwaitable、文字表示にはTextMesh Proを使用します。

## シーンの流れ

| シーン | Manager | 終了条件と遷移 |
| --- | --- | --- |
| Tutorial | TutorialManager | 登録された説明・操作体験が終了 → nextScene演出 → lobbyScene |
| Game | GameManager | 制限時間終了 → endGame演出 → ResultUIのClose待ち → nextScene演出 → lobbyScene |
| Lobby | LobbyManager | ゲート接触またはNPCへの話しかけ → 確認ポップアップ → はいならnextScene演出 → 対象のmoveScene |

各Managerがゲーム進行とシーン移動を担当し、UIParentは演出順だけを担当します。終了時はプレイヤーとゲーム用InputActionを停止してから演出を開始します。UIボタンの入力はEventSystemが処理します。

## UIParent

CanvasにUIParentとAnimatorを付けます。elementsへ通常UIを登録し、結果表示が必要なGameだけresultUIも設定します。ResultUIはelementsへ重複登録する必要はありません。各UIElementはParentとは別のGameObjectに配置します。

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

AnimatedUIElementを共通基底とし、GameUI、TutorialUI、OperationGuideUI、MessageWindow、ResultUIで同じ構成を使用します。

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

Editorメニュー：

- GC Robot / UI / Create Animation Templates：未作成のClipとControllerを生成します。既存アセットは上書きしません。
- GC Robot / UI / Assign Templates To Selected Hierarchy：選択階層のUIElementとUIParentへAnimatorを割り当てます。通常UIにはCanvasGroupも付与します。
- GC Robot / UI / Connect Scene Flows：Gameの既存UIを接続し、未設定のLobby/Tutorialに基本構成を作成します。ゲーム側のUIParent登録は既存GameUI/ResultUIへ再設定されます。

演出を変更するときはClipを編集し、完了イベントを実際の終端へ移動してください。待機Stateに流用するEnableの時刻0には、処理を実行するAnimation Eventを置かないでください。

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
