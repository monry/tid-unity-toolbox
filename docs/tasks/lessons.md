# Lessons

## UIToolkit: MultiColumnListView セル内の複合型 PropertyField

### ラベルが "." に潰れる / フォーカスで再発する
- **症状**: MultiColumnListView のセルに複合型（`[Serializable]` クラス/構造体）の `PropertyField` を置くと、子フィールドのラベルが幅 0〜3px に潰れて "." 表示になる。一度直しても**行を選択（フォーカス）すると再発**する。
- **原因**: Inspector のラベル整列システム（`unity-base-field__aligned` / AlignmentZone）が、フォーカスや再レイアウトの度に `style.width` を上書きする。狭いセル内では計算幅が極小になり潰れる。`flexShrink=0` を inline で当てても、整列システムが後から幅を上書きするため負ける。
- **対策**: 子フィールド（ラベルの親 BaseField）から `unity-base-field__aligned` クラスを除去して整列対象から外し、ラベルへ `flexShrink=0` + `width/minWidth=Auto` を当てて内容幅で表示する。クラスを外すと整列システムに二度と上書きされない。

### 複合型セルの空行（Foldout トグル）
- **症状**: 複合型は `PropertyField` が Foldout として描画するため、トグル行が空行として残る。
- **対策**: 常に展開（`foldout.value = true`）した上で `unity-foldout__toggle` を `display:none`、`unity-foldout__content` の `marginLeft` を 0 にしてインデントも除去する。

### 複合型の判定は SerializedProperty で行う
- `Q<Foldout>()` のような UI 構造での判定は、バインド直後の Foldout 構築前の過渡状態で誤判定する。`SerializedProperty.propertyType == Generic` で判定し、`PropertyField.userData` に記録して GeometryChanged ハンドラへ渡す。
- **注意**: インライン直列化される `[Serializable]` クラスは `managedReferenceValue` が **null**（managed reference ではない）。`[SerializeReference]` でない限り `managedReferenceValue` では判定できない。

対象: `Assets/Toolbox/Editor/Scripts/PropertyDrawers/SerializableDictionaryPropertyDrawer.cs`
