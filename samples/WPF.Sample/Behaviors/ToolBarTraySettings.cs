using System.Collections.Generic;
using System.Configuration;

namespace Trading.UI.Sample.Behaviors
{
    public class ToolBarTraySettings : ApplicationSettingsBase
    {
        public ToolBarTraySettings()
        {
        }

        public ToolBarTraySettings(string settingsKey) : base(settingsKey)
        {
        }

        [UserScopedSetting]
        public List<ToolBarSettings> ToolBarSettings
        {
            get => (List<ToolBarSettings>)this["ToolBarSettings"];
            set => this["ToolBarSettings"] = value;
        }
    }
}