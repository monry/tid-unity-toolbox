using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Tid.Toolbox
{
    /// <summary>
    /// Unity でシリアライズ可能な <see cref="Dictionary{TKey,TValue}"/>。
    /// <para>
    /// シリアライズ上の唯一の正は <c>_items</c>（<see cref="SerializableKeyValuePair{TKey,TValue}"/> のリスト）であり、
    /// <see cref="Dictionary{TKey,TValue}"/> 基底は <see cref="OnAfterDeserialize"/> で <c>_items</c> から再構築される実行時ビューである。
    /// この方針により、Inspector 編集中に一時的に生じる重複キー・null キーの行を破壊せず保持できる。
    /// </para>
    /// <para>
    /// 注意: 実行時にコードから辞書を変更（<c>Add</c> / indexer など）しても <c>_items</c> には反映されず、シリアライズ（保存）されない。
    /// 値を永続化したい場合は Inspector で編集すること。
    /// </para>
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<SerializableKeyValuePair<TKey, TValue>> _items = new();

        public void OnBeforeSerialize()
        {
            // _items がシリアライズ上の唯一の正。dict から再生成しないことで、
            // Inspector 編集中の一時的な重複キー行・null 行を破壊せず保持する。
        }

        public void OnAfterDeserialize()
        {
            Clear();
            foreach (var item in _items)
            {
                // 編集中はキーが重複・null になり得る。Add() は重複キーで例外を投げるため、
                // 後勝ちの indexer で取り込み、無効なキーはスキップして実行時 dict を構築する。
                if (item.Key is null)
                {
                    continue;
                }

                this[item.Key] = item.Value;
            }
        }
    }

    [Serializable]
    public struct SerializableKeyValuePair<TKey, TValue>
    {
        [SerializeField] private TKey _key;
        [SerializeField] private TValue _value;

        public SerializableKeyValuePair(TKey key, TValue value)
        {
            _key = key;
            _value = value;
        }

        public TKey Key => _key;
        public TValue Value => _value;
    }
}
