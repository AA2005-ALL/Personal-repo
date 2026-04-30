#version 330 core

struct Material {
    sampler2D diffuse;
    sampler2D specular;
    float shininess;
}; 

struct Light {
    vec3 position;
  
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

in vec3 FragPos;  // Interpolated fragment position from the vertex shader
in vec3 Normal;   // Interpolated normal
in vec2 TexCoords;

uniform vec3 viewPos;
uniform sampler2D Texture;
uniform vec3 lightPos;  // Position of the light source
uniform Material material;
uniform Light light;  
uniform float blendAmount; 

out vec4 FragColor; // Final output color

void main()
{
    vec3 textureColour = texture(Texture, TexCoords).rgb;

    // Diffuse lighting
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(lightPos - FragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = light.diffuse * diff * textureColour;

    // Ambient lighting
    vec3 ambient = light.ambient * textureColour;

    //Specular lighting 
    vec3 viewDir = normalize(viewPos - FragPos);
    vec3 reflectDir = reflect(-lightDir, norm);  
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    vec3 specular = light.specular * spec * textureColour;

    vec3 combinedLighting = ambient + diffuse + specular; 

    vec3 blendedColour = mix(textureColour, textureColour * combinedLighting, blendAmount);

    // Combine results
    FragColor = vec4(blendedColour, 1.0);  
}
