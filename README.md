# 🛠 Coffee.Internal

**NOTE: This repository is for development purposes only.**

This repository contains internal classes and utilities for development.

Used in development and demos for the following packages:

- https://github.com/mob-sakai/ParticleEffectForUGUI
- https://github.com/mob-sakai/SoftMaskForUGUI
- https://github.com/mob-sakai/CompositeCanvasRenderer

## Install Development Tools

`manifest.json`

```json
{
  "dependencies": {
    "com.coffee.nano-monitor": "https://github.com/mob-sakai/Coffee.Internal.git?path=Packages/NanoMonitor",
    "com.coffee.simple-scene-navigator": "https://github.com/mob-sakai/Coffee.Internal.git?path=Packages/SceneNavigator",
    "com.coffee.development": "https://github.com/mob-sakai/Coffee.Internal.git?path=Packages/Development",
    ...
  }
}
```

### Nano Monitor

Zero-allocation stats monitor

<img alt="Nano Monitor" src="https://github.com/mob-sakai/mob-sakai/assets/12690315/73536425-58dc-433f-a88e-37646849a7c1" width="500"/>

### Scene Navigator

Easily switch between demo scenes

<img alt="Scene Navigator" src="https://github.com/mob-sakai/mob-sakai/assets/12690315/719c3087-a39b-41af-86ed-e1636104f172" width="500"/>

### Development

- Format name of GameObjects
- Enable detailed logging
- Remove MissingComponents

<img src="https://github.com/mob-sakai/mob-sakai/assets/12690315/0796554f-e01e-450a-a585-e66aab71c514" width="200"/>

## Pull/Push Development Sources

```sh
# pull cs files, coffee.internal -> package:
$ ./update.sh pull <package_dir>

# pull cs files, package -> coffee.internal:
$ ./update.sh push <package_dir>
```

```
<package_dir>
  ├─ Editor
  │  └─ Internal
  │     └─ AssetModification
  └─ Runtime
     └─ Internal
        ├─ Extensions
        ├─ ProjectSettings
        └─ Utilities
```
