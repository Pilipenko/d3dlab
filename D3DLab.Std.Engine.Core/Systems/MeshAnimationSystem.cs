using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using D3DLab.ECS;
using D3DLab.ECS.Ext;
using D3DLab.SDX.Engine.Animation;
using D3DLab.Std.Engine.Core.Animation;
using D3DLab.Std.Engine.Core.Animation.Formats;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Utilities;

namespace D3DLab.Std.Engine.Core.Systems {
    class AnimationHittableGeometryComponent : HittableGeometryComponent {
    }

    class OrientationComponent : GraphicComponent, IStickOnHeightMapComponent {
        public Vector3 AxisUpLocal { get; set; }
        public Vector3 AttachPointLocal { get; set; }
        public Vector3 FrontDirectionLocal { get; set; }
        public OrientationComponent() {
            
        }
    }

    public class MeshAnimationSystem : BaseEntitySystem, IGraphicSystem, IGraphicSystemContextDependent {
        static int Size = Unsafe.SizeOf<Matrix4x4>() * MaxBones;
        public const int MaxBones = 1024;

        public MeshAnimationSystem() {

        }

        public double CurrentAnimationTime { get; private set; }
        public IContextState ContextState { get; set; }

        protected override void Executing(ISceneSnapshot ss) {
            var snapshot = (SceneSnapshot)ss;
            var emanager = ContextState.GetEntityManager();

            foreach (var entity in emanager.GetEntities()) {

                var meshes = entity.GetComponents<CMOAnimateMeshComponent>();
                if (meshes.Any()) {
                    var mesh = meshes.First();
                    var anim = entity.GetComponent<MeshAnimationComponent>();
                    if (mesh.Bones.IsNull()) { continue; }

                    CurrentAnimationTime += snapshot.FrameRateTime.TotalMilliseconds / 1000.0f;
                    var currentAnimation = mesh.Animations[anim.AnimationName];

                    anim.Bones = CalculateBonesMatrix(mesh, currentAnimation);
                    anim.IsModified = true;

                    var hasGeo = entity.GetComponents<AnimationHittableGeometryComponent>();
                    
                    if (!hasGeo.Any()) {
                        entity.AddComponent(ConstructMesh(anim.Bones, mesh));
                    } else if(hasGeo.First().Tree.IsBuilt && !entity.Has<OrientationComponent>()) {
                        var c = new XZPlaneMovementComponent {
                            
                        };
                        entity.AddComponents(CreateStickOnComponent(hasGeo.First()), c);

                    }
                }
            }
        }

        /// <summary>
        /// calculate bones matrix each render frame
        /// </summary>
        /// <param name="AnimatedMesh"></param>
        /// <param name="currentAnimation"></param>
        /// <returns></returns>
        Matrix4x4[] CalculateBonesMatrix(CMOAnimateMeshComponent AnimatedMesh, CMOAnimateMeshComponent.Animation currentAnimation) {
            Matrix4x4[] bones = new Matrix4x4[MaxBones];
            // Retrieve each bone's local transform
            for (var i = 0; i < AnimatedMesh.Bones.Count; i++) {
                bones[i] = AnimatedMesh.Bones[i].BoneLocalTransform;
            }

            // Keep track of the last key-frame used for each bone
            CMOAnimateMeshComponent.Keyframe?[] lastKeyForBones = new CMOAnimateMeshComponent.Keyframe?[AnimatedMesh.Bones.Count];
            // Keep track of whether a bone has been interpolated
            bool[] lerpedBones = new bool[AnimatedMesh.Bones.Count];
            for (var i = 0; i < currentAnimation.Keyframes.Count; i++) {
                // Retrieve current key-frame
                var frame = currentAnimation.Keyframes[i];

                // If the current frame is not in the future
                if (frame.Time <= CurrentAnimationTime) {
                    // Keep track of last key-frame for bone
                    lastKeyForBones[frame.BoneIndex] = frame;
                    // Retrieve transform from current key-frame
                    bones[frame.BoneIndex] = frame.Transform;
                } else {// Frame is in the future, check if we should interpolate
                    // Only interpolate a bone's key-frames ONCE
                    if (!lerpedBones[frame.BoneIndex]) {
                        // Retrieve the previous key-frame if exists
                        CMOAnimateMeshComponent.Keyframe prevFrame;
                        if (lastKeyForBones[frame.BoneIndex] != null) {
                            prevFrame = lastKeyForBones[frame.BoneIndex].Value;
                        } else {
                            continue; // nothing to interpolate
                                      // Make sure we only interpolate with 
                                      // one future frame for this bone
                        }
                        lerpedBones[frame.BoneIndex] = true;

                        // Calculate time difference between frames
                        var frameLength = frame.Time - prevFrame.Time;
                        var timeDiff = CurrentAnimationTime - prevFrame.Time;
                        float amount = (float)timeDiff / frameLength;

                        // Interpolation using Lerp on scale and translation, and Slerp on Rotation (Quaternion)
                        Vector3 t1, t2;   // Translation
                        Quaternion q1, q2;// Rotation
                        Vector3 s1, s2;     // Scale
                                            // Decompose the previous key-frame's transform

                        //SharpDX.Matrix.DecomposeUniformScale(out s1, out q1, out t1);
                        Matrix4x4.Decompose(prevFrame.Transform, out s1, out q1, out t1);

                        // Decompose the current key-frame's transform
                        //frame.Transform.DecomposeUniformScale(out s2, out q2, out t2);
                        Matrix4x4.Decompose(frame.Transform, out s2, out q2, out t2);

                        // Perform interpolation and reconstitute matrix
                        bones[frame.BoneIndex] =
                            Matrix4x4.CreateScale((float)MathUtil.Lerp(s1.X, s2.X, amount)) *
                            Matrix4x4.CreateFromQuaternion(Quaternion.Slerp(q1, q2, amount)) *
                            Matrix4x4.CreateTranslation(Vector3.Lerp(t1, t2, amount));
                    }
                }
            }

            // Apply parent bone transforms
            // We assume here that the first bone has no parent
            // and that each parent bone appears before children
            for (var i = 1; i < AnimatedMesh.Bones.Count; i++) {
                var bone = AnimatedMesh.Bones[i];
                if (bone.ParentIndex > -1) {
                    var parentTransform = bones[bone.ParentIndex];
                    bones[i] = (bones[i] * parentTransform);
                }
            }

            // Change the bone transform from rest pose space into bone space (using the inverse of the bind/rest pose)
            for (var i = 0; i < AnimatedMesh.Bones.Count; i++) {
                bones[i] = Matrix4x4.Transpose(AnimatedMesh.Bones[i].InvBindPose * bones[i]);
            }

            // Check need to loop animation
            if (currentAnimation.EndTime <= CurrentAnimationTime) {
                CurrentAnimationTime = 0;
            }

            return bones;
        }

