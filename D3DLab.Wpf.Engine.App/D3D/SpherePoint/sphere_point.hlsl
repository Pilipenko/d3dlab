@vertex@

#include "Game"

struct InputFS {
	float4 position : SV_Position;
	float4 color : COLOR;
};
InputFS main(float4 position : POSITION, float4 color : COLOR) {
	InputFS output;

	output.position = mul(position, World);
	//output.position = mul(View, output.position);
	//output.position = mul(Projection, output.position);
	output.color = color;

	return output;
}

@fragment@

float4 main(float4 position : SV_POSITION, float4 color : COLOR) : SV_TARGET{
	return color;
}

@geometry@

#include "Game"
#include "Light"
#include "Math"

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