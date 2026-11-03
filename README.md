# gc-robot

掃除機ロボットを操作し、60秒以内にごみを集めて得点を競うゲームです。通常の吸引ではごみを少しずつ縮め、ダッシュ中は範囲内のごみを直接回収します。ダッシュには得点を使うので、消費した分を取り戻せるだけのごみを狙うのがポイントです。

## ゲームルールと操作

- ごみの種類に応じて回収ごとに1〜3点を加算します。ダッシュ1回は5点を消費し、残高が足りなければ開始できません。
- 結果画面とランキングの値は時間切れ時点の所持得点です。たとえば3点のごみを2個回収してダッシュを1回使うと、残りは1点です。`GarbageCounter.CollectedCount` は `count` と同じ値を返します。
- ダッシュは約0.2秒のチャージ中に移動を止め、モデルを変形してから移動します。斜めに壁へ当たると実移動は壁面へ投影し、進行距離にはそのフレームに要求した距離を加算します。ほぼ正面で進めない場合は終了し、斜めなら壁沿いの方向を余速に引き継ぎます。
- 前後移動と旋回は `Move`、吸引は `Vacuum`、ダッシュは `Dash`。NPCへのインタラクトは既定でEまたはゲームパッドの南ボタンです。シーンごとに `InputReader.SetEnableAction` で許可する操作を指定します。引数がnullなら全操作、空配列ならゲーム操作を無効にします。

## シーンと動作フロー

ビルド設定の先頭は **Tutorial** です。基本の経路は **Tutorial → Lobby → Game → Lobby**。ロビーからチュートリアルを再受講できます。

| シーン | 開始から終了まで |
| --- | --- |
| Tutorial | 開始フェード → 会話 → 前後移動・旋回 → 吸引 → ダッシュ → 最後の会話 → 遷移フェード → Lobby。説明は複数行設定でき、会話中は操作を停止します。移動課題では前進・後退・旋回をそれぞれ一定時間入力。吸引課題では登録種類ごとに出したごみをすべて回収し、ダッシュ課題では10個を配置してダッシュの開始と終了を確認します。 |
| Lobby | 開始演出後に操作を復帰。NPCは会話を表示し、ゲートは接触時に確認ダイアログを開きます。「いいえ」は操作復帰、「はい」は遷移演出後に各ゲートのGameまたはTutorialへ移動。会話・確認中はプレイヤー操作と二重起動を抑制します。 |
| Game | 開始演出 → 操作解放 → 60秒間のプレイ → 入力停止 → 結果表示 → 終了ボタンで送信を呼び出す → 遷移フェード → Lobby。1秒間隔で種類と生成位置を選びます。現在の条件はごみの親の子数が15以下のときなので、15個の時点でもう1個生成できます。 |

プレイ中は **GarbageCounter → Presenter → TMP表示** の順で値を更新します。回収成立時には回収音も鳴ります。時間切れでは **GameManager → ResultUIPresenter → ResultUI** の順で得点を渡し、UIParentが終了演出と閉じる入力を待ちます。TutorialとLobbyではResultUIを経由せず、直接nextSceneを再生します。演出が終わってシーンを読み込むのは各Managerの役割です。

## unityroomランキング

GameシーンのResultUIにある **Unityroom Board No** は既定1。WebGL版で結果画面を閉じると整数の最終得点を一度送信します。ボードは整数・降順・単位なしで、記録ルールは `HighScoreDesc` です。HMAC認証用キーの入力場所は **Assets/Resources/UnityroomRankingClient.prefab → Unityroom Api Client → Hmac Key**。ランキングはunityroomで表示します。

## ディレクトリと調整場所

| 場所 | 内容 |
| --- | --- |
| `Assets/Scenes/` | Tutorial・Lobby・Gameのシーン本体。時間、ごみの生成範囲、ゲートの行き先、会話と操作の順番を設定。 |
| `Assets/Scripts/Player/` | Unityの入力・更新の窓口、移動、回収、所持得点、吸引・移動演出。 |
| `Assets/Scripts/` | シーン進行、ViewとPresenter、音声、ポストエフェクト、ランキング送信。 |
| `Assets/SRC/` | Player・ごみ・吸引粒子のプレハブとゲーム素材。 |
| `Assets/SRC/Sound/` | 音源とPreparedの再生用クリップ。移動・吸引はループ、回収とダッシュは単発。 |
| `Assets/UIAnimations/` | UIの共通Animatorとシーン別AnimationClip。 |
| `Assets/Shaders/`・`Assets/Settings/` | 吸引粒子、移動演出、テレビ風ポストエフェクトとURP設定。 |
| `Assets/Editor/`・`Tests/` | UIセットアップ用のEditorツールと検証スクリプト。 |

