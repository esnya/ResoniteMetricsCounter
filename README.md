# Resonite Metrics Counter

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/).

![image](https://github.com/user-attachments/assets/e4a11a7b-b122-416a-85db-20a51d9eb658)
![image](https://github.com/user-attachments/assets/bdaccc83-55e3-4621-aba9-c15b39bfbccd)

This mod provides a simple performance metrics counter for Resonite, useful for world optimisation.
It can measure execution times for components and ProtoFlux node groups.
These execution times are measured per `RefreshStage`, helping you to identify and fix performance bottlenecks in your world.

## Recommended Usage

**Tip:** Before using this mod, it is recommended to first check the Debug options in the DashMenu. For example, the "Focused World" and "Physics" tabs provide accurate values for each stage without any overhead. Once you have identified potential performance issues, you can use this mod to pinpoint specific components or slots causing the problem.

## Understanding Performance Metrics

**Important:** This mod shows performance data to help you find and fix slow parts of your world. But, be careful when reading the data. Here are some tips:

1. **Context Matters**: High numbers do not always mean a problem. It might just be a complex task.

2. **Compare Wisely**: Only compare data from the same world or setup. Different setups can give different results.

3. **Focus on Big Issues**: Fix the parts that slow down your world the most. Small issues might not need fixing right away.

4. **Work Together**: Share your findings with others. Avoid blaming without understanding the full picture.

By following these tips, you can use this mod to make your world better and help the community.

## Important Notes on Metrics

**Note:** The metrics from this mod have limits:

1. **Not Perfect**: The numbers may not be 100% correct or complete. This mod is not part of the engine, so perfect accuracy is not possible.

2. **ProtoFlux Node Groups**: For ProtoFlux, the mod measures NodeGroups, not individual Nodes. NodeGroups have many Nodes, so their numbers can be bigger than components.

## Installation

1. Install the [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
1. Place the [ResoniteMetricsCounter.dll](https://github.com/esnya/ResoniteMetricsCounter/releases/latest/download/ResoniteMetricsCounter.dll) into your `rml_mods` folder. This folder should be located at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a standard installation. You can create it if it's missing, or if you start the game once with the ResoniteModLoader installed it will create this folder for you.
1. Launch the game. If you want to check that the mod is working you can check your Resonite logs.
1. Open the `Editor` category in the `Create Dialog`.
1. Press the `Start Metrics Counter (mod)` button.

## Development Requirements

For development, you will need the [ResoniteHotReloadLib](https://github.com/Nytra/ResoniteHotReloadLib) to be able to hot reload your mod with DEBUG build.
