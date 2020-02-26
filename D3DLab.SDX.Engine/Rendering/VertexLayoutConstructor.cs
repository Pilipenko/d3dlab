using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.SDX.Engine.Rendering {
    public class VertexLayoutConstructor {
        
        const string SemanticSV_PositionName = "SV_Position";
        const string SemanticPositionName = "POSITION";
        const string SemanticNormalName = "NORMAL";
        const string SemanticColorName = "COLOR";
        const string SemanticTexCoorName = "TEXCOORD";
        const string SemanticTangentName = "TANGENT";
        const string SemanticBinormalName = "BINORMAL";
        const string SemanticBlendIndicesName = "BLENDINDICES";
        const string SemanticBlendWeightName = "BLENDWEIGHT";


        const InputClassification perverxdata = InputClassification.PerVertexData;
        const Format Vector3 = Format.R32G32B32_Float;
        const Format Vector4 = Format.R32G32B32A32_Float; 
        const Format Vector2 = Format.R32G32_Float;
        const Format UInt4 = Format.R32G32B32A32_UInt;

        int textcoorCount = 0;

        readonly List<InputElement> elements;
        public VertexLayoutConstructor() {
            elements = new List<InputElement>();
        }

        int GetOffset() {
            return elements.Count == 0 ? 0 : InputElement.AppendAligned;
        }

        public InputElement[] ConstuctElements() {
            return elements.ToArray();
        }

        /*
         * InputElement.InstanceDataStepRate 
         * The number of instances to draw using the same per-instance data before advancing in the buffer by one element. 
         * This value must be 0 for an element that contains per-vertex data (PerVertexData)
         */

        public VertexLayoutConstructor AddPositionElementAsVector3() {
            elements.Add(new InputElement(SemanticPositionName, 0, Vector3, GetOffset(), 0, perverxdata, 0));
            return this;
        }
        public VertexLayoutConstructor AddSVPositionElementAsVector4() {
            elements.Add(new InputElement(SemanticSV_PositionName, 0, Vector4, GetOffset(), 0, perverxdata, 0));
            return this;
        }
        public VertexLayoutConstructor AddPositionElementAsVector4() {
            elements.Add(new InputElement(SemanticPositionName, 0, Vector4, GetOffset(), 0, perverxdata, 0));
            return this;
        }

        public VertexLayoutConstructor AddNormalElementAsVector3() {
            elements.Add(new InputElement(SemanticNormalName, 0, Vector3, GetOffset(), 0, perverxdata, 0));
            return this;
        }

        public VertexLayoutConstructor AddColorElementAsVector4() {
            elements.Add(new InputElement(SemanticColorName, 0, Vector4, GetOffset(), 0, perverxdata, 0));
            return this;
        }
        public VertexLayoutConstructor AddColorElementAsVector3() { //can be Format.R8G8B8A8_UNorm
            elements.Add(new InputElement(SemanticColorName, 0, Vector3, GetOffset(), 0, perverxdata, 0));
            return this;
        }

        public VertexLayoutConstructor AddTexCoorElementAsVector2() {
            elements.Add(new InputElement(SemanticTexCoorName, textcoorCount, Vector2, GetOffset(), 0, perverxdata, 0));
            textcoorCount++;
            return this;
        }
        public VertexLayoutConstructor AddTexCoorElementAsVector4() {
            elements.Add(new InputElement(SemanticTexCoorName, textcoorCount, Vector4, GetOffset(), 0, perverxdata, 0));
            textcoorCount++;
            return this;
        }

        public VertexLayoutConstructor AddTangentElementAsVector3() {
            elements.Add(new InputElement(SemanticTangentName, 0, Vector3, GetOffset(), 0, perverxdata, 0));
            return this;
        }
        public VertexLayoutConstructor AddBinormalElementAsVector3() {
            elements.Add(new InputElement(SemanticBinormalName, 0, Vector3, GetOffset(), 0, perverxdata, 0));
            return this;
        }

        public VertexLayoutConstructor AddBlendIndicesElementAsVector4() {// can be  Format.R32G32B32A32_UInt
            elements.Add(new InputElement(SemanticBlendIndicesName, 0, Vector4, GetOffset(), 0, perverxdata, 0));
            return this;
        }
        public VertexLayoutConstructor AddBlendIndicesElementAsUInt4() {// can be  Format.R32G32B32A32_UInt
            elements.Add(new InputElement(SemanticBlendIndicesName, 0, UInt4, GetOffset(), 0, perverxdata, 0));
            return this;
        }
        public VertexLayoutConstructor AddBlendWeightElementAsVector4() {
            elements.Add(new InputElement(SemanticBlendWeightName, 0, Vector4, GetOffset(), 0, perverxdata, 0));
            return this;
        }

    }
}
