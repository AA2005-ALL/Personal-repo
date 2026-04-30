#pragma once
#include <SDL\SDL.h>
#include <GL/glew.h>
#include "Display.h" 
#include "Shader.h"
#include "Mesh.h"
#include "TextHandler.h"
#include "Texture.h"
#include "transform.h"
#include <random>
#include <map>

enum class GameState{PLAY, EXIT};

class MainGame
{
public:
	MainGame();
	~MainGame();
	void linkADS(Transform&);
	void linkSkybox();
	void run();

private:

	void initSystems();
	void processInput();
	void gameLoop();
	void drawGame();
	int randDivDir(); 

	struct Collider
	{
		float x;
		float z;
		float width;
		float depth;
	};

	bool CheckCollision(const Collider& col1, const Collider& col2);



	Display _gameDisplay;
	GameState _gameState;
	Mesh arrowMesh;
	Mesh ballMesh;
	Mesh goalMesh;
	Mesh goalKeeperMesh; 
	Camera myCamera;
	Texture ballTexture; 
	Texture arrowTexture;
	Texture goalTexture;
	Texture goalKeeperTexture;
	Texture skyBoxTexture;
	TextHandler fontTexture; 
	Shader ADS;
	Shader skyBox;
	Shader textShader;
	Transform ballTransform;
	Transform arrowTransform; 
	Transform goalTransform;
	Transform goalKeeperTransform;

	bool mousePressedOnce; 
	bool mousePressedTwice;
	bool arrowShaderChosen;
	bool rotateLeft; 
	bool rotateUp;

	float counter;
	float ballMoveAmount;
	float ballMoveAmountY;
	float ballSpinAmount;
	float gravity = 0.0098f;
	float skyboxVertices[108] = {          
		-1.0f,  1.0f, -1.0f,
		-1.0f, -1.0f, -1.0f,
		 1.0f, -1.0f, -1.0f,
		 1.0f, -1.0f, -1.0f,
		 1.0f,  1.0f, -1.0f,
		-1.0f,  1.0f, -1.0f,

		-1.0f, -1.0f,  1.0f,
		-1.0f, -1.0f, -1.0f,
		-1.0f,  1.0f, -1.0f,
		-1.0f,  1.0f, -1.0f,
		-1.0f,  1.0f,  1.0f,
		-1.0f, -1.0f,  1.0f,

		 1.0f, -1.0f, -1.0f,
		 1.0f, -1.0f,  1.0f,
		 1.0f,  1.0f,  1.0f,
		 1.0f,  1.0f,  1.0f,
		 1.0f,  1.0f, -1.0f,
		 1.0f, -1.0f, -1.0f,

		-1.0f, -1.0f,  1.0f,
		-1.0f,  1.0f,  1.0f,
		 1.0f,  1.0f,  1.0f,
		 1.0f,  1.0f,  1.0f,
		 1.0f, -1.0f,  1.0f,
		-1.0f, -1.0f,  1.0f,

		-1.0f,  1.0f, -1.0f,
		 1.0f,  1.0f, -1.0f,
		 1.0f,  1.0f,  1.0f,
		 1.0f,  1.0f,  1.0f,
		-1.0f,  1.0f,  1.0f,
		-1.0f,  1.0f, -1.0f,

		-1.0f, -1.0f, -1.0f,
		-1.0f, -1.0f,  1.0f,
		 1.0f, -1.0f, -1.0f,
		 1.0f, -1.0f, -1.0f,
		-1.0f, -1.0f,  1.0f,
		 1.0f, -1.0f,  1.0f
	};

	unsigned int penaltiesTaken; 
	unsigned int penaltiesScored;

	unsigned int skyboxVAO;
	unsigned int skyboxVBO;
	unsigned int cubemapTexture;
	int randChance;
	std::vector<std::string> faces
	{
		"..\\res\\right.jpg",
		"..\\res\\left.jpg",
		"..\\res\\top.jpg",
		"..\\res\\bottom.jpg",
		"..\\res\\forward.jpg",
		"..\\res\\back.jpg"
	};
};