吸引演出はPlayerのPort配下の **Vacuum Particles** を選択すると、Sceneビューでプレビューできます。広い面から吸引口へ収束し、吸引停止後も残った粒子は寿命に沿ってフェードします。移動演出は左右のタイヤ跡と煙です。

移動・吸引音は入力開始／停止に合わせてフェードします。ダッシュ音はチャージが終わって動き始めた時、回収音は得点を獲得した時に鳴ります。BGMは各シーンで2Dループ再生し、開始・退出演出に合わせて音量を変えます。リザルト待機中は通常音量です。

テレビ風表示はカメラの **Rendering → Post Processing** を有効にし、**TV Post Effect** を追加します。Volumeの **Add Override → Post-processing → TV Post Effect** からも設定できます。対象は走査線と色収差です。

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

UIParentのEnableResultは一度だけ有効です。結果画面の回答はEnable時にリセットされるため、WaitForCloseより先にCloseが押されても取りこぼしません。Parentの完了後も子UIに進行中の遷移があればその完了を待ちます。

Animatorなしでは演出を即時完了します。Animatorありで完了イベントが設定されていない場合はanimationTimeout（既定30秒）後に例外を通知し、勝手に次のシーンへ進みません。ユーザーの回答待ちにはタイムアウトを設けていません。

## 通常UIのAnimator

AnimatedUIElementを共通基底とし、GameUI、TutorialUI、OperationGuideUI、MessageBox、ConfirmationDialog、ResultUIで同じ構成を使用します。各コンポーネントのenableAnimation、disableAnimationへAnimationClipを設定します。共通のUIElement.controllerをAnimatorへ割り当てておけば、起動時に各コンポーネントがメモリ上のAnimatorOverrideControllerを作成し、自身のAnimatorへ設定します。UIParentも共通のUIParent.controllerから同様に作成します。OverrideアセットをUIごとに作る必要はありません。Clipを未設定にした箇所は共通Controllerの元Clipを使用します。

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

## クラスとメンバ関数

対象はAssets配下のC#型です。各表の左列は「戻り値 関数名(引数)」です。コンストラクターとデストラクターには戻り値がありません。継承した関数は宣言元に掲載します。非同期戻り値のAwaitableは、呼び出し側で完了を待てます。

### ゲーム進行とシーン

#### [TutorialManager](Assets/Scripts/TutorialManager.cs)

会話・操作ステップの進行。

| シグネチャ | 機能 |
| --- | --- |
| `void Awake()` | BGMを開始 |
| `void Start()` | 登録順に説明・体験を実行しロビーへ移動 |
| `Awaitable ShowLine(string line, TutorialUIPresenter presenter)` | 1行の会話と送り操作を待つ |
| `void SpawnWave(bool forDash)` | 吸引またはダッシュ用のごみを配置 |
| `void Advance()` | 文字送りを完了するか次の行へ進む |
| `void UpdateExperience()` | 課題別の達成条件を判定 |

#### [TutorialManager.TutorialData](Assets/Scripts/TutorialManager.cs)

会話文・複数行のセリフ・体験フラグ・対象操作を保持。

#### [TutorialManager.TrashEntry](Assets/Scripts/TutorialManager.cs)

ごみプレハブとサイズの設定行。

#### [LobbyManager](Assets/Scripts/LobbyManager.cs)

ロビーの進行と排他制御。

| シグネチャ | 機能 |
| --- | --- |
| `void Awake()` | BGMを開始 |
| `void Start()` | 開始演出後に操作を復帰 |
| `bool TryBeginRanking()` | 旧ランキング用の使用開始を試行 |
| `void EndRanking()` | 旧ランキング用の使用中フラグを戻す |
| `bool IsPlayer(Player candidate)` | 対象Playerか判定 |
| `Awaitable ShowConversationAsync(string message, CancellationToken cancellationToken = default)` | 操作を止めて会話を表示し復帰 |
| `Awaitable ConfirmAndTravelAsync(string message, string sceneName, CancellationToken cancellationToken = default)` | 確認後、承認なら指定シーンへ移動 |

#### [GameManager](Assets/Scripts/GameManager.cs)

本編の時間・ごみ生成・終了を管理。

| シグネチャ | 機能 |
| --- | --- |
| `void Awake()` | BGMと開始状態を設定 |
| `void Start()` | 開始から時間切れ・結果・ロビー遷移まで進行 |
| `void SpawnTrash()` | 種類と場所を選んでごみを生成 |
| `void OnDestroy()` | UI購読と生成物を片づける |

