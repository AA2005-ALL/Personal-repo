#include "MainGame.h"
#include "Camera.h"
#include <iostream>
#include <string>


MainGame::MainGame()
{
	_gameState = GameState::PLAY;
	Display* _gameDisplay = new Display(); 
	arrowShaderChosen = false; 
	ballMoveAmount = 0.0f; 
	ballMoveAmountY = 0.0f; 
	ballSpinAmount = 0.0f; 
	counter = 0.0f; 
	cubemapTexture = 0;
	mousePressedOnce = false; 
	mousePressedTwice = false; 
	rotateLeft = false; 
	rotateUp = false; 
	skyboxVAO = 0; 
	skyboxVBO = 0;
	penaltiesScored = 0; 
	penaltiesTaken = 0; 
	randChance = 0;
}

MainGame::~MainGame()
{
	//delete &_gameState; 
	/*delete &_gameDisplay; */
}



void MainGame::run()
{
	initSystems();
	gameLoop();
}

void MainGame::linkADS(Transform& transform)
{
	//Define the light position
	glm::vec3 lightPos(20.0f, 20.0f, 20.0f);

	//Define the model being passed through
	glm::mat4 modelMatrix = transform.GetModel();

	ADS.setInt("Texture", 0);
	ADS.setFloat("blendAmount", 0.5f);
	ADS.setVec3("viewPos", myCamera.getPos());

	//Light position uniform
	ADS.setVec3("lightPos", lightPos);

	ADS.setVec3("light.ambient", 0.2f, 0.2f, 0.2f);
	ADS.setVec3("light.diffuse", 1.0f, 1.0f, 1.0f);
	ADS.setVec3("light.specular", 0.8f, 0.8f, 0.8f);

	//Material properties
	ADS.setFloat("material.shininess", 32.0f);

	//Model matrix uniform
	ADS.setMat4("model", modelMatrix);
	ADS.setMat4("transform", transform.GetMVP(myCamera));
}

void MainGame::linkSkybox()
{
    glm::mat4 view = glm::mat4(glm::mat3(myCamera.getView()));

	skyBox.setMat4("view", view);
	skyBox.setMat4("projection", myCamera.getProjection());
	skyBox.setInt("skybox", 0);
}


void MainGame::initSystems()
{
	_gameDisplay.initDisplay();
	counter = 0;
	ballMoveAmount = 0;
	ballMoveAmountY = 0;
	ballSpinAmount = 0;
	randDivDir();

	//Model loading
	arrowMesh.loadModel("..\\res\\arrow.obj");
	ballMesh.loadModel("..\\res\\Football.obj");
	goalMesh.loadModel("..\\res\\Goals.obj");
	goalKeeperMesh.loadModel("..\\res\\goalie.obj");

	//Texture loading 
	arrowTexture.init("..\\res\\arrowText.jpg");
	ballTexture.init("..\\res\\footballtexture.jpg");
	goalTexture.init("..\\res\\goalsTexture.jpg");
	goalKeeperTexture.init("..\\res\\goalieTexture.jpg");

	//Shader loading 
	ADS.init("..\\res\\ADS.vert", "..\\res\\ADS.frag"); 
	skyBox.init("..\\res\\Cubemap.vert", "..\\res\\Cubemap.frag"); 
	textShader.init("..\\res\\Text.vert", "..\\res\\Text.frag");

	cubemapTexture = skyBoxTexture.loadCubemap(faces);

	//Skybox initialisation
	glGenVertexArrays(1, &skyboxVAO);
	glGenBuffers(1, &skyboxVBO);
	glBindVertexArray(skyboxVAO);
	glBindBuffer(GL_ARRAY_BUFFER, skyboxVBO);
	glBufferData(GL_ARRAY_BUFFER, sizeof(skyboxVertices), skyboxVertices, GL_STATIC_DRAW);
	glEnableVertexAttribArray(0);
	glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), (void*)0);
	glBindVertexArray(0);

	linkSkybox();
	skyBox.Bind();


	fontTexture.BindFont();

	myCamera.initCamera(glm::vec3(0, 0, -30), glm::radians(100.0f), (float)_gameDisplay.getWidth() / _gameDisplay.getHeight(), 0.01f, 1000.0f);
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

		case SDL_KEYDOWN:
			switch (evnt.key.keysym.scancode)
			{
			    case SDL_SCANCODE_R:
				if (penaltiesTaken == 5)
				{
					penaltiesTaken = 0;
					penaltiesScored = 0;
					mousePressedOnce = false; 
					mousePressedTwice = false; 
					std::cout << "Shoould reset " << penaltiesTaken << std::endl;
				}
				break;
			}
		break;
		}
	}

}

int MainGame::randDivDir()
{
	int maxChance = 2;
    int minChance = 0;

	random_device randMachine;

	mt19937 gen(randMachine());

	uniform_int_distribution<> distrib(minChance, maxChance);

	randChance = distrib(gen);

	std::cout << randChance << std::endl; 
	return randChance;
}

