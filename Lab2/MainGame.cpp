#include "MainGame.h"
#include "Camera.h"
#include <iostream>
#include <cmath>

static const int START_OBJECT_COUNT = 0;
static const int MIN_OBJECT_COUNT = 0;
static const int MAX_OBJECT_COUNT = 0;   // immutable storage cannot grow, so allocate the ceiling
static const float GRID_SPACING = 2.0f;
static const float OBJECT_SCALE = 0.6f;        // same 0.6 in move.comp
static const int FRAMES_PER_TIMING_PRINT = 60;

static const GLuint CAMERA_BINDING = 0;
static const GLuint MODEL_BINDING = 2;
static const GLuint POSITION_BINDING = 3;      // "binding = 3" in move.comp
static const GLuint VELOCITY_BINDING = 4;      // "binding = 4" in move.comp

static const int LOCAL_SIZE = 64;              // invocations per workgroup, local_size_x in move.comp

// the box the objects bounce in: x and z within +-halfSide, y from 0 to BOX_HEIGHT
static const float BOX_HEIGHT = 40.0f;
static const float MIN_SPEED = 2.0f;
static const float MAX_SPEED = 6.0f;

// small LCG so every run starts the same
static const unsigned int RANDOM_SEED = 12345u;
static const unsigned int LCG_MULTIPLIER = 1664525u;
static const unsigned int LCG_INCREMENT = 1013904223u;

// camera backs off with the grid. 200 units gives the Week 8 position (0, 70, -150)
static const float MIN_CAMERA_EXTENT = 200.0f;
static const float CAMERA_HEIGHT_PER_EXTENT = 0.35f;
static const float CAMERA_BACK_PER_EXTENT = 0.75f;
static const float CAMERA_FOV = 70.0f;
static const float CAMERA_FAR = 5000.0f;

static const int TERRAIN_TEXTURE_SIZE = 512; // height map pixels per side
static const int TERRAIN_LOCAL_SIZE = 16; // local_size_x and local_size_y in terrain.comp
static const int TERRAIN_GRID = 256; // grid vertices per side
static const float TERRAIN_SIZE = 400.0f; // world units per side. Same number in terrain.vert
static const GLuint HEIGHT_IMAGE_UNIT = 0; // "binding = 0" in terrain.comp
static const GLuint HEIGHT_TEXTURE_UNIT = 1; // "binding = 1" in terrain.vert

// 0 up to just under 1. Top 24 bits, the low bits of an LCG are poor.
static float nextRandom01(unsigned int& state)
{
	state = state * LCG_MULTIPLIER + LCG_INCREMENT;
	return (float)(state >> 8) / 16777216.0f;
}

static float nextVelocityComponent(unsigned int& state)
{
	float speed = MIN_SPEED + (MAX_SPEED - MIN_SPEED) * nextRandom01(state);
	if (nextRandom01(state) < 0.5f)
		speed = -speed;
	return speed;
}

MainGame::MainGame()
{
	_gameState = GameState::PLAY;
	benchmark = false;
	printsThisMode = 0;
	mode = DrawMode::CPU_UPDATE;
	objectCount = START_OBJECT_COUNT;
	moveTimeLoc = moveDtLoc = moveCountLoc = moveHalfSideLoc = moveBoxHeightLoc = -1;
	cameraUBO = 0;
	modelSSBO = 0;
	positionSSBO = 0;
	velocitySSBO = 0;
	frameIndex = 0;
	computeTimer[0] = computeTimer[1] = 0;
	drawTimer[0] = drawTimer[1] = 0;
	frameStart = 0;
	updateMsTotal = submitMsTotal = computeMsTotal = drawMsTotal = frameMsTotal = 0.0;
	framesTimed = 0;
	counter = 0.0f;
	deltaSeconds = 0.0f;
	terrainOn = false;
	heightTexture = 0;
	terrainTimeLoc = -1;
}

MainGame::~MainGame()
{
	glDeleteQueries(2, computeTimer);
	glDeleteQueries(2, drawTimer);
	glDeleteBuffers(1, &velocitySSBO);
	glDeleteBuffers(1, &positionSSBO);
	glDeleteBuffers(1, &modelSSBO);
	glDeleteBuffers(1, &cameraUBO);
	glDeleteTextures(1, &heightTexture);
}

void MainGame::run(bool bench, int startCount)
{
	benchmark = bench;
	if (startCount >= MIN_OBJECT_COUNT && startCount <= MAX_OBJECT_COUNT)
		objectCount = startCount;
	initSystems();
	gameLoop();
}

