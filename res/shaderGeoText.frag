//Version number
#version 400

//Layout Qualifier
layout( location = 0 ) out vec4 fragcolor;

in vec2 TexCoords; 

uniform sampler2D sampleTexture; 
 
void main()
{
    //Applies the texture to each fragment
    fragcolor = texture(sampleTexture, TexCoords);
}