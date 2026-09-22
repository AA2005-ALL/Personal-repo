#pragma once
#include <SDL\SDL.h>
#include <GL/glew.h>
#include "Display.h" 
#include "Shader.h"
#include "Mesh.h"
#include "Texture.h"
#include "transform.h"
#include "SkyBox.h"

enum class GameState{PLAY, EXIT};

class MainGame
{
public:
	MainGame();
	~MainGame();
	void linkADS();

	void run();

private:

	void initSystems();
	void processInput();
	void gameLoop();
	void drawGame();
	void linkGeo();

	Display _gameDisplay;
	GameState _gameState;
	Mesh mesh1;
	Camera myCamera;
	Texture texture;
	Shader ADS;
	Shader geoShader;
	Transform transform;

	Skybox skybox;

	float counter;


};