void MainGame::initSystems()
{
	_gameDisplay.initDisplay();

	mesh1.loadModel("..\\res\\sphere.obj");
	waterBomb.loadModel("..\\res\\mine.obj");
	waterBombTransform.GetModel();
	instancedShader.init("..\\res\\instanced.vert", "..\\res\\object.frag");
	terrainDrawShader.init("..\\res\\terrain.vert", "..\\res\\object.frag");
	eMapping.init("..\\res\\eMapping.vert", "..\\res\\eMapping.frag");

	moveShader.initCompute("..\\res\\move.comp");
	terrainCompShader.initCompute("..\\res\\terrain.comp");
	moveTimeLoc = glGetUniformLocation(moveShader.getID(), "time");
	moveDtLoc = glGetUniformLocation(moveShader.getID(), "dt");
	moveCountLoc = glGetUniformLocation(moveShader.getID(), "objectCount");
	moveHalfSideLoc = glGetUniformLocation(moveShader.getID(), "halfSide");
	moveBoxHeightLoc = glGetUniformLocation(moveShader.getID(), "boxHeight");
	terrainTimeLoc = glGetUniformLocation(terrainCompShader.getID(), "time");

	std::vector<std::string> faces
	{
		"..\\res\\skybox\\right.jpg",
		"..\\res\\skybox\\left.jpg",
		"..\\res\\skybox\\top.jpg",
		"..\\res\\skybox\\bottom2.jpg",
		"..\\res\\skybox\\front.jpg",
		"..\\res\\skybox\\back.jpg"
	};
	skybox.init(faces);

	initGpuBuffers();   // before layoutGrid, which uploads into them
	layoutGrid();
	placeCamera();

	waterBombTransform.SetPos(glm::vec3(0, 0, 0));
	waterBombTransform.SetScale(glm::vec3(5.0, 5.0, 5.0));
	
#ifdef _DEBUG
	std::cout << "DEBUG build: the CPU matrix loop is ~100x slower than Release. Measure in Release." << std::endl;
#else
	std::cout << "Release build" << std::endl;
#endif
	std::cout << "keys: 1 cpu update  2 compute move\n"
		<< "      up / down: object count x10 / /10, from " << MIN_OBJECT_COUNT << " to " << MAX_OBJECT_COUNT
		<< " (starting at " << objectCount << ")" << std::endl;
}

void MainGame::initTerrain()
{
	glCreateTextures(GL_TEXTURE_2D, 1, &heightTexture);
	glTextureStorage2D(heightTexture, 1, GL_R32F, TERRAIN_TEXTURE_SIZE,TERRAIN_TEXTURE_SIZE);
	glTextureParameteri(heightTexture, GL_TEXTURE_MIN_FILTER, GL_NEAREST); // the vertex shader uses texelFetch
	glTextureParameteri(heightTexture, GL_TEXTURE_MAG_FILTER, GL_NEAREST);

	std::vector<Vertex> vertices(TERRAIN_GRID * TERRAIN_GRID);

	for (int j = 0; j < TERRAIN_GRID; j++)
	{
		for (int i = 0; i < TERRAIN_GRID; i++)
		{
			const float u = (float)i / (TERRAIN_GRID - 1);
			const float v = (float)j / (TERRAIN_GRID - 1);
			Vertex& vertex = vertices[j * TERRAIN_GRID + i];
			vertex.position = glm::vec3((u - 0.5f) * TERRAIN_SIZE, 0.0f, (v - 0.5f) *
				TERRAIN_SIZE);
			vertex.texCoord = glm::vec2(u, v);
			vertex.normal = glm::vec3(0.0f, 1.0f, 0.0f); // terrain.vert works the real one out
		}
	}

	std::vector<unsigned int> indices;

	for (int j = 0; j < TERRAIN_GRID - 1; j++)
	{
		for (int i = 0; i < TERRAIN_GRID - 1; i++)
		{
			const unsigned int a = j * TERRAIN_GRID + i;
			const unsigned int b = a + TERRAIN_GRID; // one row on
			indices.push_back(a);
			indices.push_back(b);
			indices.push_back(a + 1);
			indices.push_back(a + 1);
			indices.push_back(b);
			indices.push_back(b + 1);
		}
	}

	terrainMesh.init(vertices.data(), (unsigned int)vertices.size(), indices.data(), (unsigned int)indices.size());
}

