using D3DLab.ECS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace D3DLab.ECS.Components{
    
    [Obsolete("Most of all material components should be a struct, class is extra case")]
    public abstract class MaterialComponent : GraphicComponent {

    }

    public class TexturedMaterialComponent : MaterialComponent {
        /// <summary>
        /// Order is important! the same order will be setted in shader recources
        /// </summary>
        public FileInfo[] Images { get; }

        public TexturedMaterialComponent(params FileInfo[] image) {
            Images = image;
        }


    }


}
