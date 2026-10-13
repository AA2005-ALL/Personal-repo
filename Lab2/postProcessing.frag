#version 400

//layout( location = 0 ) out vec4 fragcolor;

FragColor = vec4(vec3(1.0 - texture(screenTexture, TexCoords)), 1.0)

FragColor = texture(screenTexture, TexCoords);
float average = 0.2126 * FragColor.r + 0.7152 * FragColor.g + 0.0722 * FragColor.b;                                                          
FragColor = vec4(average, average, average, 1.0);
