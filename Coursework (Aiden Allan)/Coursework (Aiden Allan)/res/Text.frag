#version 330 core
in vec2 TexCoords;
out vec4 FragColor;

uniform sampler2D fontAtlas;
uniform vec4 textColor;

void main()
{
    // Font atlas is single channel (GL_R8), sample red channel as alpha
    float alpha = texture(fontAtlas, TexCoords).r;
    FragColor = vec4(textColor.rgb, textColor.a * alpha);
}