#version 450

// Extension: a flat grid, lifted by the height map compute wrote. The CPU never sees a height.
layout (location = 0) in vec3 VertexPosition;
layout (location = 1) in vec2 VertexTexCoord;
layout (location = 2) in vec3 VertexNormal;   // unused: the normal is worked out below

layout (std140, binding = 0) uniform CameraBlock
{
	mat4 view;
	mat4 projection;
	vec3 cameraPos;
	float pad;
} cam;

layout (binding = 1) uniform sampler2D heightMap;   // texture unit 1, HEIGHT_TEXTURE_UNIT in MainGame.cpp

const float MIN_Y = -15.0;           // the map holds 0 to 1, this is where that lands in the world
const float MAX_Y = -3.0;            // under the box floor at y = 0
const float TERRAIN_SIZE = 400.0;    // same as TERRAIN_SIZE in MainGame.cpp

out vec3 worldPos;
out vec3 worldNormal;
out vec2 texCoord;

// the height at a texel in world units, clamped so the edge looks at itself, not past the map
float heightAt(ivec2 texel, ivec2 size)
{
	ivec2 safe = clamp(texel, ivec2(0), size - ivec2(1));
	return mix(MIN_Y, MAX_Y, texelFetch(heightMap, safe, 0).r);
}

void main()
{
	ivec2 size = textureSize(heightMap, 0);
	ivec2 texel = ivec2(VertexTexCoord * vec2(size - ivec2(1)) + 0.5);

	// central difference: how much higher is the texel on one side than the other
	float left  = heightAt(texel + ivec2(-1, 0), size);
	float right = heightAt(texel + ivec2( 1, 0), size);
	float down  = heightAt(texel + ivec2(0, -1), size);
	float up    = heightAt(texel + ivec2(0,  1), size);
	float texelWorld = TERRAIN_SIZE / float(size.x - 1);
	worldNormal = normalize(vec3(left - right, 2.0 * texelWorld, down - up));

	vec3 wp = vec3(VertexPosition.x, heightAt(texel, size), VertexPosition.z);
	worldPos = wp;
	texCoord = VertexTexCoord;
	gl_Position = cam.projection * cam.view * vec4(wp, 1.0);
}
