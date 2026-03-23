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

void MainGame::linkADS(Transform& transform)
{
	// Define the light position
	glm::vec3 lightPos(20.0f, 20.0f, 20.0f);


	// Set the light position uniform in your shader
	ADS.setVec3("lightPos", lightPos);

	ADS.setVec3("light.ambient", 0.2f, 0.2f, 0.2f);
	ADS.setVec3("light.diffuse", 1.0f, 1.0f, 1.0f);
	ADS.setVec3("light.specular", 0.8f, 0.8f, 0.8f);

	// material properties
	ADS.setFloat("material.shininess", 32.0f);

    

	glm::mat4 modelMatrix = transform.GetModel();

	// Set the model matrix uniform in your shader
	ADS.setMat4("model", modelMatrix);
}


void MainGame::initSystems()
{
	_gameDisplay.initDisplay();
	counter = 0;
	//mesh1.init(vertices, sizeof(vertices) / sizeof(vertices[0]), indices, sizeof(indices) / sizeof(indices[0])); //size calcuated by number of bytes of an array / no bytes of one element
	arrowMesh.loadModel("..\\res\\arrow.obj");
	ballMesh.loadModel("..\\res\\Football.obj");


	arrowTexture.init("..\\res\\arrowText.jpg"); 
	ballTexture.init("..\\res\\footballtexture.jpg");
	shader.init("..\\res\\shader.vert", "..\\res\\shader.frag"); //new shader
	ADS.init("..\\res\\ADS.vert", "..\\res\\ADS.frag"); //new shader

	myCamera.initCamera(glm::vec3(0, 0, -30), glm::radians(70.0f), (float)_gameDisplay.getWidth() / _gameDisplay.getHeight(), 0.01f, 1000.0f);
	counter = 0.0f;
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

	while (SDL_PollEvent(&evnt)) //get and process events
	{
		switch (evnt.type)
		{
		case SDL_QUIT:
			_gameState = GameState::EXIT;
			break;
		case SDL_MOUSEBUTTONDOWN: 
			if (mousePressedOnce)
			{
				mousePressedTwice = true; 
			}
			else
			{
				mousePressedOnce = true;
			}
			break;
		}
	}

}


void MainGame::drawGame()
{
	_gameDisplay.clearDisplay(0.0f, 0.0f, 0.0f, 1.0f);

	if (mousePressedOnce == false && mousePressedTwice == false)
	{
		arrowTransform.GetRot();
		arrowTransform.SetPos(glm::vec3(0.0, 0.0, 0.0));

		if (arrowTransform.GetRot()->y >= 1.0f)
		{
			rotateLeft = true;
		}

		if (arrowTransform.GetRot()->y <= -1.0f)
		{
			rotateLeft = false; 
		}

		if (rotateLeft)
		{ 
			counter = counter - 0.05;
			arrowTransform.SetRot(glm::vec3(0.0, counter - 0.1, 0.0));
		}

		else
		{
			counter = counter + 0.05;
			arrowTransform.SetRot(glm::vec3(0.0, counter + 0.1, 0.0));
		}

		arrowTransform.SetScale(glm::vec3(0.5f, 0.5f, 0.5f));

		ADS.Bind();
		linkADS(arrowTransform);
		ADS.Update(arrowTransform, myCamera);
		shader.Bind();
		shader.Update(arrowTransform, myCamera);
		arrowTexture.Bind(0);
		arrowMesh.draw();
	}

   	else if(mousePressedOnce == true && mousePressedTwice == false)
	{
		arrowTransform.SetPos(glm::vec3(0.0, 0.0, 0.0));
		arrowTransform.SetScale(glm::vec3(0.5f, 0.5f, 0.5f));

		if (arrowTransform.GetRot()->x >= 1.0f)
		{
			rotateUp = true;
		}

		if (arrowTransform.GetRot()->x <= -1.0f)
		{
			rotateUp = false;
		}

		if (rotateUp)
		{
			counter = counter - 0.05;
			arrowTransform.SetRot(glm::vec3(counter - 0.1, arrowTransform.GetRot()->y, arrowTransform.GetRot()->z));
		}

		else
		{
			counter = counter + 0.05;
			arrowTransform.SetRot(glm::vec3(counter + 0.1, arrowTransform.GetRot()->y, arrowTransform.GetRot()->z));
		}

		ADS.Bind();
		linkADS(arrowTransform);
		ADS.Update(arrowTransform, myCamera);
		shader.Bind();
		shader.Update(arrowTransform, myCamera);
		arrowTexture.Bind(0);
		arrowMesh.draw();
	}

	if (mousePressedTwice)
	{
		if (ballMoveAmount == 0)
		{
			ballTransform.SetPos(glm::vec3(arrowTransform.GetRot()->x, arrowTransform.GetRot()->y, 0.0));
		}

		else
		{
			float ballHeight = arrowTransform.GetRot()->y;

			//ballHeight -= gravity;

			ballTransform.SetPos(glm::vec3(ballMoveAmount * 1.1, ballMoveAmount * ballHeight, ballMoveAmount * 2));
		}

		ballTransform.SetScale(glm::vec3(0.8f, 0.8f, 0.8f));


		ADS.Bind();
		linkADS(ballTransform);
		ADS.Update(ballTransform, myCamera);
		shader.Bind();
		shader.Update(ballTransform, myCamera);
		ballTexture.Bind(0);
		ballMesh.draw();

		ballMoveAmount = ballMoveAmount + 0.1;
	}

	glEnableClientState(GL_COLOR_ARRAY);
	glEnd();

	_gameDisplay.swapBuffer();
}