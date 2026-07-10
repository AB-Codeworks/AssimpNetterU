# AssimpNetter — Unity Package

**AssimpNetter** is a maintained .NET wrapper for the [Open Asset Import Library (Assimp)](https://github.com/assimp/assimp), a 3D model import/export library supporting dozens of formats (FBX, GLTF, OBJ, Collada, 3DS, and more).

## Installation

### Via Unity Package Manager (UPM) — Git URL

1. Open **Window → Package Manager**.
2. Click the **+** button → **Add package from git URL…**
3. Enter:
   ```
   https://github.com/AB-Codeworks/AssimpNetterU.git#path:UnityPlugin/UPM
   ```

### Via `.unitypackage`

Download the latest `AssimpNetter.unitypackage` from the [Releases](https://github.com/AB-Codeworks/AssimpNetterU/releases) page and import it via **Assets → Import Package → Custom Package…**

## Supported Platforms

| Platform       | Architecture |
|----------------|-------------|
| Windows Editor | x64, ARM64  |
| Windows Player | x64, x86, ARM64 |
| Linux Editor   | x64, ARM64  |
| Linux Player   | x64, ARM64  |
| macOS Editor   | x64, ARM64  |
| macOS Player   | x64, ARM64  |

## Quick Start

The `AssimpUnity` class handles native library loading automatically. Check `AssimpUnity.IsAssimpAvailable` before using the library:

```csharp
using Assimp;

void Start()
{
    if (!AssimpUnity.IsAssimpAvailable)
    {
        Debug.LogError("Assimp native library failed to load on this platform.");
        return;
    }

    using var ctx = new AssimpContext();
    Scene scene = ctx.ImportFile(path, PostProcessSteps.Triangulate);
    Debug.Log($"Loaded {scene.MeshCount} meshes.");
}
```

## Licensing

- **AssimpNetter managed wrapper**: [MIT License](https://opensource.org/licenses/MIT) — Copyright © 2012-2020 Nicholas Woodfield, © 2024-2026 Salvage
- **Assimp native library**: [BSD 3-Clause License](https://opensource.org/licenses/BSD-3-Clause)
