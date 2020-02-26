## Overview

One of the thousands attempts to implement own game engine with blackjack and etc. There was no goal to do engine for a specific game or make a mega universal engine. The main idea to do ECS based engine using DirectX. After finishing the main implementation it was too sad to throw all away and it became development new features just for fun.

## Solution structure:

Engine.Core - .NET Standard, no dependency from any graphics API such as DirectX etc

SDX.Engine - wrapper over SharpDX, incapsulate SharpDX handling and simplify public ECS API based on Core.

Debugger - NET Framework + WPF, Shader Editor based on AvalonEdit, specific OBJ debuger, Script console (Roslin), visual tree viewer, ECS Component properties editor.

#### ECS Component properties editor
![Component properties editor](/readmi/com_pr_editor.PNG?raw=true "Properties editor")

#### Shader editor
![Shader editor](/readmi/shader_editor.PNG?raw=true "Properties editor")


The rest of them is just a test projects.

## Third party dependencies
* [SharpDX](https://github.com/sharpdx/SharpDX)
* [geometry3Sharp](https://github.com/gradientspace/geometry3Sharp)
* [SharpNoise](https://github.com/rthome/SharpNoise)
* [AvalonEdit](https://github.com/icsharpcode/AvalonEdit)
