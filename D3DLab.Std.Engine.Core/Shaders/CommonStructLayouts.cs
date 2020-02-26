using D3DLab.ECS;
using D3DLab.ECS.Camera;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace D3DLab.Std.Engine.Core.Shaders {
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct GameStructBuffer {

        public static GameStructBuffer FromCameraState(CameraState state) {
            return new GameStructBuffer(
                      Matrix4x4.Transpose(state.ViewMatrix),
                      Matrix4x4.Transpose(state.ProjectionMatrix),
                      state.LookDirection,
                      state.Position);
        }

        public const int RegisterResourceSlot = 0;

        public readonly Vector4 LookDirection;
        public readonly Vector4 CameraPosition;

        public readonly Matrix4x4 View;
        public readonly Matrix4x4 Projection;

      //  

        public GameStructBuffer(Matrix4x4 view, Matrix4x4 proj, Vector3 lookDirection, Vector3 pos) {
            View = view;
            Projection = proj;
            LookDirection = new Vector4(lookDirection,0);// Matrix4x4.Identity ;// lookDirection;
            CameraPosition = new Vector4(pos,1);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 64)]//32 + /* Color vector4 16 */ 16 + ???
    //[StructLayout(LayoutKind.Sequential)]
    public struct LightStructBuffer {

        public static LightStructBuffer From(LightState state) {
            return new LightStructBuffer(state.Type, state.Position, state.Direction, state.Color, state.Intensity);
        }

        public const int MaxCount = 3;
        public const int RegisterResourceSlot = 1;

        [FieldOffset(0)]
        public readonly uint Type;
        [FieldOffset(4)] // + uint 4
        public readonly float Intensity;
        [FieldOffset(8)] // + float 4
        public readonly Vector3 Position;
        [FieldOffset(20)] // + vector3 12
        public readonly Vector3 Direction;
        [FieldOffset(32)] // + vector3 12
        public readonly Vector4 Color;

        public LightStructBuffer(LightTypes type, Vector3 pos, Vector3 dir, Vector4 color, float intensity) {
            Type = (uint)type;
            Intensity = intensity;
            Position = pos;
            Direction = dir;
            Color = color;
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TransforStructBuffer {
        public static TransforStructBuffer ToTranspose(Matrix4x4 world) {
            Matrix4x4.Invert(world, out var inverted);
            return new TransforStructBuffer(Matrix4x4.Transpose(world), Matrix4x4.Transpose(inverted));
        }

        public const int RegisterResourceSlot = 2;
        public readonly Matrix4x4 World;
        public readonly Matrix4x4 WorldInverse;

        TransforStructBuffer(Matrix4x4 world, Matrix4x4 inverse) {
            World = world;
            WorldInverse = inverse;
        }
    }

}