bool MainGame::CheckCollision(const Collider& col1, const Collider& col2)
{
	float xDiff = abs(col1.x - col2.x);
	bool zOverlap = (col1.z < col2.z + col2.depth) && (col1.z + col1.depth > col2.z);

	std::cout << "xDiff: " << xDiff << std::endl;

	return xDiff < 16 && zOverlap;
}

void MainGame::drawGame()
{
	_gameDisplay.clearDisplay(0.0f, 0.0f, 0.0f, 1.0f);

	if (penaltiesTaken == 5)
	{
		mousePressedTwice = true; 


		if (penaltiesScored < 3)
		{
			fontTexture.DrawText("You lost! Press R to retry", glm::vec2(352.0f, 350.0f), glm::vec4(1.0f, 1.0f, 1.0f, 1.0f), 32.0f, _gameDisplay.getWidth(), _gameDisplay.getHeight());
		}

		else
		{
			fontTexture.DrawText("You won! Press R to retry", glm::vec2(352.0f, 350.0f), glm::vec4(1.0f, 1.0f, 1.0f, 1.0f), 32.0f, _gameDisplay.getWidth(), _gameDisplay.getHeight());
		}
	}

	//Draws Skybox
	glDepthMask(GL_FALSE);
	glDepthFunc(GL_LEQUAL);
	skyBox.Bind();
	linkSkybox();

	glBindVertexArray(skyboxVAO);
	glDrawArrays(GL_TRIANGLES, 0, 36);
	glBindVertexArray(0);

	glDepthMask(GL_TRUE);
	glDepthFunc(GL_LESS);

	//Draws goal posts 
	goalTransform.SetPos(glm::vec3(56.0f, 8.0f, 74.0f));
	goalTransform.SetRot(glm::vec3(-0.2f, 3.1f, 0.0f));
	goalTransform.SetScale(glm::vec3(13.0f, 10.0f, 13.0f));

	//Keeps goalkeeper the same size at all times 
	goalKeeperTransform.SetScale(glm::vec3(15.0f, 15.0f, 15.0f));

	ADS.Bind();

	//Goal post binding 
	linkADS(goalTransform);
	goalTexture.Bind(0);
	goalMesh.draw();

	//Goalkeeper binding 
	linkADS(goalKeeperTransform);
	goalKeeperTexture.Bind(0);
	goalKeeperMesh.draw();

   
	// Disable depth test so text always draws on top
	glDisable(GL_DEPTH_TEST);

	//Text binding + drawing
	textShader.Bind();
	 
	glm::mat4 textProjection = glm::ortho(0.0f, (float)_gameDisplay.getWidth(), 0.0f, (float)_gameDisplay.getHeight(), -1.0f, 1.0f);

	textShader.setMat4("projection", textProjection);
	textShader.setVec4("textColor", glm::vec4(1.0f, 1.0f, 1.0f, 1.0f));
	textShader.setInt("fontAtlas", 0);

	glActiveTexture(GL_TEXTURE0);
	glBindTexture(GL_TEXTURE_2D, fontTexture.fontAtlasTextureID);

	fontTexture.DrawText(std::to_string(penaltiesScored) + "/" + std::to_string(5), glm::vec2(50.0f, (float)_gameDisplay.getHeight() - 50.0f), glm::vec4(1.0f, 1.0f, 1.0f, 1.0f), 64.0f, _gameDisplay.getWidth(), _gameDisplay.getHeight());

	glEnable(GL_DEPTH_TEST);

	if (mousePressedOnce == false && mousePressedTwice == false)
	{
		//Draws Goalie 
		goalKeeperTransform.SetPos(glm::vec3(0.0f, 10.0f, goalTransform.GetPos()->z));
		goalKeeperTransform.SetRot(glm::vec3(0.0f, 0.0f, 0.0f));

		arrowTransform.GetRot();
		arrowTransform.SetPos(glm::vec3(0.0f, 0.0f, -28.0f));

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
			counter = counter - 0.05f;
			arrowTransform.SetRot(glm::vec3(-10.0f, counter - 0.1f, 0.0f));
		}

		else
		{
			counter = counter + 0.05f;
			arrowTransform.SetRot(glm::vec3(-10.0f, counter + 0.1f, 0.0f));
		}

		arrowTransform.SetScale(glm::vec3(0.2f, 0.2f, 0.2f));

		ADS.Bind();
		linkADS(arrowTransform);
		arrowTexture.Bind(0);
		arrowMesh.draw();
	}

	else if (mousePressedOnce == true && mousePressedTwice == false)
	{

		if (arrowTransform.GetRot()->x >= -9.0f)
		{
			rotateUp = true;
		}

		if (arrowTransform.GetRot()->x <= -11.0f)
		{
			rotateUp = false;
		}

		if (rotateUp)
		{
			counter = counter - 0.05f;
			arrowTransform.SetRot(glm::vec3(counter + 0.1f, arrowTransform.GetRot()->y, arrowTransform.GetRot()->z));
		}

		else
		{
			counter = counter + 0.05f;
			arrowTransform.SetRot(glm::vec3(counter - 0.1f, arrowTransform.GetRot()->y, arrowTransform.GetRot()->z));
		}

		ADS.Bind();
		linkADS(arrowTransform);
	    arrowTexture.Bind(0);
		arrowMesh.draw();
	}

	if (mousePressedTwice && penaltiesTaken < 5)
	{
		ballTransform.SetScale(glm::vec3(2.5f, 2.5f, 2.5f));

		//Sets initial ball position 
		if (ballMoveAmount == 0)
		{
			ballTransform.SetPos(glm::vec3(arrowTransform.GetPos()->x, arrowTransform.GetPos()->y, arrowTransform.GetPos()->z));
		}

		//Sets ball position based on arrow placement 
	 	else if (ballTransform.GetPos()-> z < goalTransform.GetPos()->z)
		{
			float ballXPos = arrowTransform.GetRot()->y;

			if (ballXPos < 0)
			{
				ballTransform.SetPos(glm::vec3(ballXPos * ballMoveAmount, ballMoveAmountY * -1.1, ballMoveAmount * -2));
				ballTransform.SetRot(glm::vec3(0.0f, 0.0f, ballSpinAmount * 1.1f));
			}

			else if (ballXPos > 0)
			{
				ballTransform.SetPos(glm::vec3(ballXPos * ballMoveAmount, ballMoveAmountY * -1.1, ballMoveAmount * -2));
				ballTransform.SetRot(glm::vec3(0.0f, 0.0f, ballSpinAmount * 1.1f));
			}
			  
		}

		//Defines both the ball and goalies colliders and the ditance between them 
		float zDistanceToBallAndGoalie = abs(ballTransform.GetPos()->z - goalKeeperTransform.GetPos()->z);

		Collider goalieCollider = { goalKeeperTransform.GetPos()->x, goalKeeperTransform.GetPos()->z, 5.0f, 5.0f};
		Collider ballCollider = { ballTransform.GetPos()->x, ballTransform.GetPos()->z, 2.5f, 2.5f };


		//Dives Left
		if (randChance == 1)
		{
			if (goalKeeperTransform.GetPos()->x <= 11.5)
			{
				goalKeeperTransform.SetPos(glm::vec3(-1.1 * ballMoveAmount, goalKeeperTransform.GetPos()->y, goalKeeperTransform.GetPos()->z));

				if (goalKeeperTransform.GetRot()->z >= -1.1)
				{
					goalKeeperTransform.SetRot(glm::vec3(0.0f, 0.0f, 0.05f * ballMoveAmount));
				}
			}
		}

		//Dives right
		if (randChance == 2)
		{
			if (goalKeeperTransform.GetPos()->x >= -11.5)
			{
				goalKeeperTransform.SetPos(glm::vec3(1.1 * ballMoveAmount, goalKeeperTransform.GetPos()->y, goalKeeperTransform.GetPos()->z));


				if (goalKeeperTransform.GetRot()->z <= 1.1)
				{
					goalKeeperTransform.SetRot(glm::vec3(0.0f, 0.0f, -0.05f * ballMoveAmount));
				}
			}
		}

		//Stays in the middle
		else if (randChance < 1)
		{
			goalKeeperTransform.SetPos(glm::vec3(0.0, goalKeeperTransform.GetPos()->y, goalKeeperTransform.GetPos()->z));
		}

		//Goal scored logic 
		if (ballTransform.GetPos()->z >= goalTransform.GetPos()->z && CheckCollision(ballCollider, goalieCollider) == false)
		{
			penaltiesTaken++;
			penaltiesScored++;
			ballMoveAmount = 0.0f;
			ballMoveAmountY = 0.0f;
			ballTransform.SetPos(glm::vec3(arrowTransform.GetPos()->x, arrowTransform.GetPos()->y, arrowTransform.GetPos()->z));
			mousePressedOnce = false;
			mousePressedTwice = false;
			randDivDir();

			std::cout << "Goal " << penaltiesScored << std::endl;

			return;
		}

		ADS.Bind();
		linkADS(ballTransform);

		ballTexture.Bind(0);
		ballMesh.draw();

		//Penalty missed logic 
		if (zDistanceToBallAndGoalie < 1.0f)
		{
			if (CheckCollision(ballCollider, goalieCollider) == false)
			{
				ballMoveAmount = ballMoveAmount - 0.05f;
				ballMoveAmountY = ballMoveAmountY - 0.05f;
			}

			else
			{
				penaltiesTaken++;
				ballMoveAmount = 0.0f;
				ballMoveAmountY = 0.0f;
				ballTransform.SetPos(glm::vec3(arrowTransform.GetPos()->x, arrowTransform.GetPos()->y, arrowTransform.GetPos()->z));
				mousePressedOnce = false; 
				mousePressedTwice = false; 
				randDivDir();
				std::cout << "Missed "  << penaltiesTaken << std::endl;
			}
		}

		else
		{
			ballMoveAmount = ballMoveAmount - 0.05f;
			ballMoveAmountY = ballMoveAmountY - 0.05f;
		}

		ballSpinAmount = ballSpinAmount + 0.05f;
	}

	_gameDisplay.swapBuffer();
}
