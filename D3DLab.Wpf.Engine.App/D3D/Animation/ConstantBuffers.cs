using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using D3DLab.Std.Engine.Core.Animation.Formats;
using D3DLab.Std.Engine.Core.Common;

namespace D3DLab.SDX.Engine.Animation {
    
    public static class ConstantBuffers {
        /// <summary>
        /// Per Object constant buffer (matrices)
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct PerObject {
            public const int Slot = 0;
            // WorldViewProjection matrix
            public Matrix4x4 WorldViewProjection;

            // We need the world matrix so that we can
            // calculate the lighting in world space
            public Matrix4x4 World;

            // Inverse transpose of World
            public Matrix4x4 WorldInverseTranspose;

            /// <summary>
            /// Transpose the matrices so that they are in row major order for HLSL
            /// </summary>
            internal void Transpose() {
                this.World = Matrix4x4.Transpose(this.World);
                this.WorldInverseTranspose = Matrix4x4.Transpose(this.WorldInverseTranspose);
                this.WorldViewProjection = Matrix4x4.Transpose(this.WorldViewProjection);
            }
        }

        /// <summary>
        /// Directional light
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DirectionalLight {
            public Vector4 Color;
            public Vector3 Direction;
            float _padding0;
        }

        /// <summary>
        /// Per frame constant buffer (camera position)
        /// </summary>        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PerFrame {
            public const int Slot = 1;
            public DirectionalLight Light;
            public Vector3 CameraPosition;
            float _padding0;
        }

        /// <summary>
        /// Per material constant buffer
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PerMaterial {
            public const int Slot = 3;
            public Vector4 Ambient;
            public Vector4 Diffuse;
            public Vector4 Specular;
            public float SpecularPower;
            public uint HasTexture;    // Does the current material have a texture (0 false, 1 true)
            Vector2 _padding0;
            public Vector4 Emissive;
            public Matrix4x4 UVTransform; // Support UV coordinate transformations
        }

        /// <summary>
        /// Per armature/skeleton constant buffer
        /// </summary>
        public class PerArmature {
            // The maximum number of bones supported
            public const int MaxBones = 1024;
            public Matrix4x4[] Bones;

            public PerArmature() {
                Bones = new Matrix4x4[MaxBones];
            }

            public static int Size() {
                return Unsafe.SizeOf<Matrix4x4>() * MaxBones;
            }
        }
    }
}
