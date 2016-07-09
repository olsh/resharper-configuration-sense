using System;

namespace Resharper.ConfigurationSense.Models
{
    public class KeyValueSetting : IEquatable<KeyValueSetting>
    {
        #region Properties

        public string Key { get; set; }

        public string Value { get; set; }

        #endregion

        #region Methods

        public bool Equals(KeyValueSetting other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Key, other.Key) && string.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((KeyValueSetting)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Key?.GetHashCode() ?? 0) * 397) ^ (Value?.GetHashCode() ?? 0);
            }
        }

        #endregion
    }
}
