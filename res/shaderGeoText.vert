//Version Number
#version 400

//The layout qualifers
layout (location = 0) in vec3 VertexPosition;
layout (location = 1) in vec2 tc;
layout (location = 2) in vec3 VertexNormal;

//Uniform variable
uniform mat4 transform;

//Passing out texture data 
out VS_OUT
{
   vec2 texCoords;
} vs_out;

void main()
{
	vs_out.texCoords = tc;

	// Sets the position of the current vertex
	gl_Position = transform * vec4(VertexPosition, 1.0);
}