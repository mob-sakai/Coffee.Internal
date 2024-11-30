# 🛠 Coffee.Internal

**NOTE: This repository is for development purposes only.**

This repository contains internal classes and utilities for development.

Used in development and demos for the following packages:

- https://github.com/mob-sakai/ParticleEffectForUGUI
- https://github.com/mob-sakai/SoftMaskForUGUI
- https://github.com/mob-sakai/CompositeCanvasRenderer
- https://github.com/mob-sakai/UIEffect
- https://github.com/mob-sakai/UIPostProcessing

## Install Development Tools

`manifest.json`

```json
{
  "dependencies": {
    "com.coffee.nano-monitor": "https://github.com/mob-sakai/Coffee.Internal.git?path=Packages/NanoMonitor",
    "com.coffee.development": "https://github.com/mob-sakai/Coffee.Internal.git?path=Packages/Development",
    "com.coffee.open-sesame": "https://github.com/mob-sakai/Coffee.Internal.git?path=Packages/OpenSesame",
    "com.coffee.minimal-resource": "https://github.com/mob-sakai/Coffee.Internal.git?path=Packages/MinimalResource",
    ...
  }
}
```

### Nano Monitor

Zero-allocation stats monitor

<img alt="Nano Monitor" src="https://github.com/mob-sakai/mob-sakai/assets/12690315/73536425-58dc-433f-a88e-37646849a7c1" width="500"/>

### Open Sesame

### Minimal Resource

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
