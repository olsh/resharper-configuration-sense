package com.jetbrains.rider.settings

import com.jetbrains.rider.settings.simple.SimpleOptionsPage

class ConfigurationSensePluginOptionsPage : SimpleOptionsPage("Configuration Sense", "ConfigurationSense") {
    override fun getId(): String {
        return "ConfigurationSense"
    }
}
