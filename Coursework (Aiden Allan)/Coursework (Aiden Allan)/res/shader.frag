#version 400 core

in vec2 texCoord0;  
uniform sampler2D diffuse;

out vec4 FragColor; // 'out' is used for fragment output

void main()
{
    FragColor = texture(diffuse, texCoord0);  // Use 'texture' instead of 'texture2D'
}