#### [GameManager.TrashEntry](Assets/Scripts/GameManager.cs)

ごみプレハブとサイズの設定行。

#### [TitleManager](Assets/Scripts/TitleManager.cs)

旧タイトル用。ビルド対象にタイトルシーンは含まれない。

| シグネチャ | 機能 |
| --- | --- |
| `void Awake()` | BGMとボタンを登録 |
| `void Start()` | 開始演出を待つ |
| `void OpenCredits()` | クレジットを表示 |
| `void CloseCredits()` | クレジットを閉じる |
| `void OnStartButtonClicked()` | 開始演出を起動 |
| `void StartGame()` | Tutorialへの移動を開始 |
| `void OnDestroy()` | ボタン登録を解除 |

#### [Gate](Assets/Scripts/Gate.cs)

ロビーの移動ゲート。

| シグネチャ | 機能 |
| --- | --- |
| `void OnTriggerEnter(Collider other)` | 入場を検出 |
| `void OnTriggerExit(Collider other)` | 退出を検出 |
| `void OnDisable()` | 接触状態を解除 |
| `void Enter()` | 確認と移動をManagerへ依頼 |
| `void Reset()` | コライダーを初期設定 |

#### [NPC](Assets/Scripts/NPC.cs)

話しかけられる店員。

| シグネチャ | 機能 |
| --- | --- |
| `void Awake()` | ガイドと参照を初期化 |
| `void SetGuideEnable(bool enabled)` | 操作ガイドを切替 |
| `Awaitable Invoke()` | ロビーに会話表示を依頼 |

#### [UnityroomScoreSender](Assets/Scripts/UnityroomScoreSender.cs)

unityroomスコア送信。

| シグネチャ | 機能 |
| --- | --- |
| `bool SendScore(int boardNo, int score)` | WebGLで降順ボードへの送信を呼べたらtrue。サーバーの受理を示す戻り値ではない |

### プレイヤー・入力・ごみ

#### [Player](Assets/Scripts/Player/Player.cs)

Unityの入力・更新と各プレイヤー機能を接続。

| シグネチャ | 機能 |
| --- | --- |
| `void Awake()` | 依存オブジェクト・音源・イベントを初期化 |
| `void FixedUpdate()` | 移動・回収・演出を物理更新 |
| `void SetPaused(bool paused)` | 一時停止を切替 |
| `Awaitable Dash()` | ダッシュ完了を待つ |
| `void HandleDash()` | 入力からダッシュを開始 |
| `void Update()` | 音量と近くのNPCを更新 |
| `void HandleInteract()` | NPCとの会話を開始 |
| `void HandleCancel()` | Cancel入力の受け口（現在は空） |
| `void OnDisable()` | ガイドと音を停止 |
| `void OnDestroy()` | イベントを解除 |
| `AudioSource CreateAudioSource(AudioClip clip, bool loop, float volume)` | 2D音源を生成 |
| `void UpdateSounds()` | 入力と遷移から音量を更新 |
| `void FadeLoop(AudioSource source, bool requested, float maximumVolume, float duration, float sceneGain, ref float envelope)` | ループ音をフェード |
| `void PlayCollectSound()` | 回収音を単発再生 |
| `void PlayDashSound()` | ダッシュ移動開始音を単発再生 |

#### [PlayerMovement](Assets/Scripts/Player/PlayerMovement.cs)

移動、壁沿いのダッシュと余速。

| シグネチャ | 機能 |
| --- | --- |
| `PlayerMovement(CharacterController characterController, Transform playerTransform, Transform model, float maxSpeed, float rMaxSpeed, float acceleration, float rAcceleration, float dashLength, float dashChargeTime, Vector3 dashExpandedScale, float dashInitialSpeedMultiplier, float dashCollisionMargin, Func<int, bool> tryConsumeGarbage)` | 移動・消費・モデル設定を注入 |
| `void Tick(Vector2 moveInput, float deltaTime)` | 前後速度と旋回を更新 |
| `Awaitable Dash(Func<bool> isActive, CancellationToken cancellationToken)` | 消費からチャージ、移動、終了まで実行 |
| `void SetPaused(bool paused)` | 停止状態を設定 |
| `float GetDashMovableLength(Vector3 direction, float requestedLength, out bool blocked, out Vector3 hitNormal)` | 移動可能な距離と衝突法線を計算 |
| `void UpdateDashScale()` | 余速に合わせてモデルサイズを戻す |
| `void SetModelScale(Vector3 scale)` | モデルのスケールを設定 |

