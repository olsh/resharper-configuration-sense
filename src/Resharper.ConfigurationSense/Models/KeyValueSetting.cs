using System;
using System.Collections.Generic;

using JetBrains.Annotations;

namespace Resharper.ConfigurationSense.Models
{
    public sealed class KeyValueSetting : IEquatable<KeyValueSetting>
    {
        public KeyValueSetting([NotNull] string key, [NotNull] string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }

        public static IEqualityComparer<KeyValueSetting> KeyComparer { get; } = new KeyEqualityComparer();

        public string Key { get; }

        public string Value { get; }

        public bool Equals(KeyValueSetting other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(Key, other.Key) && string.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((KeyValueSetting)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Key?.GetHashCode() ?? 0) * 397) ^ (Value?.GetHashCode() ?? 0);
            }
        }

        private sealed class KeyEqualityComparer : IEqualityComparer<KeyValueSetting>
        {
            public bool Equals(KeyValueSetting x, KeyValueSetting y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (ReferenceEquals(x, null))
                {
                    return false;
                }

                if (ReferenceEquals(y, null))
                {
                    return false;
                }

                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                return string.Equals(x.Key, y.Key);
            }

            public int GetHashCode(KeyValueSetting obj)
            {
                return obj.Key != null ? obj.Key.GetHashCode() : 0;
            }
        }
    }
}
