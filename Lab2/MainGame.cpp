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

void MainGame::linkEnvMapping()
{
	//GLuint t1L = glGetUniformLocation(environmentMapping.ID(), "skybox");
	//GLuint t2L = glGetUniformLocation(environmentMapping.ID(), "diffuseMap");

	//glActiveTexture(GL_TEXTURE0); 
	//glBindTexture(GL_TEXTURE_2D, ADS.ID());
	//glUniform1i(t1L, 0);

	//glActiveTexture(GL_TEXTURE1);
	//glBindTexture(GL_TEXTURE_2D, environmentMapping.ID());
	//glUniform1i(t2L, 1);

	environmentMapping.setVec3("cameraPos", myCamera.getPos());
	environmentMapping.setMat4("model", transform.GetModel());
	environmentMapping.setMat4("view", myCamera.getView());
	environmentMapping.setMat4("projection", myCamera.getProjection());
}

void MainGame::initSystems()
{
	_gameDisplay.initDisplay(); 
	mesh1.loadModel("..\\res\\Crate1.obj");
	
	texture.init("..\\res\\bricks.jpg");
	ADS.init("..\\res\\ADS.vert", "..\\res\\ADS.frag");
	FBOShader.init("..\\res\\FBOShader.vert", "..\\res\\FBOShader.frag");
	geoShader.initGeo("..\\res\\shaderGeoText.vert", "..\\res\\shaderGeoText.geom", "..\\res\\shaderGeoText.frag");
	environmentMapping.init("..\\res\\eMapping.vert", "..\\res\\eMapping.frag");

	myCamera.initCamera(glm::vec3(0, 0, -30), 70.0f, (float)_gameDisplay.getWidth()/_gameDisplay.getHeight(), 0.01f, 1000.0f);
	counter = 0.0f;

	float w = _gameDisplay.getWidth();
	float h = _gameDisplay.getHeight();

	glGenFramebuffers(1, &FBO);
	glBindFramebuffer(GL_FRAMEBUFFER, FBO);

	// create a colorbuffer for attachment texture
	glGenTextures(1, &CBO);
	glBindTexture(GL_TEXTURE_2D, CBO);
	glTexImage2D(GL_TEXTURE_2D, 0, GL_RGB, w, h, 0, GL_RGB, GL_UNSIGNED_BYTE, NULL);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
	glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, CBO, 0);

	// create a renderbuffer object for depth and stencil attachment 
	glGenRenderbuffers(1, &RBO);
	glBindRenderbuffer(GL_RENDERBUFFER, RBO);
	glRenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, w, h); // use a single renderbuffer object for both a depth AND stencil buffer.

	glBindRenderbuffer(GL_RENDERBUFFER, 0);
	glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, RBO); // now actually attach it

	// now that we actually created the framebuffer and added all attachments we want to check if it is actually complete now
	if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
	{
		cout << "FRAMEBUFFER:: Framebuffer is complete!" << endl;
		glBindFramebuffer(GL_FRAMEBUFFER, 0);
	}



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

	glBindFramebuffer(GL_FRAMEBUFFER, FBO);
	glEnable(GL_DEPTH_TEST);
	glClearColor(0.1f, 0.1f, 0.1f, 1.0f);
	glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);

	skybox.draw(&myCamera);

	transform.SetPos(glm::vec3(0.0, 0.0, 0.0));
	transform.SetRot(glm::vec3(0.0, counter * 2, 0.0));
	transform.SetScale(glm::vec3(5.0, 5.0, 5.0));

	//ADS draw
	ADS.Bind();
	linkADS();
	ADS.Update(transform, myCamera);

	//geo draw
	//geoShader.Bind();
	//linkGeo();
	//geoShader.Update(transform, myCamera);
	//mesh1.draw();
	//mesh1.updateSphereData(*transform.GetPos(), 0.62f);

	texture.Bind(0);

	//env draw
	environmentMapping.Bind();
	linkEnvMapping();
	glActiveTexture(GL_TEXTURE0);
	glBindTexture(GL_TEXTURE_CUBE_MAP, skybox.textureID);
	mesh1.updateSphereData(*transform.GetPos(), 0.62f);


	glBindFramebuffer(GL_FRAMEBUFFER, 0);

	glDisable(GL_DEPTH_TEST);
	//glClearColor(1.0f, 1.0f, 1.0f, 1.0f); // set clear color to white (not really necessary actually, since we won't be able to see behind the quad anyways)
	glClear(GL_COLOR_BUFFER_BIT);

	FBOShader.Bind();
	glBindVertexArray(quadVAO);
	glBindTexture(GL_TEXTURE_2D, CBO);	// use the color attachment texture as the texture of the quad plane
	glDrawArrays(GL_TRIANGLES, 0, 6);

	mesh1.draw();
	/*counter = counter + 0.01f;*/
				
	glEnableClientState(GL_COLOR_ARRAY); 
	glEnd();

	_gameDisplay.swapBuffer();
} 