package com.jetbrains.rider.settings

import com.jetbrains.rider.settings.simple.SimpleOptionsPage
import com.jetbrains.rider.settings.ConfigurationSenseBundle

class ConfigurationSensePluginOptionsPage : SimpleOptionsPage(
    name = ConfigurationSenseBundle.message("configurable.name.configurationsense.title"),
    pageId = "ConfigurationSense")
{
    override fun getId(): String {
        return "ConfigurationSense"
    }
}