#### [PickupGarbage](Assets/Scripts/Player/PickupGarbage.cs)

通常吸引とダッシュの回収判定。

| シグネチャ | 機能 |
| --- | --- |
| `PickupGarbage(Transform suctionPortPivot, Transform pickupPivot, float pickupRadius, LayerMask garbageLayer, Material garbageMaterial, GarbageCounter garbageCounter)` | 吸引口・検索範囲・カウンターを受け取る |
| `void TickVacuum(bool isCollecting)` | 吸引材質と回収を更新 |
| `void CollectNearby()` | 範囲のごみを直接回収 |
| `void DrainNearby()` | 吸引でごみを縮めて回収 |
| `Collider[] FindNearby()` | 範囲内のColliderを列挙 |
| `void CollectAsync(Garbage garbage)` | 対象の回収演出を待つ |

#### [Garbage](Assets/Scripts/Garbage.cs)

吸引と回収を受けるごみ。

| シグネチャ | 機能 |
| --- | --- |
| `void Awake()` | 吸引残量を初期化 |
| `void Init(int size)` | サイズ・得点・残量を設定 |
| `bool Drain()` | 残量を減らし回収可能ならtrue |
| `Awaitable Collect(Transform collecterTransform)` | 吸引口へ移動後に破棄 |

#### [GarbageCounter](Assets/Scripts/Player/GarbageCounter.cs)

回収と消費を反映した所持得点。

| シグネチャ | 機能 |
| --- | --- |
| `void RecordCollection(int score)` | 1〜3点を加算し回収を通知 |
| `void ResetCount()` | 得点を0へ戻す |
| `void CountUp(int amount)` | 加算と変更通知 |
| `void CountDown(int amount)` | 減算と変更通知 |
| `bool TrySpend(int amount)` | 残高が足りれば消費してtrue |

#### [PlayerEffects](Assets/Scripts/Player/PlayerEffects.cs)

吸引粒子、タイヤ跡と煙。

| シグネチャ | 機能 |
| --- | --- |
| `void Awake()` | 素材と粒子を初期化 |
| `void Tick(bool vacuuming, bool movingAllowed)` | 吸引・移動に合わせて放出を更新 |
| `void OnDisable()` | 演出を停止 |
| `void OnDestroy()` | 生成した材質を解放 |

#### [PlayerCamera](Assets/Scripts/PlayerCamera.cs)

追従カメラ。

| シグネチャ | 機能 |
| --- | --- |
| `void FixedUpdate()` | 許可された範囲内でプレイヤーを追う |

#### [PlayerCamera.AreaRect](Assets/Scripts/PlayerCamera.cs)

PlayerCameraの移動範囲の最小・最大座標。

#### [InputReader](Assets/Scripts/InputReader.cs)

入力状態とActionTypeごとの許可を管理。

| シグネチャ | 機能 |
| --- | --- |
| `void Awake()` | 入力を初期化 |
| `void OnEnable()` | 入力とCancelを有効化 |
| `void OnDisable()` | 入力を無効化して値を戻す |
| `void Initialize()` | 辞書とイベントを準備 |
| `void Add(ActionType type, InputAction action)` | 種別に入力を登録 |
| `void SetEnableAction(ActionType[] types)` | 指定種別だけ有効化 |
| `ActionType[] GetEnabledActions()` | 有効な種別を取得 |
| `bool IsActionEnabled(ActionType type)` | 指定操作の有効状態を返す |
| `Vector2 GetMoveInput()` | 前後・旋回入力を返す |
| `Vector2 GetLookDiff()` | 視点差分を返す |
| `bool GetIsCollect()` | 吸引の押下状態を返す |
| `void OnCustomAction(ActionType type, InputAction.CallbackContext context)` | カスタム入力を振り分ける |
| `void OnMove(InputAction.CallbackContext context)` | 前後入力を更新 |
| `void OnSpin(InputAction.CallbackContext context)` | 旋回入力を更新 |
| `void OnLook(InputAction.CallbackContext context)` | 視点入力を更新 |
| `void OnCollect(InputAction.CallbackContext context)` | 吸引状態を更新 |
| `void OnDash(InputAction.CallbackContext context)` | ダッシュ入力を通知 |
| `void OnDestroy()` | 生成した入力を解放 |

#### [InputReader.ActionEntry](Assets/Scripts/InputReader.cs)

操作種別とInputActionの設定行。

#### [InputReader.InputPattern](Assets/Scripts/InputReader.cs)

InputReader内の入力状態の列挙。

