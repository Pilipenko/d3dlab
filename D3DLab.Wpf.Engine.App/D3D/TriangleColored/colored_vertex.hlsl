@vertex@

#include "Game"
#include "Light"

struct VSOut
{
	float4 position : SV_POSITION;
	float4 normal : NORMAL;
	float4 color : COLOR;
};

VSOut main(float4 position : POSITION, float3 normal : NORMAL, float4 color : COLOR) {
	VSOut output = (VSOut)0;

	output.position = toWVP(position);

	output.normal = mul(normal, World);
	output.normal = normalize(output.normal);

	output.color = color * computeLight(output.position.xyz, output.normal, -LookDirection.xyz, 1000);

	return output;
}

@fragment@

struct PSIn
{
	float4 position : SV_POSITION;
	float4 normal : NORMAL;
	float4 color : COLOR;
};
float4 main(PSIn input) : SV_TARGET{
	return input.color;
}