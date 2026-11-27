#version 420 core
out vec4 FragColor;

in vec3 Normal;
in vec2 texCoords; 
in vec3 Position;

uniform vec3 cameraPos;
uniform sampler2D sampleTexture;
uniform samplerCube skybox;

void main()
{             
    vec3 I = normalize(Position - cameraPos);
    vec3 R = reflect(I, normalize(Normal));
    vec3 skyboxReflection = texture(skybox, R).rgb; 
    vec3 textureColour = texture(sampleTexture, texCoords).rgb;
    vec3 combinedTexture = mix(textureColour, skyboxReflection, 0.3);
    FragColor = vec4(combinedTexture, 1.0);
}