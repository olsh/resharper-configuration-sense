using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;

namespace Resharper.ConfigurationSense.Models
{
    public class KeyValueSettingLookupItem : TextLookupItem
    {
        private readonly KeyValueSetting _keyValueSetting;

        public KeyValueSettingLookupItem(KeyValueSetting keyValueSetting, IconId image, IRangeMarker rangeMarker)
            : base(GetCompleteText(keyValueSetting), image)
        {
            _keyValueSetting = keyValueSetting;
            VisualReplaceRangeMarker = rangeMarker;
        }

        protected override RichText GetDisplayName()
        {
            var displayName = LookupUtil.FormatLookupString($"{_keyValueSetting.Key} = ");
            LookupUtil.AddInformationText(displayName, _keyValueSetting.Value);

            return displayName;
        }

        private static string GetCompleteText(KeyValueSetting keyValueSetting)
        {
            return $"\"{keyValueSetting.Key}\"";
        }
    }
}