// the grid's width in world units, never less than MIN_CAMERA_EXTENT
float MainGame::gridExtent()
{
	float extent = gridSide() * GRID_SPACING;
	if (extent < MIN_CAMERA_EXTENT)
		extent = MIN_CAMERA_EXTENT;
	return extent;
}

void MainGame::placeCamera()
{
	float extent = gridExtent();
	myCamera.initCamera(glm::vec3(0.0f, extent * CAMERA_HEIGHT_PER_EXTENT, -extent * CAMERA_BACK_PER_EXTENT), CAMERA_FOV, (float)_gameDisplay.getWidth() / _gameDisplay.getHeight(), 0.1f, CAMERA_FAR);
	myCamera.setLook(glm::vec3(0.0f, 0.0f, 0.0f));
}

// mode 2 moves the positions on the GPU, so a mode change starts the grid again
void MainGame::setMode(DrawMode newMode)
{
	mode = newMode;
	layoutGrid();
}

int MainGame::gridSide()
{
	return (int)ceilf(sqrtf((float)objectCount));
}

// Square grid on the XZ plane, centred on the origin. 10,000 objects is 100 x 100.
// The only upload of positions and velocities. After this the compute shader owns them.
void MainGame::layoutGrid()
{
	positions.resize(MAX_OBJECT_COUNT);
	velocities.resize(MAX_OBJECT_COUNT);
	transforms.resize(MAX_OBJECT_COUNT);

	// mode 2 starts at random heights so the balls are not all on the floor
	const bool scatterHeight = (mode != DrawMode::CPU_UPDATE);
	const int side = gridSide();
	unsigned int randomState = RANDOM_SEED;
	for (int i = 0; i < objectCount; i++)
	{
		int x = i % side - side / 2;
		int z = i / side - side / 2;
		float y = 0.0f;
		if (scatterHeight)
			y = nextRandom01(randomState) * BOX_HEIGHT;
		positions[i] = glm::vec4(x * GRID_SPACING, y, z * GRID_SPACING, 0.0f);

		float vx = nextVelocityComponent(randomState);
		float vy = nextVelocityComponent(randomState);
		float vz = nextVelocityComponent(randomState);
		velocities[i] = glm::vec4(vx, vy, vz, 0.0f);
	}

	glNamedBufferSubData(positionSSBO, 0, objectCount * sizeof(glm::vec4), positions.data());
	glNamedBufferSubData(velocitySSBO, 0, objectCount * sizeof(glm::vec4), velocities.data());
}

void MainGame::initGpuBuffers()
{
	// Week 7: camera UBO, SubData every frame
	glCreateBuffers(1, &cameraUBO);
	glNamedBufferStorage(cameraUBO, sizeof(CameraData), nullptr, GL_DYNAMIC_STORAGE_BIT);
	glBindBufferBase(GL_UNIFORM_BUFFER, CAMERA_BINDING, cameraUBO);

	// Week 8: one matrix per object. Mode 1 fills it with SubData, mode 2 with compute.
	glCreateBuffers(1, &modelSSBO);
	glNamedBufferStorage(modelSSBO, MAX_OBJECT_COUNT * sizeof(glm::mat4), nullptr, GL_DYNAMIC_STORAGE_BIT);

	glCreateBuffers(1, &positionSSBO);
	glNamedBufferStorage(positionSSBO, MAX_OBJECT_COUNT * sizeof(glm::vec4), nullptr, GL_DYNAMIC_STORAGE_BIT);
	glCreateBuffers(1, &velocitySSBO);
	glNamedBufferStorage(velocitySSBO, MAX_OBJECT_COUNT * sizeof(glm::vec4), nullptr, GL_DYNAMIC_STORAGE_BIT);

	// two timer pairs, compute then draw, each read a frame late
	glCreateQueries(GL_TIME_ELAPSED, 2, computeTimer);
	glCreateQueries(GL_TIME_ELAPSED, 2, drawTimer);

	initTerrain();
}

void MainGame::updateCameraBuffer()
{
	CameraData cd;
	cd.view = myCamera.getView();
	cd.projection = myCamera.getProjection();
	cd.position = myCamera.getPos();
	cd.pad = 0.0f;
	glNamedBufferSubData(cameraUBO, 0, sizeof(cd), &cd);
}

