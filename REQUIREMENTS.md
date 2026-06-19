# Windows-AAS

Please build as Windows-AAS (Asset Administration Shell). It is a plugin based architecture which allow read and write properties from/to a Windows host system.

## Requirements
* Allows control and read properties via AAS
* AAS is running on system level and is started on boot (not user login)
* It has a Windows installer
* Has detailed install instruction
* Is secured and encrypted
* Has a repository to register plugins online, that can be install
* There is an web interface (build in modern Blazor look and feel) to configure the windows core service by an admin, which is only available at 127.0.0.1 be default.
* It is analyzed against STRIDE attacks

## Architecture
* Written in the latest .Net and ASP
* The aas itself lives in a https://hub.docker.com/r/eclipsebasyx/aas-environment, which is registered to a basyx AAS/submodel registry
* The AAS also Provides MQTT
* The core windows server does not provide the AAS, but talk to the AAS environment via MQTT

## The web interface
* Allows to install, enable and disable plugins
* Read the log and see errors
* Configure the plugins

## Plugins
* Each plugin has a submodel to first access to plugin and read out basic information and to get an overview, whats available in the plugin

### AV-Plugin
* RW-Access (Read/Write) to Monitor settings: enabled/disabled, resolution, DPI, hardware info (readonly), bit depth, Frequency, mode (mirror/extend), etc.
* Monitor is accessed via the index shown in the Windows 11 Settings, so 1 to n
* RW-Access to Audio Input and Output devices: enabled/disabled, resolution, DPI, hardware info (readonly), bit depth, Frequency, etc.
* Each Audio and Video device sits in its own submodel

### Automation-Plugin
* Allows to attach scripts and configure applications to be executable from the AAS
* Allows to deploy automation with triggers, that execute scripts and applications
