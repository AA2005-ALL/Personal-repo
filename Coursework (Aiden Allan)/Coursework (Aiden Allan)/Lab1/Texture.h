#pragma once
#include <string>
#include <GL\glew.h>
#include <vector>
#include <iostream>

class Texture
{
public:
	Texture();

	void Bind(unsigned int unit); // bind upto 32 textures
	void init(const std::string& fileName);
	unsigned int loadCubemap(std::vector<std::string> faces);

	~Texture();

protected:
private:

	GLuint textureHandler;
};