#### [ActionType](Assets/Scripts/ActionType.cs)

操作種別 Move、Dash、Vacuum、Look、Interact。

### UIとPresenter

#### [IUIElement](Assets/Scripts/IUIElement.cs)

UI表示の契約。

| シグネチャ | 機能 |
| --- | --- |
| `void Enable()` | 表示開始 |
| `void Disable()` | 非表示開始 |

#### [AnimatedUIElement](Assets/Scripts/AnimatedUIElement.cs)

通常UIの表示・入力・演出待機。

| シグネチャ | 機能 |
| --- | --- |
| `void Awake()` | 初期状態を設定 |
| `void ConfigureAnimator()` | Animatorのクリップを構成 |
| `void OnDestroy()` | 生成Controllerを破棄 |
| `void PrepareEnable()` | 表示の先頭姿勢を適用 |
| `void Enable()` | 表示演出を開始 |
| `void Disable()` | 非表示演出を開始 |
| `void CompleteEnable()` | 表示完了を確定 |
| `void CompleteDisable()` | 非表示完了を確定 |
| `Awaitable WaitForTransition(CancellationToken cancellationToken = default)` | 演出終了を待機 |
| `void SetInteraction(bool enabled)` | CanvasGroupの入力可否を切替 |

#### [UIParent](Assets/Scripts/UIParent.cs)

親Canvasの演出順と終了待機。

| シグネチャ | 機能 |
| --- | --- |
| `void Awake()` | Animatorを初期化 |
| `void Update()` | 演出速度と音量倍率を更新 |
| `void ConfigureAnimator()` | 親のクリップを構成 |
| `void OnDestroy()` | 生成Controllerを破棄 |
| `Awaitable Enable(CancellationToken cancellationToken = default)` | 開始演出を待つ |
| `Awaitable FinishAsync(CancellationToken cancellationToken = default)` | 結果待機と遷移演出を進める |
| `Awaitable Play(string trigger, CancellationToken token)` | トリガーを再生して完了を待つ |
| `void EnableElements()` | 子UIの表示開始 |
| `void DisableElements()` | 子UIの非表示開始 |
| `void EnableResult()` | 結果画面を有効化 |
| `void DisableResult()` | 結果画面を閉じる |
| `void CompleteStart()` | 開始完了を通知 |
| `void CompleteEnd()` | 終了演出完了を通知 |
| `void CompleteNextScene()` | 遷移演出完了を通知 |

#### [UIAnimatorOverrides](Assets/Scripts/UIAnimatorOverrides.cs)

共通Animatorのクリップ差し替え。

| シグネチャ | 機能 |
| --- | --- |
| `AnimatorOverrideController Create(Animator animator, params (string originalName, AnimationClip replacement)[] clips)` | 指定のクリップ対からOverrideControllerを構築 |

#### [GameUI](Assets/Scripts/GameUI.cs)

ゲーム中のHUD。

| シグネチャ | 機能 |
| --- | --- |
| `void SetTime(float seconds)` | 残り時間を表示 |
| `void SetTrashNum(int count)` | 所持値を表示 |

#### [GameUIPresenter](Assets/Scripts/GameUIPresenter.cs)

カウンターとHUDを接続。

| シグネチャ | 機能 |
| --- | --- |
| `GameUIPresenter(GameUI view)` | Viewを保持 |
| `void Bind(GarbageCounter source)` | カウンターを購読 |
| `void SetTime(float seconds)` | 残り時間をViewへ渡す |
| `void SetCount(int count)` | 所持値をViewへ渡す |
| `void Dispose()` | 購読を解除 |

#### [TutorialUI](Assets/Scripts/TutorialUI.cs)

チュートリアルの表示をまとめる。

| シグネチャ | 機能 |
| --- | --- |
| `void SetTrashNum(int count)` | 所持値の表示を更新 |

#### [TutorialUIPresenter](Assets/Scripts/TutorialUIPresenter.cs)

会話・ガイド・所持値の仲介。

| シグネチャ | 機能 |
| --- | --- |
| `TutorialUIPresenter(TutorialUI view)` | 各ViewのPresenterを準備 |
| `void Bind(GarbageCounter source)` | カウンターを購読 |
| `void SetCount(int value)` | 所持値を表示 |
| `void Dispose()` | 購読を解除 |

#### [OperationGuideUI](Assets/Scripts/OperationGuideUI.cs)

操作種別のアイコンと説明文。

