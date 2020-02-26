@vertex@


Texture2D fineLevelTexture : register(t0);



cbuffer TerrainBuff : register(b3){
    float ZScaleFactor;
    float3 LightDirection;

    float4 ScaleFactor;

    float2 AlphaOffset;
    float2 ViewerPos;

    float4 FineTextureBlockOrigin;

    float4x4 WorldViewProjMatrix;
    
    float OneOverWidth;   
}

SamplerState ElevationSampler // fine level height sampler
{
//  Texture   = (fineLevelTexture);
    Filter = MIN_MAG_MIP_LINEAR; 
    MipFilter = None;
    MinFilter = Point;
    MagFilter = Point;
    AddressU = Wrap;
    AddressV = Wrap;
};




struct OUTPUT
{
    vector pos : SV_POSITION;
    float2 uv : TEXCOORD0; // coordinates for normal-map lookup
    float2 zalpha : TEXCOORD1; // coordinates for elevation-map lookup
};

OUTPUT main(float2 gridPos : TEXCOORD0)
{
    OUTPUT output;
    // convert from grid xy to world xy coordinates
    //  ScaleFactor.xy: grid spacing of current level
    //  ScaleFactor.zw: origin of current block within world
    float2 worldPos = gridPos * ScaleFactor.xy + ScaleFactor.zw;
                     
    // compute coordinates for vertex texture
    //  FineBlockOrig.xy: 1/(w, h) of texture
    //  FineBlockOrig.zw: origin of block in texture           
    float2 uv = float2(gridPos * FineTextureBlockOrigin.zw + FineTextureBlockOrigin.xy);
    
    // sample the vertex texture
    float zf_zd = fineLevelTexture.SampleLevel(ElevationSampler, uv, 1);

    // unpack to obtain zf and zd = (zc - zf)
    //  zf is elevation value in current (fine) level
    //  zc is elevation value in coarser level
    float zf = floor(zf_zd);
    float zd = frac(zf_zd) * 512 - 256; // zd = zc - z

    // compute alpha (transition parameter), and blend elevation.
    float2 alpha = clamp((abs(worldPos - ViewerPos) - AlphaOffset) * OneOverWidth, 0, 1);
    alpha.x = max(alpha.x, alpha.y);
    
    float z = zf + alpha.x * zd;
    z = z * ZScaleFactor;
    
    output.pos = mul(float4(worldPos.x, worldPos.y, z, 1), WorldViewProjMatrix);
    
    output.uv = uv;
    output.zalpha = float2(0.5 + z / 1600, alpha.x);
    
    
    return output;
}

@fragment@

cbuffer TerrainBuff : register(b3)
{
    float ZScaleFactor;
    float3 LightDirection;

    float4 ScaleFactor;

    float2 AlphaOffset;
    float2 ViewerPos;

    float4 FineTextureBlockOrigin;

    float4x4 WorldViewProjMatrix;
    
    float OneOverWidth;
}

Texture2D normalsTexture : register(t0);
Texture2D rampTexture : register(t1);

SamplerState ZBasedColorSampler
{
//  Texture = (rampTexture);
    Filter = MIN_MAG_MIP_LINEAR;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};
SamplerState NormalMapSampler
{
//  Texture = (normalsTexture);
    Filter = MIN_MAG_MIP_LINEAR;
    MipFilter = None;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};


float4 main(float2 uv : TEXCOORD0, float2 zalpha : TEXCOORD1) : SV_Target
{
    // do a texture lookup to get the normal in current level
    float4 normalfc = normalsTexture.Sample(NormalMapSampler, uv);
    // normal_fc.xy contains normal at current (fine) level
    // normal_fc.zw contains normal at coarser level
    // blend normals using alpha computed in vertex shader  
    float3 normal = float3((1 - zalpha.y) * normalfc.xy + zalpha.y * (normalfc.zw), 1.0);
    
    // unpack coordinates from [0, 1] to [-1, +1] range, and renormalize.
    normal = normalize(normal * 2 - 1);

    float s = clamp(dot(normal, LightDirection), 0, 1);
    //return s * tex1D(ZBasedColorSampler, zalpha.x);
    return s * rampTexture.Sample(ZBasedColorSampler, float2(zalpha.x, 0));
}
