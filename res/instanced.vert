#version 450

// Task 2 onward: one draw for every copy. The model matrices sit in a shader
// storage buffer, one per instance, and gl_InstanceID says which one is mine.
layout (location = 0) in vec3 VertexPosition;
layout (location = 1) in vec2 VertexTexCoord;
layout (location = 2) in vec3 VertexNormal;

layout (std140, binding = 0) uniform CameraBlock
{
	mat4 view;
	mat4 projection;
	vec3 cameraPos;
	float pad;
} cam;

// std430: a mat4 is 64 bytes with no padding, so the C++ side is a plain
// array of glm::mat4. Bound with glBindBufferBase or glBindBufferRange at 2.
// gl_InstanceID counts from 0 for every draw, so when one buffer holds the
// matrices of several draws, bind the range that belongs to this draw.
layout (std430, binding = 2) readonly buffer Models
{
	mat4 model[];
};

out vec3 worldPos;
out vec3 worldNormal;
out vec2 texCoord;

void main()
{
	mat4 m = model[gl_InstanceID];
	vec4 wp = m * vec4(VertexPosition, 1.0);
	worldPos = wp.xyz;
	worldNormal = mat3(m) * VertexNormal;
	texCoord = VertexTexCoord;
	gl_Position = cam.projection * cam.view * wp;
}
