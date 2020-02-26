using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace D3DLab.ECS.Ext {
    public static class MatrixEx {
        /// <summary>
        /// Shouls be faster that Invert, but not sure :)
        /// </summary>
        /// <param name="viewMatrix"></param>
        /// <returns></returns>
        public static Matrix4x4 PsudoInverted(this Matrix4x4 viewMatrix) {
            //var v33Transpose = new Matrix3x3(
            //    viewMatrix.M11, viewMatrix.M21, viewMatrix.M31,
            //    viewMatrix.M12, viewMatrix.M22, viewMatrix.M32,
            //    viewMatrix.M13, viewMatrix.M23, viewMatrix.M33);

            //var vpos = viewMatrix.Row4.ToVector3();

            //     vpos = Vector3.Transform(vpos, v33Transpose) * -1;

            var x = viewMatrix.M41 * viewMatrix.M11 + viewMatrix.M42 * viewMatrix.M12 + viewMatrix.M43 * viewMatrix.M13;
            var y = viewMatrix.M41 * viewMatrix.M21 + viewMatrix.M42 * viewMatrix.M22 + viewMatrix.M43 * viewMatrix.M23;
            var z = viewMatrix.M41 * viewMatrix.M31 + viewMatrix.M42 * viewMatrix.M32 + viewMatrix.M43 * viewMatrix.M33;

            return new Matrix4x4(
                viewMatrix.M11, viewMatrix.M21, viewMatrix.M31, 0,
                viewMatrix.M12, viewMatrix.M22, viewMatrix.M32, 0,
                viewMatrix.M13, viewMatrix.M23, viewMatrix.M33, 0, -x, -y, -z, 1);
        }
        /// <summary>
        /// See remarks!!
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        /// <remarks>
        /// return Identity if Matrix4x4 can't be inverted
        /// </remarks>
        public static Matrix4x4 Inverted(this Matrix4x4 m) {
            if(Matrix4x4.Invert(m, out var inverted)) {
                return inverted;
            }
            return Matrix4x4.Identity;
        }
    }
}
