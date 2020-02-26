@vertex@

#include "Game"
#include "Light"

struct VSOut
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 tex : TEXCOORD0;
    float3 normal : NORMAL;
    float4 positionOrig : TEXCOORD1;
    float3 tangent : TANGENT;
    float3 binormal : BINORMAL;
    float2 normalMap : TEXCOORD2;
    float4 depthPosition : TEXCOORD3;
    float4 texWeights : TEXCOORD4;
};

VSOut main(float4 position : POSITION, float3 normal : NORMAL, float2 tex : TEXCOORD0, float2 normalMapTexCoor : TEXCOORD1,
           float3 tangent : TANGENT, float3 binormal : BINORMAL, float4 color : COLOR, float4 texWeights : TEXCOORD2)
{
    VSOut output = (VSOut) 0;

    output.positionOrig = position;
    output.position = toWVP(position);

    output.tex = tex;
    output.normalMap = normalMapTexCoor;

    normal = mul(World, normal);
    normal = normalize(normal);

    output.color = color * computeLight(output.position.xyz, normal, -LookDirection.xyz, 1000);
    output.normal = normal;
	
    output.tangent = mul(World, tangent);
    output.tangent = normalize(output.tangent);
	
    output.binormal = mul(World, binormal);
    output.binormal = normalize(output.binormal);

    output.depthPosition = output.position;
    output.texWeights = texWeights;

	return
    output;
}

@fragment@

#include "Math"
#include "Game"
#include "Light"

struct PSIn
{
    float4 position : SV_POSITION;
    float4 color : COLOR;
    float2 tex : TEXCOORD0;
    float3 normal : NORMAL;
    float4 positionOrig : TEXCOORD1;
    float3 tangent : TANGENT;
    float3 binormal : BINORMAL;
    float2 normalMap : TEXCOORD2;
    float4 depthPosition : TEXCOORD3;
    float4 texWeights : TEXCOORD4;
};

Texture2D seafloorTexture : register(t0);
Texture2D shoreTexture : register(t1);
Texture2D sandTexture : register(t2);
Texture2D grassTexture : register(t3);
Texture2D dirtTexture : register(t4);
Texture2D rockTexture : register(t5);
Texture2D slopeTexture : register(t6);

Texture2D rockNormalMapTexture : register(t7);
Texture2D snowNormalMapTexture : register(t8);
Texture2D distanceNormalMapTexture : register(t9);




SamplerState SampleType;


//static const float seafloor = 0;//-0.25
//static const float shore = 0.0625;
//static const float sand = 0.125;
//static const float grass = 0.375;
//static const float rock = 0.75;

static const float seafloor = 0.05;
static const float shore = 0.2;
static const float sand = 0.2;
static const float grass = 0.3;
static const float rock = 1;


float hash(float n)
{
    return frac(sin(n) * 43758.5453123);
}

/*
 * Texture Rerepetition
 * http://iquilezles.org/www/articles/texturerepetition/texturerepetition.htm
 */
float sum(float3 v)
{
    return v.x + v.y + v.z;
}
float4 textuRerepetitionWithNoise(Texture2D texture2d, PSIn input)
{
    float2 x = input.tex; // float2();
    float4 origColor = texture2d.Sample(SampleType, x);
	// sample variation pattern
    float2 c = input.normalMap;
    float k = c.x; // cheap (cache friendly) lookup    

	// compute index    
    float index = k * 8.0;
    float i = floor(index);
    float f = frac(index);

	// offsets for the different virtual patterns    
    float2 offa = sin(float2(3.0, 7.0) * (i + 0.0)); // can replace with any other hash    
    float2 offb = sin(float2(3.0, 7.0) * (i + 1.0)); // can replace with any other hash    

	// compute derivatives for mip-mapping    
    float2 dx = ddx(x), dy = ddy(x);

	// sample the two closest virtual patterns    
    float3 cola = texture2d.SampleGrad(SampleType, x + offa, dx, dy).xyz;
    float3 colb = texture2d.SampleGrad(SampleType, x + offb, dx, dy).xyz;

	// interpolate between the two virtual patterns
    float3 color = lerp(cola, colb, smoothstep(0.2, 0.8, f - 0.1 * sum(cola - colb)));
    return float4(color, origColor.a);
}
/*
 * End: Texture Rerepetition
 */

 /*
  * Bump mapping 
  */
