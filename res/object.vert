#version 450

// Task 1: one draw per object. The model matrix arrives as a uniform,
// so the CPU sets it before every draw call.
layout (location = 0) in vec3 VertexPosition;
layout (location = 1) in vec2 VertexTexCoord;
layout (location = 2) in vec3 VertexNormal;

// Week 7's camera block, filled once per frame.
layout (std140, binding = 0) uniform CameraBlock
{
	mat4 view;
	mat4 projection;
	vec3 cameraPos;
	float pad;
} cam;

uniform mat4 model;

out vec3 worldPos;
out vec3 worldNormal;
out vec2 texCoord;

void main()
{
	vec4 wp = model * vec4(VertexPosition, 1.0);
	worldPos = wp.xyz;
	worldNormal = mat3(model) * VertexNormal;   // fine while the scale is uniform
	texCoord = VertexTexCoord;
	gl_Position = cam.projection * cam.view * wp;
}
