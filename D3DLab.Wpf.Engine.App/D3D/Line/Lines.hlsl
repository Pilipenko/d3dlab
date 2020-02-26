@vertex@

#include "Game"

struct InputFS {
	float4 position : SV_Position;
	float4 color : COLOR;
};

InputFS main(float4 position : POSITION, float4 color : COLOR) {
	InputFS output;

	output.position = toWVP(position);
	output.color = color;

	return output;
}

@fragment@

float4 main(float4 position : SV_POSITION, float4 color : COLOR) : SV_TARGET{
	return color;
}

@geometry@