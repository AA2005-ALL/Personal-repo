#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in vec2 aTexCoords;

uniform mat4 model;

uniform mat4 transform;

out vec3 FragPos;  // Position of the fragment in world space
out vec3 Normal;   // Normal to be used in the fragment shader
out vec2 TexCoords; //Texture Coordinates of the fragment shader

void main()
{
    // Transform the vertex position to world space
    FragPos = vec3(model * vec4(aPos, 1.0));
    
    // Pass the normal to the fragment shader after transforming it
    Normal = mat3(transpose(inverse(model))) * aNormal;
    
    // Project the vertex position to clip space
    gl_Position = transform * vec4(aPos, 1.0);

    TexCoords = aTexCoords;
}
