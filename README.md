# MultiMc.SolderUpdater
Downloads and updates mods and configs from a solder modpack API endpoint

## Installation
Just download the [Latest Release](https://github.com/GGG-KILLER/MultiMc.SolderUpdater/releases/latest) and extract the zip file somewhere (anywhere) but take note of the path **with the exe**.

## Usage
Inside MultiMC, go to the Settings tab for your instance and then go to the "Custom commands" tab.

In the "Pre-launch command", insert the following:
```
"<path to the exe you extracted>" "<url of the solder API>" "<modpack slug>" "$INST_MC_DIR"
```

If you already added some mods and configurations before, open your instance's directory and delete **both** the `mods` and `config` folders.

Now when you launch minecraft again the mods will be downloaded for you and updated on the following launches.
