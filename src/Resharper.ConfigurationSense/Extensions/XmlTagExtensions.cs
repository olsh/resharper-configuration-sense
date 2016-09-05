using System.Collections.Generic;

using JetBrains.ReSharper.Psi.Xml.Tree;

using Resharper.ConfigurationSense.Constants;
using Resharper.ConfigurationSense.Models;

namespace Resharper.ConfigurationSense.Extensions
{
    public static class XmlTagExtensions
    {
        #region Methods

        public static IEnumerable<KeyValueSetting> GetChildSettings(
            this IXmlTag xmlTag, 
            string keyAttribute, 
            string valueAttribute)
        {
            var result = new LinkedList<KeyValueSetting>();

            var innerXmlTags = new Queue<IXmlTag>(xmlTag.InnerTags);

            while (innerXmlTags.Count > 0)
            {
                var currentXmlTag = innerXmlTags.Dequeue();

                var attributes = currentXmlTag.GetAttributes();

                var keyValueSetting = new KeyValueSetting();
                foreach (var attribute in attributes)
                {
                    if (attribute.XmlName.Equals(keyAttribute))
                    {
                        keyValueSetting.Key = attribute.UnquotedValue;
                    }
                    else if (attribute.XmlName.Equals(valueAttribute))
                    {
                        keyValueSetting.Value = attribute.UnquotedValue;
                    }
                }

                if (keyValueSetting.Key == null || keyValueSetting.Value == null)
                {
                    continue;
                }

                result.AddLast(keyValueSetting);
            }

            return result;
        }

        public static string GetExternalConfigName(this IXmlTag xmlTag)
        {
            var attributes = xmlTag.GetAttributes();

            foreach (var attribute in attributes)
            {
                if (attribute.XmlName.Equals(SettingsConstants.ExternalConfigTagName))
                {
                    return attribute.UnquotedValue;
                }
            }

            return null;
        }

        #endregion
    }
}
