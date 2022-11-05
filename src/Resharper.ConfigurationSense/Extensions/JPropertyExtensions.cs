using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace Resharper.ConfigurationSense.Extensions
{
    public static class JPropertyExtensions
    {
        public static string GetSettingsPath(this JProperty property)
        {
            if (property.Parent == null)
            {
                return string.Empty;
            }

            var positions = new List<string>();
            JToken previous = null;
            for (JToken current = property; current != null; current = current.Parent)
            {
                switch (current.Type)
                {
                    case JTokenType.Property:
                        var prop = (JProperty)current;
                        positions.Add(prop.Name);
                        break;
                    case JTokenType.Array:
                    case JTokenType.Constructor:
                        if (previous != null)
                        {
                            int index = ((IList<JToken>)current).IndexOf(previous);

                            positions.Add(index.ToString());
                        }
                        break;
                }

                previous = current;
            }

            positions.Reverse();

            return string.Join(":", positions);
        }
    }
}