| シグネチャ | 機能 |
| --- | --- |
| `void Awake()` | 共通UIとガイドを初期化 |
| `void Initialize()` | ガイド辞書を構築 |
| `void SetText(string text)` | 説明文を更新 |
| `void SetGuideEnable(ActionType[] types)` | 指定種別のガイドだけを表示 |

#### [OperationGuideUI.GuideEntry](Assets/Scripts/OperationGuideUI.cs)

操作種別と表示するGameObjectの設定行。

#### [OperationGuidePresenter](Assets/Scripts/OperationGuidePresenter.cs)

操作ガイドの進行役。

| シグネチャ | 機能 |
| --- | --- |
| `OperationGuidePresenter(OperationGuideUI view)` | Viewを保持 |
| `void Show(string message, ActionType[] actions)` | 説明文と対象操作を表示 |
| `void Hide()` | ガイドを閉じる |

#### [MessageBox](Assets/Scripts/MessageBox.cs)

会話の文字送り画面。

| シグネチャ | 機能 |
| --- | --- |
| `void SetAdvanceHandler(Action handler)` | 次へボタンの処理を登録 |
| `Awaitable SetText(string text, CancellationToken cancellationToken = default)` | 文字を順次表示 |
| `void DisplayFullText()` | 全文を即時表示 |
| `void Disable()` | 文字送りを終了し非表示 |

#### [MessageBoxPresenter](Assets/Scripts/MessageBoxPresenter.cs)

会話表示の進行役。

| シグネチャ | 機能 |
| --- | --- |
| `MessageBoxPresenter(MessageBox view)` | Viewを保持 |
| `void DisplayFullText()` | 全文表示 |
| `void Hide()` | 画面を閉じる |
| `Awaitable ShowMessage(string message, Action advance)` | 文と送り操作を設定して表示 |
| `Awaitable ShowAndWaitAsync(string message, CancellationToken cancellationToken = default)` | 会話の終了を待つ |

#### [ConfirmationDialog](Assets/Scripts/ConfirmationDialog.cs)

はい・いいえの画面。

| シグネチャ | 機能 |
| --- | --- |
| `void Init(string message, Action cancelHandler, Action submitHandler)` | 本文と回答処理を登録 |
| `void ClearHandlers()` | 回答処理を解除 |
| `void Bind(Button button, Action handler)` | ボタンに処理を結びつける |

#### [ConfirmationDialogPresenter](Assets/Scripts/ConfirmationDialogPresenter.cs)

確認画面の進行役。

| シグネチャ | 機能 |
| --- | --- |
| `ConfirmationDialogPresenter(ConfirmationDialog view)` | Viewを保持 |
| `Awaitable<bool> ShowAndWaitAsync(string message, CancellationToken cancellationToken = default)` | 表示・回答・非表示を待ち承認ならtrue |

#### [ResultUI](Assets/Scripts/ResultUI.cs)

得点表示・終了ボタン。

| シグネチャ | 機能 |
| --- | --- |
| `void SetCount(int count)` | 得点を保持して表示 |
| `void Enable()` | ボタンを登録して画面を開く |
| `void Close()` | 送信を一度呼び終了通知 |
| `void Confirm()` | 既存のボタンからCloseを呼ぶ |
| `Awaitable WaitForClose(CancellationToken cancellationToken = default)` | 終了操作を待つ |

#### [ResultUIPresenter](Assets/Scripts/ResultUIPresenter.cs)

結果画面の進行役。

| シグネチャ | 機能 |
| --- | --- |
| `ResultUIPresenter(ResultUI view)` | Viewを保持 |
| `void SetResult(int collectedCount)` | 最終得点を渡す |

### 音声と描画

#### [SceneBgm](Assets/Scripts/SceneBgm.cs)

シーン別BGMの設定。

| シグネチャ | 機能 |
| --- | --- |
| `void Play(GameObject owner)` | 2Dループ音源を用意しフェード付きで再生 |

#### [SceneBgmFade](Assets/Scripts/SceneBgmFade.cs)

遷移とBGMの音量を同期。

| シグネチャ | 機能 |
| --- | --- |
| `void Initialize(AudioSource audioSource, float volume, UIParent parent)` | 音源・最大音量・UIParentを設定 |
| `void LateUpdate()` | 遷移進行に合わせて音量を更新 |

#### [TVPostEffect](Assets/Scripts/TVPostEffect.cs)

カメラ用の走査線の濃さ・間隔と色収差の設定。

#### [TVPostEffectFeature](Assets/Scripts/TVPostEffectFeature.cs)

URPのテレビ風Renderer Feature。