// mode 1: the Week 8 loop. This is the update column.
void MainGame::updateTransforms()
{
	const glm::vec3 up(0.0f, 1.0f, 0.0f);
	const glm::vec3 scale(OBJECT_SCALE);
	for (int i = 0; i < objectCount; i++)
		transforms[i] = glm::translate(glm::vec3(positions[i])) * glm::rotate(counter + i * 0.01f, up) * glm::scale(scale);
}

void MainGame::uploadTransforms()
{
	glNamedBufferSubData(modelSSBO, 0, objectCount * sizeof(glm::mat4), transforms.data());
}

// mode 2: move.comp owns positions and velocities and writes the matrices
void MainGame::dispatchMove()
{
	glBindBufferBase(GL_SHADER_STORAGE_BUFFER, POSITION_BINDING, positionSSBO);   // read and written
	glBindBufferBase(GL_SHADER_STORAGE_BUFFER, VELOCITY_BINDING, velocitySSBO);   // read and written
	glBindBufferBase(GL_SHADER_STORAGE_BUFFER, MODEL_BINDING, modelSSBO);         // written

	// the grid is side objects wide at GRID_SPACING each, centred on the origin
	const float halfSide = gridSide() * GRID_SPACING / 2.0f;

	moveShader.Bind();
	glUniform1f(moveTimeLoc, counter);
	glUniform1f(moveDtLoc, deltaSeconds);
	glUniform1ui(moveCountLoc, (GLuint)objectCount);
	glUniform1f(moveHalfSideLoc, halfSide);
	glUniform1f(moveBoxHeightLoc, BOX_HEIGHT);

	// round up: 10,000 / 64 is 157 groups. The shader skips the spare threads.
	const GLuint groups = (GLuint)((objectCount + LOCAL_SIZE - 1) / LOCAL_SIZE);
	glDispatchCompute(groups, 1, 1);

	// the vertex shader reads what move.comp wrote
	glMemoryBarrier(GL_SHADER_STORAGE_BARRIER_BIT);
}

void MainGame::dispatchTerrain()
{
	glBindImageTexture(HEIGHT_IMAGE_UNIT, heightTexture, 0, GL_FALSE, 0,GL_WRITE_ONLY, GL_R32F);
	terrainCompShader.Bind();

	glUniform1f(terrainTimeLoc, counter);

	// Round up in x and y. 512 / 16 is exactly 32 here, but another size may not divide.
	const GLuint groups = (GLuint)((TERRAIN_TEXTURE_SIZE + TERRAIN_LOCAL_SIZE - 1) / TERRAIN_LOCAL_SIZE);
	glDispatchCompute(groups, groups, 1);

	// The vertex shader samples what the dispatch wrote. The bit names the reader: a texture fetch.
    glMemoryBarrier(GL_TEXTURE_FETCH_BARRIER_BIT);
}

void MainGame::drawInstanced()
{
	glBindBufferBase(GL_SHADER_STORAGE_BUFFER, MODEL_BINDING, modelSSBO);
	instancedShader.Bind();
	mesh1.drawInstanced(objectCount);
}

void MainGame::drawWaterMine()
{
	eMapping.Bind();
	eMapping.setMat4("model", waterBombTransform.GetModel());
	eMapping.setMat4("view", myCamera.getView());
	eMapping.setMat4("projection", myCamera.getProjection());
	waterBomb.draw();
}

void MainGame::drawTerrain()
{
	glBindTextureUnit(HEIGHT_TEXTURE_UNIT, heightTexture);
	terrainDrawShader.Bind();
	terrainMesh.draw();
}

void MainGame::beginTimer(GLuint* queries)
{
	glBeginQuery(GL_TIME_ELAPSED, queries[frameIndex & 1]);
}

void MainGame::endTimer()
{
	glEndQuery(GL_TIME_ELAPSED);
}

// last frame's query. This frame's has not run yet and asking for it would stall.
double MainGame::readTimer(GLuint* queries)
{
	if (frameIndex == 0)
		return 0.0;
	GLuint64 ns = 0;
	glGetQueryObjectui64v(queries[(frameIndex + 1) & 1], GL_QUERY_RESULT, &ns);
	return ns / 1.0e6;
}

static double msSince(Uint64 then)
{
	return (SDL_GetPerformanceCounter() - then) * 1000.0 / SDL_GetPerformanceFrequency();
}

