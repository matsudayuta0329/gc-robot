# ゲームエリアとUIの設定

Unity 6000.3.9f1 / Input System / TextMeshProを使用。非同期処理はUnity Awaitable。
既存のシーン・プレハブ・アニメーションは変更していないため、以下をInspectorで設定する。

## ゲームシーン

1. Playerの既存参照（InputReader、Pickup Pivot、Suction Port Pivot、Garbage Layer等）を設定する。CharacterControllerとGarbageCounterが必要。
   Modelには拡縮する見た目のTransformを設定する。ダッシュ時は0.2秒停止し、(1,1,1)から(10.5,1.7,1.5)へ線形に拡大してから移動する。ダッシュ終了時の初速から通常最高速度へ戻る間に(1,1,1)まで線形に縮小する。値はPlayerのDash Charge Time、Dash Expanded Scale、Dash Initial Speed Multiplierで調整できる。
   ModelにはCharacterControllerを持つPlayerルートではなく、見た目だけを含む子Transformを割り当てる。ダッシュは事前のCapsuleCastで壁までの安全距離に制限し、壁に当たった時点で終了する。壁との余白はDash Collision Marginで調整できる。
2. GameManagerを空のGameObjectへ追加し、Player、同じInputReader、GarbageCounter、GameUI、ResultUIを設定する。GarbageCounterはPlayerから自動取得も可能。
3. Trash ObjectsへルートにGarbageを持つプレハブとSize（回収回数・固有得点1〜3）を登録する。ColliderとPlayerのGarbage Layerに含まれるレイヤーも設定する。
4. Limitは制限秒数、Spawn Intervalは生成間隔。Spawn Pointsからランダムに生成する。未設定の場合はGameManagerの位置。Trash Parentは任意。
5. GameUIのTime Text / Score Text / Count TextにTMPを設定。Count Textはダッシュ消費を含む現在の所持数、Score Textは回収したごみの固有得点合計。
6. ResultUIのScore Text / Count Text / Continue Buttonを設定する。結果のCountはダッシュで減らない累計回収数。Continue ButtonはInitで自動接続される。独自ボタンの場合はConfirmを呼ぶ。
7. Lobby Sceneに戻り先のシーン名を指定しBuild ProfilesのScene Listに登録する。空の場合は結果を閉じて停止する。

開始演出中は操作停止 → 操作開始とごみ生成 → 時間切れで操作停止 → リザルト表示・GameUI非表示 → 続けるボタン → 終了演出 → ロビーへ移動。
スコアは各ごみの固有得点の合計。既存のダッシュ消費（所持数から5減算、所持数が不足していても実行可）は維持している。
TrashCount.SetTrashNumは指定どおり空関数。実際のゲーム中の個数表示はGameUI.Count Textを使用する。

## UI共通

AnimatedUIElementを継承する各UIにはRoot、Animator、Enable/Disable Trigger、Enable/Disable Durationがある。
Root省略時は自身を表示切替する。Animatorなしでも利用可能。
Durationに演出の秒数を指定する。非表示アニメーションの待機後にSetActive(false)となり、途中でEnableした場合は非表示予約を取り消す。
ManagerはUIの子に置かず、UIを非表示にしても存在する別オブジェクトに置く。
表示アニメーションはAnimator Controllerに用意し、指定したTriggerを作る。

MessageWindowは会話とロビーで共通。Message TextにはTMP、Submit/Cancel ButtonにはUI Buttonを設定する。
SetTextで文字送り、DisplayFullTextで即時表示。Initは両ボタンのInspector登録済みイベントも含めて置き換える。
UIボタン操作にはEventSystemとInputSystemUIInputModuleを用意する（InputReaderが制御するのはプレイヤー入力のみ）。

## 入力とロビー

InputReader.ActionsはActionTypeとInputActionのシリアライズ可能な組のリスト。
未指定の種類は既存PlayerInputActionを使い、MoveにはMoveとSpinをまとめる。
カスタムMove/LookはVector2、Dash/Vacuum/InteractはButton入力として設定する。
SetEnableAction(null)で全有効、空配列で全無効。無効化時は保持中の移動・吸引入力も解除する。
Interactの既定入力はEキー / ゲームパッド南ボタン。

NPCにはMessage、Move Scene、Guide、MessageWindow、同じPlayerとInputReaderを設定する。
NPCのCollider（Trigger可）をPlayerのNpc Layerに含め、Npc Radiusで会話範囲を決める。
キャンセル時は会話前の入力マスク・停止状態へ戻り、決定時は指定シーンへ進む。Move Sceneが空なら決定時も操作に戻る。

## タイトル・チュートリアル

TitleManagerにはCredits、Start Button、Animator、Tutorial Sceneを設定。
Start Buttonは自動接続。Animator使用時は開始演出のAnimation EventでStartGameを呼ぶ。Animatorなしなら即移動。

TutorialManagerにはTutorialUI、InputReader、Player、Lobby Sceneと順番どおりのTutorial Dataを設定。
TutorialUIにはMessageWindow、TrashCount、OperationGuideUIを設定する。
OperationGuideUIにはMessage Text（TMP）と各ActionTypeのガイドGameObjectを登録する。
会話中は操作とガイドを全停止。進むボタンで全文表示、その次の押下で次の項目へ進む。
操作体験はMoveと対象操作を有効化し、対応する入力を実行すると完了する。外部の体験完了判定からAdvanceを呼ぶこともできる。

## 検証

Tests/Run-GameAreaTests.ps1をPowerShellから実行する。
Temp/GameAreaValidationに分離したプロジェクトを作り、元のシーンを開き直さずにPlayModeテストを実行する。
Unityのインストール先が異なる場合は-UnityEditorでUnity.exeの絶対パスを渡す。
結果はTemp/game-area-results.xml、ログはTemp/game-area-tests.log。
実際のTMP表示、Animatorの遷移、レイアウト、登録したシーンへの移動はInspector設定後に確認する。
