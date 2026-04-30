#pragma once
#include <string>
#include <GL\glew.h>
#include <glm/glm.hpp>
#include <iostream>

struct FontData;

class TextHandler
{
  public:
	  TextHandler();
	  ~TextHandler();
	  void BindFont();
	  void DrawText(const std::string& text, glm::vec2 position, glm::vec4 color, float size, int screenWidth, int screenHeight);

	  GLuint fontAtlasTextureID = 0;
	  GLuint textVAO = 0;
	  GLuint textVBO = 0;

private:
	FontData* fontData = nullptr; 
};