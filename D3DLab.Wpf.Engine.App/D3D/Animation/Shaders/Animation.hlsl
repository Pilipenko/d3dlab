@vertex@

#include "Common"
#include "Game"

void SkinVertex(float4 weights, uint4 bones, inout float4 position, inout float3 normal)
{
	// If there are skin weights apply vertex skinning
	if (weights.x != 0)
	{
		// Calculate the skin transform from up to four bones and weights
		float4x4 skinTransform = Bones[bones.x] * weights.x +
			Bones[bones.y] * weights.y +
			Bones[bones.z] * weights.z +
			Bones[bones.w] * weights.w;

		// Apply skinning to vertex and normal
		position = mul(position, skinTransform);

		// We assume here that the skin transform includes only uniform scaling (if any)
		normal = mul(normal, (float3x3)skinTransform);
	}
}

PixelShaderInput main(VertexShaderInput vertex)
{
	PixelShaderInput result = (PixelShaderInput)0;

	// Apply vertex skinning if any
	SkinVertex(vertex.SkinWeights, vertex.SkinIndices, vertex.Position, vertex.Normal);

	result.Position = toWVP(vertex.Position);
	result.Diffuse = vertex.Color * MaterialDiffuse;
	// Apply material UV transformation
	result.TextureUV = mul(float4(vertex.TextureUV.x, vertex.TextureUV.y, 0, 1), (float4x2)UVTransform).xy;

	// We use the inverse transpose of the world so that if there is non uniform
	// scaling the normal is transformed correctly. We also use a 3x3 so that 
	// the normal is not affected by translation (i.e. a vector has the same direction
	// and magnitude regardless of translation)
	result.WorldNormal = mul(vertex.Normal, (float3x3)WorldInverse);

	result.WorldPosition = mul(vertex.Position, World).xyz;

	return result;
}

@fragment@

Texture2D Texture0 : register(t0);
SamplerState Sampler : register(s0);



#include "Common"
#include "Game"
#include "Light"

float4 main(PixelShaderInput pixel) : SV_Target
{

	return float4(0.8, 0.8, 0.8, 1.0) * computeLight(pixel.Position.xyz, pixel.WorldNormal, -LookDirection.xyz, 1000);

	// Normalize our vectors as they are not 
	// guaranteed to be unit vectors after interpolation
	float3 normal = normalize(pixel.WorldNormal);
	float3 toEye = normalize(CameraPosF4.xyz - pixel.WorldPosition);

	float3 ambient = MaterialAmbient.rgb;
	float3 emissive = MaterialEmissive.rgb;

	

	float4 finalcolor;
	float4 lc = float4(0.8, 0.8, 0.8, 1.0);//todo: remake from iutside color
	
	// Texture sample here (use white if no texture)
	float4 sample = (float4)1.0f;
	if (HasTexture) {
		sample = Texture0.Sample(Sampler, pixel.TextureUV);
	}

	for (int i = 0; i < 3; ++i) {
		Light l = lights[i];

		float3 toLight = normalize(l.LightDirF3);

		

		float3 diffuse = Lambert(pixel.Diffuse, normal, toLight);
		float3 specular = SpecularBlinnPhong(normal, toLight, toEye);

		//float3 color = (saturate(ambient + diffuse) * sample.rgb + specular);
		//color = color * computeLight(pixel.Position.xyz, normal, toLight, 1000);

		// Calculate final color component
		float3 color = (saturate(ambient + diffuse) * sample.rgb + specular) * lc + emissive;
		// We saturate ambient+diffuse to ensure there is no over-
		// brightness on the texture sample if the sum is greater than 1

		// Calculate final alpha value
		float alpha = pixel.Diffuse.a * sample.a;

		finalcolor = float4(color, alpha);
	}
	// Return result
	return finalcolor;
}