        /// <summary>
        /// Calculate mesh only once ... no worry about animation, no accurate bounding box
        /// </summary>
        /// <param name="bones"></param>
        /// <param name="mesh"></param>
        /// <returns></returns>
        /// <remarks>
        /// https://gamedev.stackexchange.com/questions/46332/mesh-manipulation-on-gpu-vs-cpu
        /// https://gamedev.stackexchange.com/questions/43986/calculate-an-aabb-for-bone-animated-model
        /// </remarks>
        AnimationHittableGeometryComponent ConstructMesh(Matrix4x4[] bones, CMOAnimateMeshComponent mesh) {
            var pos = new List<Vector3>();
            var norms = new List<Vector3>();
            var indeces = new List<int>();

            for (int indx = 0; indx < mesh.VertexBuffers.Count; indx++) {
                var vb = mesh.VertexBuffers[indx];
                var vertices = new Vertex[vb.Length];
                for (var i = 0; i < vb.Length; i++) {
                    // Retrieve skinning information for vertex
                    var skin = new SkinningVertex();
                    if (mesh.SkinningVertexBuffers.Count > 0) {
                        skin = mesh.SkinningVertexBuffers[indx][i];
                    }

                    // Create vertex
                    vertices[i] = new Vertex(vb[i].Position, vb[i].Normal, (Vector4)vb[i].Color, vb[i].UV, skin);
                    //the same matrix as in animation.hlsl
                    var skinTransform = Matrix4x4.Transpose(
                        bones[skin.BoneIndex0] * skin.BoneWeight0 +
                        bones[skin.BoneIndex1] * skin.BoneWeight1 +
                        bones[skin.BoneIndex2] * skin.BoneWeight2 +
                        bones[skin.BoneIndex3] * skin.BoneWeight3);

                    pos.Add(Vector3.Transform(vb[i].Position, skinTransform));
                    norms.Add(Vector3.TransformNormal(vb[i].Normal, skinTransform));
                }
            }

            var offset = 0;
            // Initialize index buffers
            for (var i = 0; i < mesh.IndexBuffers.Count; i++) {
                var ib = mesh.IndexBuffers[i];
                foreach (var ii in ib) {
                    indeces.Add(offset + (int)ii);
                }
                offset += mesh.VertexBuffers[i].Length;
            }

            var geo = new AnimationHittableGeometryComponent();
            geo.Positions = pos.ToImmutableArray();
            geo.Indices = indeces.ToImmutableArray();
            geo.Normals = norms.ToImmutableArray();
            geo.IsModified = true;

            return geo;
        }

        OrientationComponent CreateStickOnComponent(AnimationHittableGeometryComponent geo) {
            var box = geo.Box;
            var com = new OrientationComponent();

            com.AxisUpLocal = -Vector3.UnitZ;//local axis of current anim mesh by default
            com.AttachPointLocal = box.GetCenter() + com.AxisUpLocal * box.Size().Z/2;//the bottom point of Box for stick on terrain
            com.FrontDirectionLocal = Vector3.UnitY;

            return com;
        }
    }
}
