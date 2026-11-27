#version 420 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 tc;
layout (location = 2) in vec3 aNormal;

out vec3 Normal;
out vec2 texCoords;
out vec3 Position;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    texCoords = tc;
    Normal = mat3(transpose(inverse(model))) * aNormal;
    Position = vec3(model * vec4(aPos, 1.0));
    gl_Position = projection * view * model * vec4(aPos, 1.0);
}