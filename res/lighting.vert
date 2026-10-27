#version 450

// Attribute locations match Shader::init (glBindAttribLocation 0, 1, 2)
// and the interleaved Vertex layout you build in Mesh::initModel.
layout (location = 0) in vec3 VertexPosition;
layout (location = 1) in vec2 VertexTexCoord;
layout (location = 2) in vec3 VertexNormal;

// Per-frame data. Filled once per frame from C++ with glNamedBufferSubData.
// std140: mat4 = 64 bytes, vec3 is padded to 16, so the block is 144 bytes.
// The C++ struct must match this exactly (see Week07_tasks.md, task 2).
layout (std140, binding = 0) uniform CameraBlock
{
	mat4 view;         // offset 0
	mat4 projection;   // offset 64
	vec3 cameraPos;    // offset 128
	float pad;         // offset 140, keeps the block at 144 bytes
} cam;

// Per-object data stays a plain uniform. One mesh, one model matrix.
uniform mat4 model;

out vec3 worldPos;
out vec3 worldNormal;
out vec2 texCoord;

void main()
{
	vec4 wp = model * vec4(VertexPosition, 1.0);
	worldPos = wp.xyz;

	// Normal matrix removes the effect of non-uniform scale on the normal.
	worldNormal = mat3(transpose(inverse(model))) * VertexNormal;

	texCoord = VertexTexCoord;

	gl_Position = cam.projection * cam.view * wp;
}
