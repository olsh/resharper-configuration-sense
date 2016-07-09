using System.Collections.Generic;

using JetBrains.ReSharper.Psi.Xml.Tree;

namespace Resharper.ConfigurationSense.Extensions
{
    public static class XmlFileExtensions
    {
        #region Methods

        public static IEnumerable<IXmlTag> FindAllByTagName(this IXmlFile xmlFile, string tagName)
        {
            var result = new LinkedList<IXmlTag>();

            var xmlTags = new Queue<IXmlTag>(xmlFile.InnerTags);
            while (xmlTags.Count > 0)
            {
                var xmlTag = xmlTags.Dequeue();
                if (xmlTag.GetTagName().Equals(tagName))
                {
                    result.AddLast(xmlTag);
                }

                foreach (var innerTag in xmlTag.InnerTags)
                {
                    xmlTags.Enqueue(innerTag);
                }
            }

            return result;
        }

        #endregion
    }
}
