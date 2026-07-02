using System.Linq;
using System.Reflection;
using Tid.Toolbox.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Tid.Toolbox.Editor.PropertyDrawers;

/// <summary>
/// <see cref="SerializableDictionary{TKey,TValue}"/> をテーブル風に描画する UIToolkit ベースの PropertyDrawer。
/// <see cref="DictionaryHeaderAttribute"/> が付与されている場合は、その Key / Value を列ヘッダに用いる。
/// </summary>
[CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
public class SerializableDictionaryPropertyDrawer : PropertyDrawer
{

    private const string ItemsFieldName = "_items";
    private const string KeyFieldName = "_key";
    private const string ValueFieldName = "_value";

    private const string UssClassName = "tid-toolbox-property-drawer-serializable-dictionary";

    private const string ValueElementName = "unity-list-view__reorderable-item__container";
    private const string ValueElementUssClassName = "unity-list-view__reorderable-item__container";

    private static StyleSheet StyleSheet { get; } = ToolboxEditorUtility.LoadStyleSheet<SerializableDictionaryPropertyDrawer>();

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var foldout = new Foldout
        {
            text = property.displayName,
            value = true,
        };
        foldout.styleSheets.Add(StyleSheet);
        foldout.AddToClassList(UssClassName);

        var itemsProperty = property.FindPropertyRelative(ItemsFieldName);
        if (itemsProperty == null)
        {
            // 想定外の型に適用された場合のフォールバック。
            foldout.Add(new HelpBox($"`{ItemsFieldName}` が見つかりません。", HelpBoxMessageType.Warning));
            return foldout;
        }

        var header = fieldInfo.GetCustomAttribute<DictionaryHeaderAttribute>();
        var keyHeader = header?.Key ?? DictionaryHeaderAttribute.DefaultKeyHeader;
        var valueHeader = header?.Value ?? DictionaryHeaderAttribute.DefaultValueHeader;
        var keyWidthRatio = header?.KeyWidthRatio ?? DictionaryHeaderAttribute.DefaultKeyWidthRatio;

        var listView = new MultiColumnListView
        {
            showAddRemoveFooter = true,
            reorderable = true,
            reorderMode = ListViewReorderMode.Animated,
            showBoundCollectionSize = false,
            selectionType = SelectionType.Multiple,
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            // _items 配列をデータソースとしてバインドする（追加 / 削除 / 並べ替えは MultiColumnListView 側で処理される）。
            bindingPath = itemsProperty.propertyPath,
        };

        listView.columns.Add(new Column
        {
            name = KeyFieldName,
            title = keyHeader,
            stretchable = false,
            minWidth = 60,
            width = new Length(keyWidthRatio * 100.0f, LengthUnit.Percent),
            makeCell = MakeLabelCell,
            bindCell = (element, index) => BindCell(element, itemsProperty, index, KeyFieldName),
            unbindCell = UnbindCell,
        });

        listView.columns.Add(new Column
        {
            name = ValueFieldName,
            title = valueHeader,
            stretchable = true,
            minWidth = 60,
            makeCell = MakeValueCell,
            bindCell = (element, index) => BindCell(element.Q<PropertyField>(), itemsProperty, index, ValueFieldName),
            unbindCell = UnbindCell,
        });

        foldout.Add(listView);
        return foldout;
    }

    private static VisualElement MakeLabelCell()
    {
        return MakeCell();
    }

    private static VisualElement MakeValueCell()
    {
        var wrapper = new VisualElement
        {
            name = ValueElementName,
            style =
            {
                paddingLeft = 0,
            },
        };
        wrapper.AddToClassList(ValueElementUssClassName);
        wrapper.Add(MakeCell());
        return wrapper;
    }

    private static VisualElement MakeCell()
    {
        // ラベルは列ヘッダで表現するため、セル内のフィールドラベルは隠してセル幅いっぱいに描画する。
        var propertyField = new PropertyField
        {
            label = string.Empty,
            style =
            {
                flexGrow = 1,
            },
        };
        // PropertyField の子要素はバインド後に非同期で構築されるため、レイアウト確定を待ってラベル表示を調整する。
        propertyField.RegisterCallback<GeometryChangedEvent>(AdjustCellLayout);
        return propertyField;
    }

    private static void AdjustCellLayout(GeometryChangedEvent evt)
    {
        if (evt.target is not PropertyField propertyField)
        {
            return;
        }

        // 複合型か否かは UI 構造ではなく SerializedProperty の種別で判定し、BindCell で userData に記録している
        // （Foldout 構築前の過渡状態で誤って単純型として扱い、子ラベルを隠してしまうのを避ける）。
        if (propertyField.userData is not CellData { PropertyType: SerializedPropertyType.Generic, RootElement: { } rootElement })
        {
            return;
        }
        foreach (var field in propertyField.Query<PropertyField>().ToList().Select(x => x.Children().ToList()[0]))
        {
            field.RemoveFromClassList("unity-base-field__aligned");
        }
        propertyField.parent.style.paddingLeft = 13;
        foreach (var label in propertyField.Query<Label>().ToList().Where(label => label.FindElementInParent<MultiColumnListView>() == rootElement))
        {
            label.style.minWidth = 80;
            label.style.width = new StyleLength(rootElement.resolvedStyle.width / 4.0f);
        }
    }

    private static void BindCell(VisualElement element, SerializedProperty itemsProperty, int index, string relativeFieldName)
    {
        if (element is not PropertyField propertyField)
        {
            return;
        }

        if (index < 0 || index >= itemsProperty.arraySize)
        {
            return;
        }

        var elementProperty = itemsProperty.GetArrayElementAtIndex(index).FindPropertyRelative(relativeFieldName);
        propertyField.BindProperty(elementProperty);

        // 複合型（インライン直列化される独自の [Serializable] 型）は Foldout として描画される。
        // 値が managed reference ではなく Generic で直列化されるため、propertyType で判定して AdjustCellLayout に渡す。
        propertyField.userData = new CellData(element.FindElementInParent<MultiColumnListView>()!, elementProperty.propertyType);

    }

    private static void UnbindCell(VisualElement element, int index)
    {
        if (element is PropertyField propertyField)
        {
            propertyField.Unbind();
        }
    }

    private record struct CellData(
        VisualElement RootElement,
        SerializedPropertyType PropertyType
    );
}
