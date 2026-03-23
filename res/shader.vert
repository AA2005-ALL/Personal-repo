#version 400 core

layout (location = 0) in vec3 position;  
layout (location = 1) in vec2 texCoord;

out vec2 texCoord0;  // 'varying' is replaced with 'out'

uniform mat4 transform;

void main()
{
    gl_Position = transform * vec4(position * 5.0f, 1.0);
    texCoord0 = texCoord;  // Pass the texture coordinates to the fragment shader
}
