#version 450

// Shared by object.vert and instanced.vert. One directional light and a
// colour taken from the world position, so every copy is tinted differently
// without any per-object data reaching the fragment shader.
in vec3 worldPos;
in vec3 worldNormal;
in vec2 texCoord;

out vec4 FragColour;

const vec3 LIGHT_DIR = normalize(vec3(0.4, 1.0, 0.3));
const float AMBIENT = 0.15;

void main()
{
	vec3 N = normalize(worldNormal);
	float ndl = max(dot(N, LIGHT_DIR), 0.0);
	vec3 tint = 0.55 + 0.45 * cos(vec3(worldPos.x, worldPos.z, worldPos.x + worldPos.z) * 0.15 + vec3(0.0, 2.1, 4.2));
	FragColour = vec4(tint * (AMBIENT + (1.0 - AMBIENT) * ndl), 1.0);
}
