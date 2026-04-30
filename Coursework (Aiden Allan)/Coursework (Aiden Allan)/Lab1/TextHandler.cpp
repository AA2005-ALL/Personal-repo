#include "TextHandler.h"
#include <fstream>
#define STB_RECT_PACK_IMPLEMENTATION
#include "stb_rect_pack.h"
#define STB_TRUETYPE_IMPLEMENTATION
#include "stb_truetype.h"

static const uint32_t FIRST_CHAR = 32;
static const uint32_t CHAR_COUNT = 95;

struct FontData
{
	stbtt_packedchar  packedChars[CHAR_COUNT];
	stbtt_aligned_quad alignedQuads[CHAR_COUNT];
};

TextHandler::TextHandler()
{
	fontData = new FontData();
}

TextHandler::~TextHandler()
{
	delete fontData;
	fontData = nullptr;
}

void TextHandler::BindFont()
{
	if (fontData == nullptr)
	{
		std::cout << "fontData is null" << std::endl;
		fontData = new FontData(); 
	}

	std::string fontFilePath = "..\\res\\Gwenchana.ttf";

	std::ifstream inputFileStream(fontFilePath, std::ios::binary);

	if (!inputFileStream.is_open())
	{
		std::cout << "Could not open font file: " << fontFilePath << std::endl;
		return;
	}

	std::cout << "Font file opened " << fontFilePath << std::endl;

	// Find the size of the file to allocate memory dynamically
	inputFileStream.seekg(0, std::ios::end);
	auto&& size = inputFileStream.tellg();
	inputFileStream.seekg(0, std::ios::beg);

	// Allocate the buffer
	uint8_t* fontDataBuf = new uint8_t[static_cast<size_t>(size)];

	inputFileStream.read(reinterpret_cast<char*>(fontDataBuf), size);
	inputFileStream.close();

	uint32_t fontAtlasWidth = 1024; 
	uint32_t fontAtlasHeight = 1024; 

	uint8_t* fontAtlasBitmap = new uint8_t[fontAtlasWidth * fontAtlasHeight];


	memset(fontAtlasBitmap, 0, fontAtlasWidth * fontAtlasHeight);

	float fontSize = 64.0f;


	stbtt_pack_context ctx;

	stbtt_PackBegin(
		&ctx,                                     
		(unsigned char*)fontAtlasBitmap,          
		fontAtlasWidth,                           
		fontAtlasHeight,                          
		0,                                        // Stride in bytes
		1,                                        // Padding between the glyphs
		nullptr);

	stbtt_PackFontRange(
		&ctx,                                     
		fontDataBuf,                              
		0,                                                                        
		fontSize,                  
		FIRST_CHAR,                              
		CHAR_COUNT,                               
		fontData->packedChars                     
	);
	stbtt_PackEnd(&ctx);

	float cursorX = 0.0f;
	float cursorY = 0.0f;

	for (int i = 0; i < (int)CHAR_COUNT; i++)
	{
		stbtt_GetPackedQuad(
			fontData->packedChars,              
			fontAtlasWidth,                      
			fontAtlasHeight,                     
			i,                             
			&cursorX, &cursorY,       
			&fontData->alignedQuads[i],          
			0                                    
		);
	}

	glGenTextures(1, &fontAtlasTextureID);
	glBindTexture(GL_TEXTURE_2D, fontAtlasTextureID);

	glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

	// Upload the data to the GPU.
	glTexImage2D(GL_TEXTURE_2D, 0, GL_R8, fontAtlasWidth, fontAtlasHeight, 0, GL_RED, GL_UNSIGNED_BYTE, fontAtlasBitmap);


	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);


	glBindTexture(GL_TEXTURE_2D, 0);

	// Set up VAO/VBO
	glGenVertexArrays(1, &textVAO);
	glGenBuffers(1, &textVBO);
	glBindVertexArray(textVAO);

	glBindBuffer(GL_ARRAY_BUFFER, textVBO);
	glBufferData(GL_ARRAY_BUFFER, sizeof(float) * 6 * 4, nullptr, GL_DYNAMIC_DRAW);
	glEnableVertexAttribArray(0);
	glVertexAttribPointer(0, 4, GL_FLOAT, GL_FALSE, 4 * sizeof(float), 0);
	glBindBuffer(GL_ARRAY_BUFFER, 0);
	glBindVertexArray(0);

	std::cout << "Completed binding: " << std::endl; 

	// Free resources
	delete[] fontAtlasBitmap;
	delete[] fontDataBuf;
	}

void TextHandler::DrawText(const std::string& text, glm::vec2 position, glm::vec4 color, float size, int screenWidth, int screenHeight)
{
	glEnable(GL_BLEND);
	glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);


	glActiveTexture(GL_TEXTURE0);
	glBindTexture(GL_TEXTURE_2D, fontAtlasTextureID);


	glBindVertexArray(textVAO);

	float cursorX = position.x;
	float cursorY = position.y;

	float scale = size / 64;


	for (char c : text)
	{
		//Skip characters outside atlas range
		if (c < FIRST_CHAR || c >= FIRST_CHAR + CHAR_COUNT)
		{
			continue;
		}

		int charIndex = c - FIRST_CHAR;
		stbtt_aligned_quad& q = fontData->alignedQuads[charIndex];

		float tempX = 0.0f, tempY = 0.0f;
		stbtt_GetPackedQuad(fontData->packedChars, 1024, 1024, charIndex, &tempX, &tempY, &q, 0);

		stbtt_packedchar& pc = fontData->packedChars[charIndex];

		//Calculate quad dimensions, applying scale
		float x0 = cursorX + q.x0 * scale;
		float y0 = cursorY + q.y0 * scale;  
		float x1 = cursorX + q.x1 * scale;
		float y1 = cursorY + q.y1 * scale;

		//Two triangles forming the character quad
		float vertices[6][4] = {
			{ x0, y1,  q.s0, q.t0 }, 
			{ x0, y0,  q.s0, q.t1 },  
			{ x1, y0,  q.s1, q.t1 },  

			{ x0, y1,  q.s0, q.t0 },  
			{ x1, y0,  q.s1, q.t1 },  
			{ x1, y1,  q.s1, q.t0 },  
		};

		glBindBuffer(GL_ARRAY_BUFFER, textVBO);
		glBufferSubData(GL_ARRAY_BUFFER, 0, sizeof(vertices), vertices);
		glBindBuffer(GL_ARRAY_BUFFER, 0);

		glDrawArrays(GL_TRIANGLES, 0, 6);

		cursorX += pc.xadvance * scale;
	}

	glBindVertexArray(0);
	glBindTexture(GL_TEXTURE_2D, 0);
	glDisable(GL_BLEND);
}