using System;
using System.Numerics;
using NUnit.Framework;

namespace Assimp.Test
{
    [TestFixture]
    public class ThreeMFRecoveryTestFixture
    {
        [Test]
        public void TestShouldSkipPostProcessingWhenMeshesAreMissing()
        {
            Scene scene = new Scene();
            scene.RootNode = new Node("Root");

            IntPtr scenePtr = Scene.ToUnmanagedScene(scene);
            try
            {
                bool shouldSkip = ThreeMFRecovery.ShouldSkipPostProcessing(scenePtr, PostProcessSteps.Triangulate, out string message);

                Assert.That(shouldSkip, Is.True);
                Assert.That(message, Does.Contain("contains no native meshes"));
            }
            finally
            {
                Scene.FreeUnmanagedScene(scenePtr);
            }
        }

        [Test]
        public void TestRecoverSceneAttachesMeshToSingleLeafNode()
        {
            Scene scene = new Scene();
            scene.RootNode = new Node("Root");
            scene.RootNode.Children.Add(new Node("Object_2"));

            Mesh mesh = new Mesh("RecoveredMesh", PrimitiveType.Triangle);
            mesh.Vertices.Add(Vector3.Zero);
            mesh.Vertices.Add(Vector3.UnitX);
            mesh.Vertices.Add(Vector3.UnitY);
            mesh.Faces.Add(new Face(new [] { 0, 1, 2 }));
            scene.Meshes.Add(mesh);

            IntPtr scenePtr = Scene.ToUnmanagedScene(scene);
            try
            {
                bool recovered = ThreeMFRecovery.TryRecoverScene(scenePtr, scene, out string message);

                Assert.That(recovered, Is.True);
                Assert.That(message, Does.Contain("attached 1 orphaned mesh"));
                Assert.That(scene.RootNode.Children[0].MeshIndices, Is.EqualTo(new [] { 0 }));
            }
            finally
            {
                Scene.FreeUnmanagedScene(scenePtr);
            }
        }

        [Test]
        public void TestRecoverSceneCreatesRecoveryNodeWhenMappingIsAmbiguous()
        {
            Scene scene = new Scene();
            scene.RootNode = new Node("Root");
            scene.RootNode.Children.Add(new Node("Object_2"));
            scene.RootNode.Children.Add(new Node("Object_3"));

            for(int i = 0; i < 3; i++)
            {
                Mesh mesh = new Mesh($"RecoveredMesh{i}", PrimitiveType.Triangle);
                mesh.Vertices.Add(Vector3.Zero);
                mesh.Vertices.Add(Vector3.UnitX);
                mesh.Vertices.Add(Vector3.UnitY);
                mesh.Faces.Add(new Face(new [] { 0, 1, 2 }));
                scene.Meshes.Add(mesh);
            }

            IntPtr scenePtr = Scene.ToUnmanagedScene(scene);
            try
            {
                bool recovered = ThreeMFRecovery.TryRecoverScene(scenePtr, scene, out string message);

                Assert.That(recovered, Is.True);
                Assert.That(message, Does.Contain("attached 3 orphaned mesh"));
                Assert.That(scene.RootNode.ChildCount, Is.EqualTo(3));
                Assert.That(scene.RootNode.Children[2].Name, Is.EqualTo("Recovered3MFMeshes"));
                Assert.That(scene.RootNode.Children[2].MeshIndices, Is.EqualTo(new [] { 0, 1, 2 }));
            }
            finally
            {
                Scene.FreeUnmanagedScene(scenePtr);
            }
        }
    }
}
