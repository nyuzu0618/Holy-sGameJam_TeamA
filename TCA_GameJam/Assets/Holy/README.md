# アウトゲーム（タイトル／リザルト）README

ひつじの逆襲のアウトゲーム一式です。タイトル画面、本編シーンの中で表示するリザルト画面、シーン切り替え時のフェードが入っています。

- 作成環境：Unity 6000.3.13f1（URP）
- 必要なパッケージ（UnityPackage には含まれていません）
  - **Input System**（`Project Settings > Player > Active Input Handling` が `Input System Package (New)` か `Both`）
  - **TextMeshPro**（`Window > TextMeshPro > Import TMP Essential Resources`）
- スクリプトはすべて名前空間 **`OutGame`** に入っています（自分のスクリプトとクラス名がかぶらないようにするため）

---

## 1. 導入

1. `OutGame_Holy.unitypackage` をダブルクリック（または `Assets > Import Package > Custom Package...`）して、全部チェックのまま **Import**
2. コンパイルが終わると、次のシーンが自動でビルド一覧（`File > Build Profiles` の Scene List）に登録されます
   - `title`（一覧の先頭）
   - `MainGame`（プロジェクト内のどこにあっても、この名前のシーンなら登録されます）
   - `MainGameTest`
3. `Assets/Holy/title.unity` を開いて再生 → 何かキーで `MainGame` に移動すれば OK

> **UnityPackage に `MainGame.unity` は入っていません**（本編担当の MainGame を上書きしないため）。
> 本編シーンは **`MainGame`** という名前にしてください。

### 画面の流れ

```
title ──(何かキー)──▶ MainGame（本編）
  ▲                    │ ゲーム終了で ResultCanvas を表示
  │                    ▼
  │              ┌ ResultCanvas ┐
  └─(TITLE ボタン)┤              ├─(RETRY ボタン)─▶ MainGame を読み込み直す
                 └──────────────┘
```

シーンが切り替わるたびに、黒でフェードアウト → フェードインします。

---

## 2. 中身

```
Assets/Holy/
├─ title.unity               タイトルシーン
├─ MainGameTest.unity        リザルトの動作確認用（スペースでリザルト表示）
├─ README.md                 このファイル
├─ Prefabs/
│   ├─ ResultCanvas.prefab   リザルト画面（MainGame に置く）
│   └─ SceneFader.prefab     フェード（MainGame に置く）
├─ Image/
│   └─ タイトル統合.psd       タイトル画像（3840×2160）
├─ Scripts/                  （すべて namespace OutGame）
│   ├─ SceneFader.cs         フェードイン／フェードアウト＋シーン移動
│   ├─ TitleManager.cs       何かキーを押したら本編へ
│   ├─ PulseScale.cs         文字の拡大縮小（「START」用）
│   ├─ ResultManager.cs      リザルトの表示・値のセット・ボタン
│   └─ MainGameTest.cs       動作確認用（スペースでリザルト表示）
└─ Editor/
    └─ OutGameSceneBuilder.cs  ビルド一覧への自動登録・組み立て直し
```

---

## 3. MainGame（本編シーン）でやること

### 3-1. プレハブを2つ置く

`Assets/Holy/Prefabs` から MainGame の Hierarchy にドラッグします。

| プレハブ | 役割 |
|---|---|
| **ResultCanvas** | リザルト画面。ゲーム開始時は自動で隠れている |
| **SceneFader** | シーン開始時のフェードイン／リトライ・タイトルへ移動するときのフェードアウト |

- ResultCanvas は **非アクティブにしないでください**（隠すのはスクリプトが自動でやります）
- MainGame に **EventSystem** が無いとボタンが押せません
  （`GameObject > UI > Event System` で追加し、**Input System UI Input Module** にする）

### 3-2. ゲーム終了時にリザルトを表示する

```csharp
using OutGame;   // ← 忘れずに

public class GameManager : MonoBehaviour
{
    [SerializeField] private ResultManager result;   // Inspector で ResultCanvas をドラッグ

    void OnGameEnd()
    {
        result.Show(value1, value2);   // 値をセットして表示
    }
}
```

- **TITLE ボタン** → `title` シーンへ移動
- **RETRY ボタン** → 今のシーン（MainGame）を読み込み直してやり直し

> **Time.timeScale に注意：** ゲーム終了時に `Time.timeScale = 0` にしても、リザルトの表示・フェードは動きます。
> ただし **シーンを読み込み直しても timeScale は 0 のまま** なので、MainGame の `Start()` などで `Time.timeScale = 1;` に戻してください。

---

## 4. リザルトに渡す要素

リザルトには **「画像 ×値」が2つ** あり、本編から **int の値を2つ** 渡します。

