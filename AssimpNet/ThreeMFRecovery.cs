using System;
using System.Collections.Generic;
using System.Linq;
using Assimp.Unmanaged;

namespace Assimp
{
    /// <summary>
    /// Provides 3MF-specific safeguards for malformed native scenes before they are exposed to managed code.
    /// </summary>
    public static class ThreeMFRecovery
    {
        /// <summary>
        /// Detects malformed 3MF scenes that are unsafe to send through Assimp post-processing.
        /// </summary>
        /// <param name="scenePtr">Pointer to the native scene.</param>
        /// <param name="postProcessFlags">Requested post-process flags.</param>
        /// <param name="message">Informational or warning message describing the decision.</param>
        /// <returns>True if post-processing should be skipped; otherwise false.</returns>
        public static bool ShouldSkipPostProcessing(IntPtr scenePtr, PostProcessSteps postProcessFlags, out string message)
        {
            message = null;

            if(scenePtr == IntPtr.Zero || postProcessFlags == PostProcessSteps.None)
                return false;

            NativeSceneInfo sceneInfo = InspectScene(scenePtr);

            if(sceneInfo.MeshCount == 0)
            {
                message = "Warning: 3MF recovery skipped post-processing because the imported scene contains no native meshes.";
                return true;
            }

            if(sceneInfo.OrphanedMeshIndices.Count > 0)
            {
                message = $"Warning: 3MF recovery skipped post-processing because {sceneInfo.OrphanedMeshIndices.Count} mesh(es) are not attached to any node.";
                return true;
            }

            return false;
        }

        /// <summary>
        /// Repairs managed mesh-to-node links for native 3MF scenes whose meshes were imported but not attached to nodes.
        /// </summary>
        /// <param name="scenePtr">Pointer to the native scene.</param>
        /// <param name="scene">Managed scene to repair.</param>
        /// <param name="message">Informational or warning message describing the outcome.</param>
        /// <returns>True if one or more orphaned meshes were attached; otherwise false.</returns>
        public static bool TryRecoverScene(IntPtr scenePtr, Scene scene, out string message)
        {
            message = null;

            if(scenePtr == IntPtr.Zero || scene == null || scene.RootNode == null)
                return false;

            NativeSceneInfo sceneInfo = InspectScene(scenePtr);

            if(sceneInfo.MeshCount == 0)
            {
                message = "Warning: 3MF recovery could not find any native meshes to recover.";
                return false;
            }

            if(scene.MeshCount == 0)
            {
                message = "Warning: 3MF recovery found native mesh slots, but no managed meshes were produced.";
                return false;
            }

            List<int> orphanedMeshIndices = sceneInfo.OrphanedMeshIndices
                .Where(index => index >= 0 && index < scene.MeshCount)
                .ToList();

            if(orphanedMeshIndices.Count == 0)
                return false;

            int recoveredMeshCount = AttachOrphanedMeshes(scene, orphanedMeshIndices);
            if(recoveredMeshCount == 0)
            {
                message = $"Warning: 3MF recovery found {orphanedMeshIndices.Count} orphaned mesh(es), but could not attach them to the node hierarchy.";
                return false;
            }

            message = $"Info: 3MF recovery attached {recoveredMeshCount} orphaned mesh(es) to the node hierarchy.";
            return true;
        }

        private static int AttachOrphanedMeshes(Scene scene, List<int> orphanedMeshIndices)
        {
            List<Node> leafNodesWithoutMeshes = new List<Node>();
            CollectLeafNodesWithoutMeshes(scene.RootNode, leafNodesWithoutMeshes);

            if(leafNodesWithoutMeshes.Count == orphanedMeshIndices.Count)
            {
                for(int i = 0; i < orphanedMeshIndices.Count; i++)
                {
                    leafNodesWithoutMeshes[i].MeshIndices.Add(orphanedMeshIndices[i]);
                }

                return orphanedMeshIndices.Count;
            }

            if(leafNodesWithoutMeshes.Count == 1)
            {
                leafNodesWithoutMeshes[0].MeshIndices.AddRange(orphanedMeshIndices);
                return orphanedMeshIndices.Count;
            }

            Node recoveryNode = new Node("RecoveredMeshes_3MF");
            recoveryNode.MeshIndices.AddRange(orphanedMeshIndices);
            scene.RootNode.Children.Add(recoveryNode);
            return orphanedMeshIndices.Count;
        }

        private static void CollectLeafNodesWithoutMeshes(Node node, List<Node> nodes)
        {
            if(node == null)
                return;

            if(!node.HasChildren && !node.HasMeshes)
            {
                nodes.Add(node);
                return;
            }

            foreach(Node child in node.Children)
            {
                CollectLeafNodesWithoutMeshes(child, nodes);
            }
        }

        private static NativeSceneInfo InspectScene(IntPtr scenePtr)
        {
            AiScene aiScene = MemoryHelper.MarshalStructure<AiScene>(scenePtr);
            HashSet<int> referencedMeshIndices = new HashSet<int>();

            if(aiScene.RootNode != IntPtr.Zero)
            {
                CollectReferencedMeshIndices(aiScene.RootNode, referencedMeshIndices);
            }

            List<int> orphanedMeshIndices = new List<int>();
            for(int i = 0; i < aiScene.NumMeshes; i++)
            {
                if(!referencedMeshIndices.Contains(i))
                    orphanedMeshIndices.Add(i);
            }

            return new NativeSceneInfo((int) aiScene.NumMeshes, orphanedMeshIndices);
        }

        private static void CollectReferencedMeshIndices(IntPtr nodePtr, HashSet<int> referencedMeshIndices)
        {
            if(nodePtr == IntPtr.Zero)
                return;

            AiNode node = MemoryHelper.MarshalStructure<AiNode>(nodePtr);

            if(node.NumMeshes > 0 && node.Meshes != IntPtr.Zero)
            {
                int[] meshIndices = MemoryHelper.FromNativeArray<int>(node.Meshes, (int) node.NumMeshes);
                foreach(int meshIndex in meshIndices)
                {
                    referencedMeshIndices.Add(meshIndex);
                }
            }

            if(node.NumChildren == 0 || node.Children == IntPtr.Zero)
                return;

            IntPtr[] children = MemoryHelper.FromNativeArray<IntPtr>(node.Children, (int) node.NumChildren);
            foreach(IntPtr child in children)
            {
                CollectReferencedMeshIndices(child, referencedMeshIndices);
            }
        }

        private readonly struct NativeSceneInfo
        {
            public NativeSceneInfo(int meshCount, List<int> orphanedMeshIndices)
            {
                MeshCount = meshCount;
                OrphanedMeshIndices = orphanedMeshIndices ?? new List<int>();
            }

            public int MeshCount { get; }

            public List<int> OrphanedMeshIndices { get; }
        }
    }
}
