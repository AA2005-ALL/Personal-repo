#version 450

// One light. std430 layout:
//   vec3 position   offset 0   (vec3 aligns to 16, but nothing before it)
//   float radius    offset 12  (fills the gap after the vec3)
//   vec3 colour     offset 16
//   float intensity offset 28
//   size 32 bytes, stride 32 in the array
// Keep the float straight after each vec3. Move the floats to the end and
// std430 pads each vec3 to 16 bytes, the struct becomes 48 bytes, and your
// C++ struct no longer matches. Read the buffer back to prove it (task 4).
struct Light
{
	vec3 position;
	float radius;
	vec3 colour;
	float intensity;
};

// Bound with glBindBufferBase(GL_SHADER_STORAGE_BUFFER, 1, lightSSBO).
// Unsized array: the buffer can hold as many lights as you allocated.
layout (std430, binding = 1) readonly buffer LightBlock
{
	Light lights[];
};

// Same block as the vertex shader. The declaration must match word for word.
layout (std140, binding = 0) uniform CameraBlock
{
	mat4 view;
	mat4 projection;
	vec3 cameraPos;
	float pad;
} cam;

// How many of the allocated lights are live this frame. Set from C++ each
// frame. Lets you scale the count up without reallocating the buffer.
uniform int lightCount;

uniform sampler2D diffuse;

in vec3 worldPos;
in vec3 worldNormal;
in vec2 texCoord;

out vec4 FragColour;

const float AMBIENT = 0.05;
const float SHININESS = 32.0;
const float SPECULAR_STRENGTH = 0.25;

void main()
{
	vec3 N = normalize(worldNormal);
	vec3 V = normalize(cam.cameraPos - worldPos);
	vec3 albedo = texture(diffuse, texCoord).rgb;

	vec3 result = AMBIENT * albedo;

	// Every fragment loops over every light. That is the cost you measure
	// in task 5: pixels x lights, every frame.
	for (int i = 0; i < lightCount; i++)
	{
		vec3 toLight = lights[i].position - worldPos;
		float dist = length(toLight);
		if (dist > lights[i].radius)
			continue;

		vec3 L = toLight / dist;

		// Linear falloff squared: 1 at the light, 0 at the radius.
		float att = 1.0 - dist / lights[i].radius;
		att = att * att;

		float ndl = max(dot(N, L), 0.0);

		// Blinn-Phong specular.
		vec3 H = normalize(L + V);
		float spec = pow(max(dot(N, H), 0.0), SHININESS);

		vec3 lightRGB = lights[i].colour * lights[i].intensity * att;
		result += lightRGB * (albedo * ndl + SPECULAR_STRENGTH * spec);
	}

	FragColour = vec4(result, 1.0);
}