| 渡す値 | 表示される場所 | セットの方法 |
|---|---|---|
| **Value1** | 1つ目の「画像 ×99」 | `Show(value1, value2)` の第1引数 ／ `SetValue1(int)` ／ `Value1` |
| **Value2** | 2つ目の「画像 ×99」 | `Show(value1, value2)` の第2引数 ／ `SetValue2(int)` ／ `Value2` |

```csharp
// まとめてセットして表示
result.Show(12, 34);

// 個別にセットしてから表示
result.SetValue1(12);
result.SetValue2(34);
result.Show();

// プロパティでも OK（表示中に変えるとすぐ反映）
result.Value1 = 12;
```

| ResultManager のメソッド／プロパティ | 説明 |
|---|---|
| `Show(int value1, int value2)` | 値をセットして表示（フェードイン） |
| `Show()` | 今の値のまま表示 |
| `Hide()` | 隠す |
| `SetValue1(int)` / `SetValue2(int)` | 値をセット |
| `Value1` / `Value2` | 値の取得・セット |
| `IsShowing` | 表示中なら true |
| `GoToTitle()` / `Retry()` | ボタンと同じ動作をコードから呼ぶ |

- アイコン画像はリザルト側の **Icon Sprite** で設定します（本編から渡す必要はありません）

---

## 5. Inspector の設定

### 5-1. ResultCanvas（ResultManager）

プレハブを開いて（ダブルクリック）設定すると、置いた全シーンに反映されます。

| 項目 | 初期値 | 説明 |
|---|---|---|
| Background Image | （割り当て済み） | 背景を表示する Image（変更不要） |
| **Background Sprite** | なし | **背景画像を指定する**。空なら下の色で画面を暗くする |
| Background Color | 黒（半透明） | 背景画像が無いときの色 |
| Value1 Item / Value2 Item | | 「画像 ×値」の1つ目と2つ目 |
| 　Icon Image / Count Text | （割り当て済み） | 表示先（変更不要） |
| 　**Icon Sprite** | なし（白い四角） | **アイコン画像を指定する** |
| 　Value | 99 | 仮の値（`Show()` などで上書きされる） |
| 　Prefix | `×` | 数字の前に付く文字 |
| Title Button / Retry Button | （割り当て済み） | ボタン（変更不要） |
| Title Scene Name | `title` | TITLE ボタンの移動先 |
| Hide On Start | ON | ゲーム開始時は隠す |
| Fade Duration | 0.3 | 表示するときのフェード秒数 |

### 5-2. title.unity

| オブジェクト | 項目 | 初期値 | 説明 |
|---|---|---|---|
| TitleManager | Game Scene Name | `MainGame` | 何かキーを押したときの移動先 |
| SceneFader | Fade Color / Fade In・Out Duration | 黒 / 0.5秒 | フェードの色と秒数 |
| TitleCanvas / StartText | Text | `START` | 表示する文字 |
| 〃 PulseScale | Min・Max Scale / Cycles Per Second | 0.9・1.1 / 1 | 拡大縮小の大きさと速さ |

### 5-3. MainGameTest.unity（リザルトの動作確認用）

MainGameTest を直接開いて再生し、**スペースキー** でリザルトを表示できます（RETRY で MainGameTest を読み込み直し）。

| オブジェクト | 項目 | 説明 |
|---|---|---|
| MainGameTest | Random Values | ON：スペースを押すたびにランダムな値 ／ OFF：下の値で表示 |
| | Test Value1 / Test Value2 | Random Values が OFF のときに表示する値 |

---

## 6. 困ったとき

| 症状 | 原因と対処 |
|---|---|
| キーやボタンを押すと暗くなったまま止まる | 移動先のシーンがビルド一覧に無い、またはシーン名の打ち間違い。Console の赤いエラー（`has not been added to the build settings`）を確認。手動登録は `Tools > Holy > シーンをビルド一覧に登録` |
| `ResultManager` が見つからないエラー | スクリプトの先頭に `using OutGame;` を書く |
| ボタンが押せない | シーンに EventSystem があり、**Input System UI Input Module** が付いているか確認 |
| リザルトが出ない | ResultCanvas が非アクティブになっていないか、`Show()` を呼んでいるか確認 |
| リトライ後にゲームが止まっている | `Time.timeScale` が 0 のまま。MainGame の開始時に `Time.timeScale = 1;` |
| `MainGame` という名前のシーンが複数あると警告が出る | 同じ名前のシーンは1つにする（先に一覧に入っている方が読み込まれます） |
| 日本語が □ になる | TMP の標準フォントは英語のみ。日本語フォント（Noto Sans JP など）を入れて `Create > TextMeshPro > Font Asset` で作り、テキストの Font Asset に設定する |
| 配置が崩れた／作り直したい | `Tools > Holy > アウトゲームを組み立てる`（既にあるオブジェクトは再利用するので重複しません） |
