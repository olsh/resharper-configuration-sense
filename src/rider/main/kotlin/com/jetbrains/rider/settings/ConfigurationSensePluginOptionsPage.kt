package com.jetbrains.rider.settings

import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class ConfigurationSensePluginOptionsPage : SimpleOptionsPage(
    name = ConfigurationSenseBundle.message("configurable.name.configurationsense.title"),
    pageId = "Configuration Sense")
{
    override fun getId(): String {
        return "preferences.configurationSense"
    }
}