| シグネチャ | 機能 |
| --- | --- |
| `void Create()` | 描画パスと材質を準備 |
| `void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)` | 有効なカメラにパスを登録 |
| `void Dispose(bool disposing)` | 材質を解放 |

#### [TVPostEffectFeature.TVPass](Assets/Scripts/TVPostEffectFeature.cs)

テレビ風エフェクトの描画パス。

| シグネチャ | 機能 |
| --- | --- |
| `void Setup(Material effectMaterial, Vector4 values)` | 材質と設定を渡す |
| `void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)` | RenderGraphに描画を登録 |

#### [TVPostEffectVolume](Assets/Scripts/TVPostEffectVolume.cs)

走査線と色収差のVolume設定。

| シグネチャ | 機能 |
| --- | --- |
| `bool IsActive()` | どちらかの強度が正なら有効 |

### Editorツール

#### [UIAnimationSetup](Assets/Editor/UIAnimationSetup.cs)

UIアニメーションのEditorツール。

| シグネチャ | 機能 |
| --- | --- |
| `void CreateTemplates()` | テンプレートを生成 |
| `AnimationClip Clip(string name, string path, float from, float to, params AnimationEvent[] events)` | クリップを作る |
| `AnimationEvent Event(float time, string method)` | Animation Eventを作る |
| `AnimatorState State(AnimatorStateMachine machine, string name, Motion motion = null)` | ステートを追加 |
| `void Transition(AnimatorState source, AnimatorState target, string trigger = null)` | 遷移を追加 |
| `void CreateElement()` | 通常UIのControllerを生成 |
| `void CreateParent()` | 親UIのControllerを生成 |
| `void AssignSelected()` | 選択中のUIへ割当 |
| `void Assign(Component component, string controllerName)` | 対象の設定を更新 |
| `void AssignClipIfEmpty(SerializedObject target, string field, string clipName)` | 未設定のクリップを補う |

#### [UIFlowSceneSetup](Assets/Editor/UIFlowSceneSetup.cs)

シーンのUI・参照を設定するEditorツール。

| シグネチャ | 機能 |
| --- | --- |
| `void ConnectScenes()` | シーンを順に配線 |
| `T Find<T>()` | 指定型を探す |
| `void Ref(Object target, string field, Object value)` | 参照を書き込む |
| `void TextValue(Object target, string field, string value)` | 文字列を書き込む |
| `Canvas Canvas()` | Canvasを用意 |
| `GameObject Rect(string name, Transform parent, Vector2 size, Vector2 position)` | UI矩形を生成 |
| `TMP_Text Label(string name, Transform parent, string text, Vector2 position)` | TMPラベルを生成 |
| `TMP_FontAsset UIFont()` | 日本語フォントを取得 |
| `void UpdateUIFonts()` | UIフォントを更新 |
| `Button Button(string name, Transform parent, Vector2 position)` | ボタンを生成 |
| `ConfirmationDialog Confirmation(Transform canvas)` | 確認画面を生成 |
| `MessageBox Conversation(Transform canvas)` | 会話画面を生成 |
| `UIParent Parent(Canvas canvas, AnimatedUIElement[] elements, ResultUI result = null)` | 親UIに子UIを登録 |
| `Player Player()` | Playerを取得 |
| `void SetupGame()` | ゲームシーンを配線 |
| `void SetupLobby()` | ロビーを配線 |
| `void SetupTutorial()` | チュートリアルを配線 |

### 生成コードとテンプレート

#### [PlayerInputAction](Assets/PlayerInputAction.cs)

Input Systemの自動生成ラッパー。

| シグネチャ | 機能 |
| --- | --- |
| `PlayerInputAction()` | 入力アセットを構築 |
| `~PlayerInputAction()` | 破棄時にマップ状態を検査 |
| `void Dispose()` | 入力アセットを解放 |
| `bool Contains(InputAction action)` | アクションの所属を調べる |
| `IEnumerator<InputAction> GetEnumerator()` | アクションを列挙 |
| `IEnumerator IEnumerable.GetEnumerator()` | 非ジェネリック列挙 |
| `void Enable()` | 全マップを有効化 |
| `void Disable()` | 全マップを無効化 |
| `InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)` | 名前またはIDから検索 |
| `int FindBinding(InputBinding bindingMask, out InputAction action)` | Bindingに対応するアクションを検索 |

#### [PlayerInputAction.PlayerActions](Assets/PlayerInputAction.cs)

プレイヤー入力マップ。

