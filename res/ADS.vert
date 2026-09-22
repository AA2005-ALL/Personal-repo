#version 330 core

layout (location = 0) in vec3 VertexPosition; // The input vertex position
layout (location = 2) in vec3 aNormal; // The input vertex normal (for lighting)

uniform mat4 model;

uniform mat4 transform;

out vec3 FragPos;  // Position of the fragment in world space
out vec3 Normal;   // Normal to be used in the fragment shader

void main()
{
    // Transform the vertex position to world space
    FragPos = vec3(model * vec4(VertexPosition, 1.0));
    
    // Pass the normal to the fragment shader after transforming it
    Normal = mat3(transpose(inverse(model))) * aNormal;
    
    // Project the vertex position to clip space
    gl_Position = transform * vec4(VertexPosition, 1.0);
}
