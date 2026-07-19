using System;

namespace Tid.Toolbox.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DictionaryHeaderAttribute : Attribute
    {
        internal const string DefaultKeyHeader = "Key";
        internal const string DefaultValueHeader = "Value";
        internal const float DefaultKeyWidthRatio = 0.4f;

        public DictionaryHeaderAttribute() : this(DefaultKeyHeader, DefaultValueHeader)
        {
        }

        public DictionaryHeaderAttribute(string key, string value, float keyWidthRatio = DefaultKeyWidthRatio)
        {
            Key = key;
            Value = value;
            KeyWidthRatio = keyWidthRatio;
        }

        public string Key { get; init; }
        public string Value { get; init; }
        public float KeyWidthRatio { get; init; }
    }
}