#if UNITY_WEBGL && !UNITY_EDITOR

using System;

namespace SQLite
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TableAttribute : Attribute
    {
        public TableAttribute(string name) { }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PrimaryKeyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class AutoIncrementAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NotNullAttribute : Attribute
    {
    }
}

#endif