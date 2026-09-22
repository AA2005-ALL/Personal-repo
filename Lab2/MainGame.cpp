#include "MainGame.h"
#include "Camera.h"
#include <iostream>
#include <string>


MainGame::MainGame()
{
	_gameState = GameState::PLAY;
	Display* _gameDisplay = new Display(); //new display
}

MainGame::~MainGame()
{
}

void MainGame::run()
{
	initSystems(); 
	gameLoop();
}

void MainGame::linkADS()
{
	// Define the light position
	glm::vec3 lightPos(10.0f, 10.0f, -10.0f);

	// Define the light color (white light)
	glm::vec3 lightColor(1.0f, 1.0f, 1.0f);

	// Define the object color (red object in this case)
	glm::vec3 objectColor(1.0f, 0.0f, 0.0f);

	// Set the light position uniform in your shader
	ADS.setVec3("lightPos", lightPos);

	// Set the light color uniform in your shader
	ADS.setVec3("lightColor", lightColor);

	// Set the object color uniform in your shader
	ADS.setVec3("objectColor", objectColor);

	glm::mat4 modelMatrix = transform.GetModel();

	// Set the model matrix uniform in your shader
	ADS.setMat4("model", modelMatrix);
}

void MainGame::linkGeo()
{
	geoShader.setFloat("time", 1.0f);
	geoShader.setFloat("randColourX", 255.0f);
	geoShader.setFloat("randColourY", 165.0f);
	geoShader.setFloat("randColourZ", 0.0f);

	glm::mat4 modelMatrix = transform.GetModel();

	geoShader.setMat4("transform", modelMatrix);
}


void MainGame::initSystems()
{
	_gameDisplay.initDisplay(); 
	mesh1.loadModel("..\\res\\monkey3.obj");
	
	texture.init("..\\res\\bricks.jpg");
	ADS.init("..\\res\\ADS.vert", "..\\res\\ADS.frag");
	geoShader.initGeo("..\\res\\shaderGeoText.vert", "..\\res\\shaderGeoText.geom", "..\\res\\shaderGeoText.frag");

	myCamera.initCamera(glm::vec3(0, 0, -30), 70.0f, (float)_gameDisplay.getWidth()/_gameDisplay.getHeight(), 0.01f, 1000.0f);
	counter = 0.0f;

	vector<std::string> faces
	{
		"..\\res\\skybox\\right.jpg",
		"..\\res\\skybox\\left.jpg",
		"..\\res\\skybox\\top.jpg",
		"..\\res\\skybox\\bottom.jpg",
		"..\\res\\skybox\\front.jpg",
		"..\\res\\skybox\\back.jpg"
	};

	skybox.init(faces);
}

void MainGame::gameLoop()
{
	while (_gameState != GameState::EXIT)
	{
		processInput();
		drawGame();
	}
}

void MainGame::processInput()
{
	SDL_Event evnt;

	while(SDL_PollEvent(&evnt)) //get and process events
	{
		switch (evnt.type)
		{
			case SDL_QUIT:
				_gameState = GameState::EXIT;
				break;
		}
	}
	
}


void MainGame::drawGame()
{
	_gameDisplay.clearDisplay(0.0f, 0.0f, 0.0f, 1.0f);

	transform.SetPos(glm::vec3(0.0, 0.0, 0.0));
	transform.SetRot(glm::vec3(0.0, counter * 2, 0.0));
	transform.SetScale(glm::vec3(5.0, 5.0, 5.0));

	//ADS.Bind();
	geoShader.Bind();
	//linkADS();
	linkGeo();
	ADS.Update(transform, myCamera);

	texture.Bind(0);
	mesh1.draw();
	
	counter = counter + 0.01f;

	skybox.draw(&myCamera);
				
	glEnableClientState(GL_COLOR_ARRAY); 
	glEnd();

	_gameDisplay.swapBuffer();
} 