using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.LookupItems.Impl;
using JetBrains.ReSharper.Feature.Services.Lookup;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;

namespace Resharper.ConfigSense.Models
{
    public class KeyValueSettingLookupItem : TextLookupItem
    {
        #region Fields

        private readonly KeyValueSetting _keyValueSetting;

        #endregion

        #region Constructors

        public KeyValueSettingLookupItem(KeyValueSetting keyValueSetting, IconId image, IRangeMarker rangeMarker)
            : base(GetCompleteText(keyValueSetting), image)
        {
            _keyValueSetting = keyValueSetting;
            VisualReplaceRangeMarker = rangeMarker;
        }

        #endregion

        #region Methods

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

        #endregion
    }
}
