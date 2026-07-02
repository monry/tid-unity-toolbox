using System;
using Tid.Toolbox;
using Tid.Toolbox.Attributes;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace Toolbox
{
    public class SerializableDictionarySample : MonoBehaviour
    {
        [SerializeField]
        [DictionaryHeader("キー", "バリュー")]
        private SerializableDictionary<int, string> _dictionary = new();

        [SerializeField]
        [DictionaryHeader("複雑な", "型")]
        private SerializableDictionary<int, Foo> _complexDictionary = new();

        [SerializeField]
        private Foo _foo = new();
        [SerializeField] private bool _bar;
        [SerializeField] private int _baz;
        [SerializeField] private string _qux = string.Empty;

        [Serializable]
        public class Foo
        {
            [SerializeField] private bool _bar;
            [SerializeField] private int _baz;
            [SerializeField] private string _qux = string.Empty;
            [SerializeField] private Hoge _hoge = new();
            [SerializeField] private SerializableDictionary<int, Hoge> _dictionary = new();
        }

        [Serializable]
        public class Hoge
        {
            [SerializeField] private bool _fuga;
            [SerializeField] private int _piyo;
            [SerializeField] private string _ponyo = string.Empty;
        }
    }
}