void MainGame::recordFrameTime()
{
	Uint64 now = SDL_GetPerformanceCounter();
	if (frameStart != 0)
	{
		double frameMs = (now - frameStart) * 1000.0 / SDL_GetPerformanceFrequency();
		deltaSeconds = (float)(frameMs / 1000.0);   // dt for move.comp next frame
		counter += deltaSeconds;
		frameMsTotal += frameMs;
	}
	frameStart = now;

	framesTimed++;
	if (framesTimed == FRAMES_PER_TIMING_PRINT)
	{
		static const char* names[] = { "", "cpu update", "compute move" };
		std::cout << names[(int)mode] << "  " << objectCount << " objects"
			<< "  update " << updateMsTotal / framesTimed << " ms"
			<< "  submit " << submitMsTotal / framesTimed << " ms"
			<< "  compute " << computeMsTotal / framesTimed << " ms"
			<< "  draw " << drawMsTotal / framesTimed << " ms"
			<< "  frame " << frameMsTotal / framesTimed << " ms" << std::endl;
		updateMsTotal = submitMsTotal = computeMsTotal = drawMsTotal = frameMsTotal = 0.0;
		framesTimed = 0;

		// --bench: the first print of a mode includes the switch, keep the second, then move on
		if (benchmark && ++printsThisMode == 2)
		{
			printsThisMode = 0;
			if (mode == DrawMode::COMPUTE_MOVE)
				_gameState = GameState::EXIT;
			else
				setMode((DrawMode)((int)mode + 1));
		}
	}
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
	while (SDL_PollEvent(&evnt))
	{
		switch (evnt.type)
		{
		case SDL_QUIT:
			_gameState = GameState::EXIT;
			break;
		case SDL_KEYDOWN:
			if (evnt.key.repeat)   // a held key must not fire again
				break;

			if (evnt.key.keysym.sym >= SDLK_1 && evnt.key.keysym.sym <= SDLK_2)
			{
				setMode((DrawMode)(evnt.key.keysym.sym - SDLK_1 + 1));
			}

			if (evnt.key.keysym.sym == SDLK_t)
			{
				terrainOn = !terrainOn;
				std::cout << "terrain " << (terrainOn ? "on" : "off") << std::endl;
			}

			if (evnt.key.keysym.sym == SDLK_UP || evnt.key.keysym.sym == SDLK_DOWN)
			{
				const bool up = (evnt.key.keysym.sym == SDLK_UP);

				if (up && objectCount * 10 > MAX_OBJECT_COUNT)
				{
					std::cout << "already at the ceiling: " << MAX_OBJECT_COUNT << " objects" << std::endl;
				}

				else if (!up && objectCount / 10 < MIN_OBJECT_COUNT)
				{
					std::cout << "already at the floor: " << MIN_OBJECT_COUNT << " objects" << std::endl;
				}

				else
				{
					objectCount = up ? objectCount * 10 : objectCount / 10;
					std::cout << "object count " << objectCount << std::endl;
					layoutGrid();
					placeCamera();
				}
			}
			break;
		}
	}
}

void MainGame::drawGame()
{
	_gameDisplay.clearDisplay(0.8f, 0.8f, 0.8f, 1.0f);

	// update: CPU time making the per-object data. Mode 1 the loop, mode 2 the dispatch.
	Uint64 t0 = SDL_GetPerformanceCounter();
	updateCameraBuffer();
	beginTimer(computeTimer); // nothing is dispatched in mode 1, so it reads ~0

	switch (mode)
	{
	  case DrawMode::CPU_UPDATE:   updateTransforms();   break;
	  case DrawMode::COMPUTE_MOVE: dispatchMove();       break;
	}

	// after the switch (mode), before endTimer()
	if (terrainOn)
	{
		dispatchTerrain();
	}

	endTimer();
	updateMsTotal += msSince(t0);

	beginTimer(drawTimer);
	glActiveTexture(GL_TEXTURE2);
	glBindTexture(GL_TEXTURE_CUBE_MAP, skybox.textureID);
	skybox.draw(&myCamera);

	// after the switch (mode), before endTimer()
	if (terrainOn)
	{
		drawTerrain();
	}

	// submit: CPU time to issue the draw, not to run it. Mode 1 also issues its upload here.
	Uint64 t1 = SDL_GetPerformanceCounter();

	if (mode == DrawMode::CPU_UPDATE)
	{
		uploadTransforms();
	}

	drawInstanced();
	drawWaterMine();

	submitMsTotal += msSince(t1);
	endTimer();

	computeMsTotal += readTimer(computeTimer);   // outside the CPU regions: a read can wait
	drawMsTotal += readTimer(drawTimer);

	_gameDisplay.swapBuffer();
	frameIndex++;
	recordFrameTime();
}
