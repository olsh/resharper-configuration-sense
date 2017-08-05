using System.Collections.Generic;

using JetBrains.ReSharper.Psi.Xml.Tree;

namespace Resharper.ConfigurationSense.Extensions
{
    public static class XmlFileExtensions
    {
        public static IEnumerable<IXmlTag> FindAllByTagName(this IXmlTagContainer xmlFile, string tagName)
        {
            var xmlTags = new Queue<IXmlTag>(xmlFile.InnerTags);
            while (xmlTags.Count > 0)
            {
                var xmlTag = xmlTags.Dequeue();
                if (xmlTag.GetTagName().Equals(tagName))
                {
                    yield return xmlTag;
                }

                foreach (var innerTag in xmlTag.InnerTags)
                {
                    xmlTags.Enqueue(innerTag);
                }
            }
        }
    }
}