float4 normalMapping(float4 colorFirst, float4 normalMapColor, PSIn input)
{
    float3 bumpNormal;
    float lightIntensity;

    normalMapColor = (normalMapColor * 2.0f) - 1.0f;
    float3x3 texSpace = float3x3(input.tangent, input.binormal, input.normal);
	//bumpNormal = (bumpMap.x * input.tangent) + (bumpMap.y * input.binormal) + (bumpMap.z * input.normal);
    bumpNormal = normalize(mul(normalMapColor, texSpace)); // normalize(bumpNormal);

    float intensity = 0.0;
    for (int i = 0; i < 3; ++i)
    {
        Light l = lights[i];
        if (l.Type == 3)
        {
            intensity += dot(l.LightDirF3, bumpNormal);
        }
    }
	//lightIntensity = saturate(dot(bumpNormal, -LookDirection.xyz));//float3(0, 1.2, 0.5)
    float4 color = saturate(colorFirst * intensity);
	//float intensity = computeLight(input.position.xyz, bumpNormal, -LookDirection.xyz, 1000);
    return float4(color.xyz, colorFirst.a);
} /*
float4 normalMappingByDepth(float4 colorFirst, PSIn input) {//Texture2D normalMapBakinTexture
	float4 bumpMap;
	float3 bumpNormal;
	float lightIntensity;

	float depthValue = input.depthPosition.z / input.depthPosition.w;
	// Select the normal map for the first material based on the distance.
	if (depthValue > 0.998f) {
		bumpMap = distanceNormalMapTexture.Sample(SampleType, input.normalMap);
	}
	else {
		bumpMap = rockNormalMapTexture.Sample(SampleType, input.tex);
	}

	return normalMapping(colorFirst, bumpMap, input);
}

 * End: Bump mapping 
 */



float3 blend(float depth, float4 texture1, float a1, float4 texture2, float a2)
{
    float ma = max(texture1.a + a1, texture2.a + a2) - depth;

    float b1 = max(texture1.a + a1 - ma, 0);
    float b2 = max(texture2.a + a2 - ma, 0);

    return (texture1.rgb * b1 + texture2.rgb * b2) / (b1 + b2);
}
float4 interpolateColorsEdge(float elevation, float4 colorToMin, float4 colorToMax, float rangeMin, float rangeMax)
{
    float blendAmount = toNewRange(elevation, rangeMin, rangeMax, 0.0, 1.0);

    return float4(blend(0.5, colorToMin, 1 - blendAmount, colorToMax, blendAmount), 1);
	//the same result as blend
    return lerp(colorToMin, colorToMax, blendAmount);
}
float4 blendColorsForSlope(float4 color1, float4 color2, float slope)
{
    float4 blendAmount;
    float4 color;
    if (slope < 0.2)
    {
        blendAmount = slope / 0.2f;
        color = lerp(color1, color2, blendAmount);
    }
    if ((slope < 0.7) && (slope >= 0.2f))
    {
        blendAmount = (slope - 0.2f) * (1.0f / (0.7f - 0.2f));
        color = lerp(color2, color1, blendAmount);
    }
    if (slope >= 0.7)
    {
        color = color1;
    }
    return color;
}


float4 biome(float e, PSIn input, float slope, float4 w)
{
    float4 color;
    float2 tex = input.tex;

    color = shoreTexture.Sample(SampleType, tex) * w.x;
    //color += grassTexture.Sample(SampleType, tex) * w.y;
    color += textuRerepetitionWithNoise(grassTexture, input) * w.y;
    //color += rockTexture.Sample(SampleType, tex) * w.z;
    color += textuRerepetitionWithNoise(rockTexture, input) * w.z;
    color += float4(1, 1, 1, 1) * w.w;

    return color;
}

