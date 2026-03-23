#version 330 core

struct Material {
    sampler2D diffuse;
    vec3 specular;
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

uniform vec3 lightPos;  // Position of the light source
uniform vec3 lightColor; // Color of the light
uniform vec3 objectColor; // Base color of the object
uniform Material material;
uniform Light light;  

out vec4 FragColor; // Final output color

void main()
{
    // Diffuse lighting
    vec3 norm = normalize(Normal);
    vec3 lightDir = normalize(lightPos - FragPos);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = light.diffuse * diff * vec3(texture(material.diffuse, TexCoords));

    // Ambient lighting
    float ambientStrength = 0.1;
    vec3 ambient = light.ambient * vec3(texture(material.diffuse, TexCoords));



    //Specular lighting 
    vec3 reflectDir = reflect(-lightDir, norm);  
    vec3 specular = light.specular * material.specular;  

    // Combine results
    vec3 result = (ambient + diffuse + specular);
    FragColor = vec4(result, 1.0);
}
