using D3DLab.ECS;
using D3DLab.ECS.Components;
using System.IO;
using System.Numerics;

namespace D3DLab.Std.Engine.Core.Components.Materials {
       
  

    public class GradientMaterialComponent : MaterialComponent {
        public Vector4 Apex { get; set; }
        public Vector4 Center { get; set; }

        public GradientMaterialComponent() {
            // Set the Pink color at the top of the sky dome.
            Apex = new Vector4(0.0f, 0.15f, 0.66f, 1.0f); ;
            // Set the Blue color at the center of the sky dome.
            Center = new Vector4(0.81f, 0.38f, 0.66f, 1.0f);
            IsModified = true;
        }
    }

    

    
    

    public class MaterialComponent1 : GraphicComponent {
        public float Specular { get; set; } = -1; // -1 not specular

        public Vector4 AmbientColor { get; set; }
        public Vector4 DiffuseColor { get; set; }

    }

   
}
