/*
 * Copyright (c) 2012-2020 AssimpNet - Nicholas Woodfield
 * Copyright (c) 2024-2026 AssimpNetter - Stefan Koch, AB-Codeworks
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Assimp.Unmanaged;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assimp
{
    /// <summary>
    /// AssimpNetter Unity integration. Handles one-time initialization (before scene load) of the
    /// <see cref="AssimpLibrary"/> instance, setting native DLL probing paths for the current platform.
    /// </summary>
    /// <remarks>
    /// In the Unity Editor the native library is loaded from the UPM package's
    /// <c>Runtime/Plugins/{platform}/</c> folder. In standalone player builds Unity copies the
    /// appropriate native binary to <c>{ApplicationDataPath}/Plugins/</c>.
    /// </remarks>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class AssimpUnity
    {
        private static bool s_triedLoading;
        private static bool s_assimpAvailable;

        /// <summary>
        /// Gets whether the native Assimp library was successfully loaded on the current platform.
        /// </summary>
        public static bool IsAssimpAvailable => s_assimpAvailable;

        // Editor domain-reload entry point.
        static AssimpUnity()
        {
            InitializePlugin();
        }

        // Runtime entry point — called once before the first scene loads.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializePlugin()
        {
            if (s_triedLoading)
                return;

            UnmanagedLibrary libInstance = AssimpLibrary.Instance;

            // Already loaded (e.g. another subsystem loaded it first).
            if (libInstance.IsLibraryLoaded)
            {
                s_assimpAvailable = true;
                s_triedLoading = true;
                return;
            }

            // In the editor, [CallerFilePath] gives us the compile-time path of this source file
            // inside the UPM package cache, which is a real path we can navigate from.
            // In a standalone player build, the path no longer exists on disk, so we fall back
            // to Application.dataPath-relative paths that Unity populates during the build.
            string runtimeFolder = GetRuntimeFolder();
            string pluginsFolder = Path.Combine(Application.dataPath, "Plugins");

            string nativePath64 = null;
            string nativePath32 = null;
            string override64LibName = null;
            string override32LibName = null;

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    nativePath64 = Path.Combine(runtimeFolder, "Plugins",
                        RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                            ? "win-arm64"
                            : "win-x64");
                    nativePath32 = Path.Combine(runtimeFolder, "Plugins", "win-x86");
                    break;

                case RuntimePlatform.WindowsPlayer:
                    nativePath64 = pluginsFolder;
                    nativePath32 = pluginsFolder;
                    break;

                case RuntimePlatform.LinuxEditor:
                    nativePath64 = Path.Combine(runtimeFolder, "Plugins",
                        RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                            ? "linux-arm64"
                            : "linux-x64");
                    // Linux 32-bit is not supported; leave nativePath32 null.
                    break;

                case RuntimePlatform.LinuxPlayer:
                    nativePath64 = Path.Combine(pluginsFolder, "x86_64");
                    nativePath32 = Path.Combine(pluginsFolder, "x86");
                    break;

                case RuntimePlatform.OSXEditor:
                    nativePath64 = Path.Combine(runtimeFolder, "Plugins",
                        RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                            ? "osx-arm64"
                            : "osx-x64");
                    // Unity requires macOS dylibs to be named *.bundle for plugin import.
                    override64LibName = "libassimp.bundle";
                    override32LibName = "libassimp.bundle";
                    break;

                case RuntimePlatform.OSXPlayer:
                    nativePath64 = pluginsFolder;
                    nativePath32 = pluginsFolder;
                    override64LibName = "libassimp.bundle";
                    override32LibName = "libassimp.bundle";
                    break;
            }

            if (nativePath64 == null && nativePath32 == null)
            {
                Debug.LogWarning($"[AssimpNetter] Platform not supported: {Application.platform}");
                s_triedLoading = true;
                return;
            }

            libInstance.Resolver.SetOverrideLibraryName64(override64LibName);
            libInstance.Resolver.SetOverrideLibraryName32(override32LibName);
            libInstance.Resolver.SetProbingPaths64(nativePath64);
            libInstance.Resolver.SetProbingPaths32(nativePath32);
            libInstance.ThrowOnLoadFailure = false;

            s_assimpAvailable = libInstance.LoadLibrary();
            s_triedLoading = true;

            // Restore default behaviour so later explicit load calls throw on failure.
            libInstance.ThrowOnLoadFailure = true;

            if (s_assimpAvailable)
                Debug.Log($"[AssimpNetter] Native library loaded successfully on {Application.platform}.");
            else
                Debug.LogWarning($"[AssimpNetter] Failed to load native library on {Application.platform}.");
        }

        /// <summary>
        /// Returns the <c>Runtime/</c> folder of this UPM package by using the compile-time path
        /// of this source file. In a standalone player build the path will not exist on disk;
        /// callers should fall back to <see cref="Application.dataPath"/> in that case.
        /// </summary>
        private static string GetRuntimeFolder([CallerFilePath] string filePath = "")
        {
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                return Path.GetDirectoryName(filePath);

            // Fallback: not in a package-cache context (e.g. embedded package, standalone build).
            return Application.dataPath;
        }
    }
}
