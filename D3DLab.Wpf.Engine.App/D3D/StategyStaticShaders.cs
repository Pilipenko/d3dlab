using D3DLab.ECS.Shaders;
using D3DLab.SDX.Engine.Shader;
using D3DLab.Std.Engine.Core.Shaders;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace D3DLab.SDX.Engine.Rendering.Strategies {
    public static class StategyStaticShaders {

        #region simple shaders

 

        public const string pixelShaderNoLogicText =
@"
float4 main(float4 position : SV_POSITION, float4 color : COLOR) : SV_TARGET {
    return color;
}
";

        #endregion
        /*
        public static class LineVertex {
            const string path = @"D3DLab.SDX.Engine.Rendering.Shaders.Custom.Lines.hlsl";
            static readonly D3DShaderTechniquePass pass;
            static readonly VertexLayoutConstructor layconst;

            static LineVertex() {
                layconst = new VertexLayoutConstructor()
                   .AddPositionElementAsVector3()
                   .AddColorElementAsVector4();

                var d = new CombinedShadersLoader();
                pass = new D3DShaderTechniquePass(d.Load(path, "LV_"));
            }

            public static D3DShaderTechniquePass GetPasses() => pass;
            public static VertexLayoutConstructor GetLayoutConstructor() => layconst;

            [StructLayout(LayoutKind.Sequential)]
            public struct LineVertexColor {
                public readonly Vector3 Position;
                public readonly Vector4 Color;

                public LineVertexColor(Vector3 position, Vector4 color) {
                    Position = position;
                    Color = color;
                }
            }            
        }*/
        /*
        public static class ColoredVertexes {
            static readonly D3DShaderTechniquePass pass;
            static readonly VertexLayoutConstructor layconst;

            static ColoredVertexes() {
                pass = new D3DShaderTechniquePass(new IShaderInfo[] {
                    new ShaderInMemoryInfo("CV_VertexShader", vertexShaderText, null, ShaderStages.Vertex.ToString(), "main"),
                    new ShaderInMemoryInfo("CV_FragmentShader", pixelShaderText, null, ShaderStages.Fragment.ToString(), "main"),
                });
                layconst = new VertexLayoutConstructor()
                   .AddPositionElementAsVector3()
                   .AddNormalElementAsVector3()
                   .AddColorElementAsVector4();
            }

            public static IRenderTechniquePass GetPasses() => pass;
            public static VertexLayoutConstructor GetLayoutConstructor() => layconst;

            [StructLayout(LayoutKind.Sequential)]
            public struct VertexPositionColor {
                public readonly Vector3 Position;
                public readonly Vector3 Normal;
                public readonly Vector4 Color;

                public VertexPositionColor(Vector3 position, Vector3 normal, Vector4 color) {
                    Position = position;
                    Normal = normal;
                    Color = color;
                }
            }

            #region shaders

            const string vertexShaderText =
    @"
#include ""Game""
#include ""Light""

struct VSOut
{
    float4 position : SV_POSITION;
    float4 normal : NORMAL;
    float4 color : COLOR;
};
VSOut main(float4 position : POSITION, float3 normal : NORMAL, float4 color : COLOR) { 
    VSOut output = (VSOut)0;
    
    output.position = toWVP(position);

    output.normal = mul(normal,World);
    output.normal = normalize(output.normal);

    output.color = color * computeLight(output.position.xyz, output.normal, -LookDirection.xyz, 1000);

    return output;
}
";

            const string pixelShaderText =
    @"
struct PSIn
{
    float4 position : SV_POSITION;
    float4 normal : NORMAL;
    float4 color : COLOR;
};
float4 main(PSIn input) : SV_TARGET {
    return input.color;
}
";
            #endregion

        }
        */
        public static class SphereByPoint {

            static readonly D3DShaderTechniquePass pass;
            static readonly VertexLayoutConstructor layconst;

            static SphereByPoint() {
                layconst = new VertexLayoutConstructor()
                   .AddPositionElementAsVector3()
                   .AddColorElementAsVector4();
                pass = new D3DShaderTechniquePass(new IShaderInfo[] {
                    new ShaderInMemoryInfo("SPH_VertexShader", vertexShaderText, null, ShaderStages.Vertex.ToString(), "main"),
                    new ShaderInMemoryInfo("SPH_GeometryShader", geometryShaderText, null, ShaderStages.Geometry.ToString(), "main"),
                    new ShaderInMemoryInfo("SPH_FragmentShader", pixelShaderNoLogicText, null, ShaderStages.Fragment.ToString(), "main"),
                });
            }

            public static D3DShaderTechniquePass GetPasses() => pass;
            public static VertexLayoutConstructor GetLayoutConstructor() => layconst;


            [StructLayout(LayoutKind.Sequential)]
            public struct SpherePoint {
                public readonly Vector3 Position;
                public readonly Vector4 Color;

                public SpherePoint(Vector3 position, Vector4 color) {
                    Position = position;
                    Color = color;
                }
            }

            const string vertexShaderText =
@"
#include ""Game""

struct InputFS {
	float4 position : SV_Position;
	float4 color : COLOR;
};
InputFS main(float4 position : POSITION, float4 color : COLOR){
    InputFS output;

    output.position = mul(position, World);
    //output.position = mul(View, output.position);
    //output.position = mul(Projection, output.position);
    output.color = color;

    return output;
}
";
            const string hullShaderText = @"";

            const string geometryShaderText =
@"
#include ""Game""
#include ""Light""
#include ""Math""

float radius = 1.5f;

struct InputFS {
    float4 position : SV_Position;
    float4 color : COLOR;
};
//float PI = 3.14159265359f;


InputFS createVertex(in float3 sphCenter, in float3 p, in float4 color) {
    InputFS fs = (InputFS)0;
    fs.position = toScreen(p);
    float3 normal = p - sphCenter;
    fs.color = color * computeLight(p, normal, -LookDirection.xyz, 1000);
    return fs;
}

[maxvertexcount(75)]//75
void main(point InputFS points[1], inout TriangleStream<InputFS> output) {
    float PI = 3.14159265359f;
    float radius = 2.5;

    float3 look = -normalize(LookDirection.xyz);
    float4 color = points[0].color;
    float i = 10 * (PI / 180);
    InputFS fs = (InputFS)0;
    float3 sphCenter = points[0].position.xyz;
    float3 center = sphCenter + look * radius;
    float3 tangent = cross(look, float3(1, 0, 0));

    float3 N = look;
    float3x3 TBN = fromAxisAngle3x3(i, N);

    output.Append(createVertex(sphCenter, center, color));
    output.Append(createVertex(sphCenter, center + tangent * radius, color));
    tangent = normalize(mul(tangent, TBN));
    output.Append(createVertex(sphCenter, center + tangent * radius, color));

    for (float angle = 10; angle < 360; angle += 10) {
        output.Append(createVertex(sphCenter, center, color));

        tangent = normalize(mul(tangent, TBN));

        output.Append(createVertex(sphCenter, center + tangent * radius, color));
    }
}
";
        }

    }
}
