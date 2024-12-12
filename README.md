# Resonite Metrics Counter

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/).

![Gei1YYFaQAAsnlv](https://github.com/user-attachments/assets/0d8497a4-1b74-4977-8aff-036ad2b588c5)

This mod provides a simple performance metrics counter for Resonite, useful for world optimisation.
It can measure execution times for components and ProtoFlux node groups.
These execution times are measured per `RefreshStage`, helping you to identify and fix performance bottlenecks in your world.

## Installation

1. Install the [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
1. Place the [ResoniteMetricsCounter.dll](https://github.com/esnya/ResoniteMetricsCounter/releases/latest/download/ResoniteMetricsCounter.dll) into your `rml_mods` folder. This folder should be located at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a standard installation. You can create it if it's missing, or if you start the game once with the ResoniteModLoader installed it will create this folder for you.
1. Launch the game. If you want to check that the mod is working you can check your Resonite logs.
1. Open the `Editor` category in the `Create Dialog`.
1. Press the `Start Metrics Counter (mod)` button.

## Development Requirements

For development, you will need the [ResoniteHotReloadLib](https://github.com/Nytra/ResoniteHotReloadLib) to be able to hot reload your mod with DEBUG build.