float4 biome1(float e, PSIn input, float slope)
{
    float4 color;
    float2 tex = input.tex;
    float4 slopeColor = slopeTexture.Sample(SampleType, tex);

	/*
	if (e < seafloor) {
		color = shoreTexture.Sample(SampleType, tex);
		color = normalMapping(color, distanceNormalMapTexture.Sample(SampleType, input.normalMap), input);
		//color = interpolateColorsEdge(e, color, shoreTexture.Sample(SampleType, tex), 0, seafloor);
		//color = blendColorsForSlope(color, slopeColor, slope);
	} else*/
    if (e < shore)
    {
        color = shoreTexture.Sample(SampleType, tex);
		//color = normalMappingByDepth(color, input);
        color = normalMapping(color, distanceNormalMapTexture.Sample(SampleType, input.normalMap), input);
        color = interpolateColorsEdge(e, color, grassTexture.Sample(SampleType, tex), seafloor, shore);
		//color = blendColorsForSlope(color, slopeColor, slope);
    }
	/*else if (e < sand) {
		color = blendColorsForSlope(sandTexture.Sample(SampleType, tex), slopeTexture.Sample(SampleType, tex), slope);
	}*/
    else if (e < grass)
    {
        color = textuRerepetitionWithNoise(grassTexture, input);
        color = interpolateColorsEdge(e, color, rockTexture.Sample(SampleType, tex), shore, grass);
        color = blendColorsForSlope(color, slopeColor, slope);
    }
    else if (e < rock)
    {
        color = interpolateColorsEdge(e, rockTexture.Sample(SampleType, tex), float4(1.0f, 1.0f, 1.0f, 1.0f), grass, rock);
        color = blendColorsForSlope(color, slopeColor, slope);
    }
    else
    { // snow
        color = blendColorsForSlope(rockTexture.Sample(SampleType, tex), slopeColor, slope);
        float4 blendAmount;
        if (slope < 0.2)
        {
            color = lerp(float4(1, 1, 1, 1), color, 0.2);
        }
    }




    return color;
}

float4 main(PSIn input) : SV_TARGET
{
    float4 color = input.color;
    float2 tex = input.tex;
    float3 normal = input.normal;
    float3 orig = input.positionOrig;

    float slope = 1.0f - normal.y;

    return saturate(color * biome(orig.y / 50, input, slope, input.texWeights));
}

//float4 calculateBumpColor(float4 color, PSIn input) {
//	float4 bumpMap = (color * 2.0f) - 1.0f;
//
//	float3 bumpNormal = (bumpMap.x * input.tangent) + (bumpMap.y * input.binormal) + (bumpMap.z * input.normal);
//	bumpNormal = normalize(bumpNormal);
//
//	float lightIntensity = computeLight(input.position.xyz, bumpNormal, -LookDirection.xyz, 1000);// saturate(dot(bumpNormal, lightDir));
//
//	return saturate(color * lightIntensity);
//}


float4 main1(PSIn input) : SV_TARGET
{
    float4 color = input.color;
    float2 tex = input.tex;
    float3 normal = input.normal;



	//return color * grassTexture.Sample(SampleType, tex);

    float blendAmount;
    float4 textureColor;

    float4 grassColor = grassTexture.Sample(SampleType, tex);
    float4 slopeColor = slopeTexture.Sample(SampleType, tex);
    float4 rockColor = rockTexture.Sample(SampleType, tex);

	// Calculate the slope of this point.
    float slope = 1.0f - normal.y;

	// Determine which texture to use based on height.
    if (slope < 0.2)
    {
        blendAmount = slope / 0.2f;
        textureColor = lerp(grassColor, slopeColor, blendAmount);
    }

    if ((slope < 0.7) && (slope >= 0.2f))
    {
        blendAmount = (slope - 0.2f) * (1.0f / (0.7f - 0.2f));
        textureColor = lerp(slopeColor, rockColor, blendAmount);
    }

    if (slope >= 0.7)
    {
        textureColor = rockColor;
    }


    return color * textureColor;
}

@geometry@