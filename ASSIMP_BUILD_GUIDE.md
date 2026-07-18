# Assimp 6.0.5 Native Binaries Build Guide

This guide explains how to build Assimp 6.0.5 native binaries for all supported platforms.

## Overview

Assimp 6.0.5 must be built for each platform and the resulting binaries placed in `libs/Assimp/` according to the platform-specific folder structure:

- `libs/Assimp/linux-x64/libassimp.so`
- `libs/Assimp/linux-arm64/libassimp.so`
- `libs/Assimp/osx-x64/libassimp.dylib`
- `libs/Assimp/osx-arm64/libassimp.dylib`
- `libs/Assimp/win-x64/assimp.dll`
- `libs/Assimp/win-x86/assimp.dll`
- `libs/Assimp/win-arm64/assimp.dll`

## Prerequisites

- CMake 3.10 or later
- C++ compiler appropriate for the target platform
- Git

### Platform-Specific Requirements

**Linux:**
```bash
sudo apt-get install cmake git build-essential
```

**macOS:**
```bash
brew install cmake git
```

**Windows:**
- Visual Studio 2019 or later with C++ build tools
- CMake (download from cmake.org or via Chocolatey)
- Git

## Build Instructions

### 1. Clone Assimp 6.0.5

```bash
git clone --depth 1 --branch v6.0.5 https://github.com/assimp/assimp.git
cd assimp
```

### 2. Linux x64 (Already Built ✓)

The Linux x64 library has already been built and included.

### 3. Linux ARM64

On an ARM64 Linux system (or with cross-compilation):

```bash
mkdir -p build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON -DASSIMP_BUILD_TESTS=OFF
make -j4
# Copy: build/bin/libassimp.so.6.0.5 to ../libs/Assimp/linux-arm64/libassimp.so
```

### 4. macOS x64

```bash
mkdir -p build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON -DASSIMP_BUILD_TESTS=OFF
make -j4
# Copy: build/bin/libassimp.dylib to ../libs/Assimp/osx-x64/libassimp.dylib
```

### 5. macOS ARM64

```bash
mkdir -p build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON -DASSIMP_BUILD_TESTS=OFF \
  -DCMAKE_OSX_ARCHITECTURES=arm64
make -j4
# Copy: build/bin/libassimp.dylib to ../libs/Assimp/osx-arm64/libassimp.dylib
```

### 6. Windows x64

Using Visual Studio:

```cmd
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON -DASSIMP_BUILD_TESTS=OFF -G "Visual Studio 16 2019" -A x64
cmake --build . --config Release
REM Copy: build/bin/Release/assimp.dll to ../libs/Assimp/win-x64/assimp.dll
```

### 7. Windows x86

Using Visual Studio:

```cmd
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON -DASSIMP_BUILD_TESTS=OFF -G "Visual Studio 16 2019" -A Win32
cmake --build . --config Release
REM Copy: build/bin/Release/assimp.dll to ../libs/Assimp/win-x86/assimp.dll
```

### 8. Windows ARM64

Using Visual Studio:

```cmd
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release -DBUILD_SHARED_LIBS=ON -DASSIMP_BUILD_TESTS=OFF -G "Visual Studio 16 2019" -A ARM64
cmake --build . --config Release
REM Copy: build/bin/Release/assimp.dll to ../libs/Assimp/win-arm64/assimp.dll
```

## CMake Configuration Notes

Key CMake flags used:
- `-DCMAKE_BUILD_TYPE=Release`: Build in release mode for optimized binaries
- `-DBUILD_SHARED_LIBS=ON`: Build as shared library (.so, .dylib, .dll)
- `-DASSIMP_BUILD_TESTS=OFF`: Skip building tests to save time

## Verification

After updating binaries, verify the build works on each platform:

```bash
dotnet test AssimpNet.Test -c Release
```

All tests should pass with the message: `Passed! - Failed: 0, Passed: 41, Skipped: 1`

## Key Changes in Assimp 6.0.5

The upgrade from 5.4.3 to 6.0.5 includes:
- **PR #5338**: Fix for out-of-bounds vertex array access
- **PR #5306**: Checks for invalid indices
- **PR #5318**: Fixed empty mesh handling
- Improved buffer overflow protection in various loaders
- Enhanced safety for BLEND file importing

### Note on BLEND Import Errors

If you encounter "BLEND: Number of vertices is larger than the corresponding array", this indicates the Blender file has corrupted or inconsistent metadata (the `totloop` count doesn't match the `mloop` array size).

**Solutions:**
1. Repair the Blender file: Open in Blender, run Mesh > Clean Up, and re-export
2. Export to FBX/OBJ format from Blender and use that instead
3. Check that the .blend file wasn't corrupted during transfer

## Notes

- Binary files should be committed to the repository
- Each platform's binary should be built on or for its target platform for best compatibility
- The managed wrapper (AssimpNetter.dll) is version 6.0.5.0 and matches these native binaries
