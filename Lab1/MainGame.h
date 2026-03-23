#pragma once
#include <SDL\SDL.h>
#include <GL/glew.h>
#include "Display.h" 
#include "Shader.h"
#include "Mesh.h"
#include "Texture.h"
#include "transform.h"

enum class GameState{PLAY, EXIT};

class MainGame
{
public:
	MainGame();
	~MainGame();
	void linkADS(Transform&);

	void run();

private:

	void initSystems();
	void processInput();
	void gameLoop();
	void drawGame();

	Display _gameDisplay;
	GameState _gameState;
	Mesh arrowMesh;
	Mesh ballMesh;
	Camera myCamera;
	Texture ballTexture; 
	Texture arrowTexture;
	Shader ADS;
	Shader shader;
	Transform ballTransform;
	Transform arrowTransform; 

	bool mousePressedOnce; 
	bool mousePressedTwice;
	bool arrowShaderChosen;
	bool rotateLeft; 
	bool rotateUp;

	float counter;
	float ballMoveAmount;
	float gravity = 0.0098f;

};

