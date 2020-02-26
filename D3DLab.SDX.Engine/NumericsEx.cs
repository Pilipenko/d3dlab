namespace D3DLab.SDX.Engine {
    public static class SDXMathematicsEx {
        public static SharpDX.Vector3 ToSDXVector3(this System.Numerics.Vector3 v) {
            return new SharpDX.Vector3(v.X, v.Y, v.Z);
        }

        public static SharpDX.Matrix ToSDXMatrix(this System.Numerics.Matrix4x4 m) {
            return new SharpDX.Matrix(
                m.M11,
                m.M12,
                m.M13,
                m.M14,
                m.M21,
                m.M22,
                m.M23,
                m.M24,
                m.M31,
                m.M32,
                m.M33,
                m.M34,
                m.M41,
                m.M42,
                m.M43,
                m.M44);
        }

    }

    public static class NumericsEx {
        public static System.Numerics.Vector4 ToNumericV4(this SharpDX.Vector4 v4) {
            return new System.Numerics.Vector4(v4.X, v4.Y, v4.Z, v4.W);
        }
        public static System.Numerics.Vector4 ToNumericV4(this SharpDX.Color color) {
            return new System.Numerics.Vector4(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            //return new System.Numerics.Vector4(color.R, color.G, color.B, color.A);
        }

        public static System.Numerics.Vector3 ToNVector3(this SharpDX.Vector3 v) {
            return new System.Numerics.Vector3(v.X, v.Y, v.Z);
        }

        public static System.Numerics.Vector2 ToNVector2(this SharpDX.Vector2 v) {
            return new System.Numerics.Vector2(v.X, v.Y);
        }

        public static System.Numerics.Matrix4x4 ToMatrix4x4(this SharpDX.Matrix m) {
            return new System.Numerics.Matrix4x4(
                m.M11,
                m.M12,
                m.M13,
                m.M14,
                m.M21,
                m.M22,
                m.M23,
                m.M24,
                m.M31,
                m.M32,
                m.M33,
                m.M34,
                m.M41,
                m.M42,
                m.M43,
                m.M44);
        }



        class Color {
            private static byte ToByte(float component) {
                var value = (int)(component * 255.0f);
                return ToByte(value);
            }

            public static byte ToByte(int value) {
                return (byte)(value < 0 ? 0 : value > 255 ? 255 : value);
            }
        }
    }
}