| シグネチャ | 機能 |
| --- | --- |
| `PlayerActions(PlayerInputAction wrapper)` | 親ラッパーを保持 |
| `InputActionMap Get()` | マップを取得 |
| `void Enable()` | マップを有効化 |
| `void Disable()` | マップを無効化 |
| `void AddCallbacks(IPlayerActions instance)` | 通知先を追加 |
| `void UnregisterCallbacks(IPlayerActions instance)` | イベント登録を解除 |
| `void RemoveCallbacks(IPlayerActions instance)` | 通知先を削除 |
| `void SetCallbacks(IPlayerActions instance)` | 通知先を差し替え |
| `implicit operator InputActionMap(PlayerActions set)` | 入力ラッパーを対応するアクションマップへ変換 |

#### [PlayerInputAction.UIActions](Assets/PlayerInputAction.cs)

UI入力マップ。

| シグネチャ | 機能 |
| --- | --- |
| `UIActions(PlayerInputAction wrapper)` | 親ラッパーを保持 |
| `InputActionMap Get()` | マップを取得 |
| `void Enable()` | マップを有効化 |
| `void Disable()` | マップを無効化 |
| `void AddCallbacks(IUIActions instance)` | 通知先を追加 |
| `void UnregisterCallbacks(IUIActions instance)` | イベント登録を解除 |
| `void RemoveCallbacks(IUIActions instance)` | 通知先を削除 |
| `void SetCallbacks(IUIActions instance)` | 通知先を差し替え |
| `implicit operator InputActionMap(UIActions set)` | 入力ラッパーを対応するアクションマップへ変換 |

#### [PlayerInputAction.IPlayerActions](Assets/PlayerInputAction.cs)

生成入力マップの通知インターフェース。

| シグネチャ | 機能 |
| --- | --- |
| `void OnMove(InputAction.CallbackContext context)` | Moveの入力通知を受け取る |
| `void OnSpin(InputAction.CallbackContext context)` | Spinの入力通知を受け取る |
| `void OnLook(InputAction.CallbackContext context)` | Lookの入力通知を受け取る |
| `void OnCollect(InputAction.CallbackContext context)` | Collectの入力通知を受け取る |
| `void OnDash(InputAction.CallbackContext context)` | Dashの入力通知を受け取る |

#### [PlayerInputAction.IUIActions](Assets/PlayerInputAction.cs)

生成入力マップの通知インターフェース。

| シグネチャ | 機能 |
| --- | --- |
| `void OnNavigate(InputAction.CallbackContext context)` | Navigateの入力通知を受け取る |
| `void OnSubmit(InputAction.CallbackContext context)` | Submitの入力通知を受け取る |
| `void OnCancel(InputAction.CallbackContext context)` | Cancelの入力通知を受け取る |
| `void OnPoint(InputAction.CallbackContext context)` | Pointの入力通知を受け取る |
| `void OnClick(InputAction.CallbackContext context)` | Clickの入力通知を受け取る |
| `void OnRightClick(InputAction.CallbackContext context)` | RightClickの入力通知を受け取る |
| `void OnMiddleClick(InputAction.CallbackContext context)` | MiddleClickの入力通知を受け取る |
| `void OnScrollWheel(InputAction.CallbackContext context)` | ScrollWheelの入力通知を受け取る |
| `void OnTrackedDevicePosition(InputAction.CallbackContext context)` | TrackedDevicePositionの入力通知を受け取る |
| `void OnTrackedDeviceOrientation(InputAction.CallbackContext context)` | TrackedDeviceOrientationの入力通知を受け取る |

#### [Readme](Assets/TutorialInfo/Readme.cs)

Unityテンプレートの案内データ。

#### [Readme.Section](Assets/TutorialInfo/Readme.cs)

テンプレート案内の見出し・本文・リンク。

#### [ReadmeEditor](Assets/TutorialInfo/Editor/ReadmeEditor.cs)

Unityテンプレート案内のEditor。

| シグネチャ | 機能 |
| --- | --- |
| `ReadmeEditor()` | Editor初期化処理を登録 |
| `void SelectReadmeAutomatically()` | 初回に案内を選択 |
| `Readme SelectReadme()` | 案内アセットを検索・選択 |
| `void RemoveTutorial()` | 案内を削除 |
| `void OnHeaderGUI()` | Inspectorヘッダーを描画 |
| `void OnInspectorGUI()` | 案内本文を描画 |
| `VisualElement CreateInspectorGUI()` | InspectorのUIを生成 |

## 検証

検証手順は [ゲーム・UIのPlayMode検証](Tests/Run-GameAreaTests.ps1) と [シーンフロー検証](Tests/Run-SceneFlowTests.ps1) を参照してください。unityroomでのスコア受理は公開WebGLビルドで確認します。
