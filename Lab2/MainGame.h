#pragma once
#include <SDL\SDL.h>
#include <GL\glew.h>
#include <vector>
#include "Display.h"
#include "Shader.h"
#include "Camera.h"
#include "Mesh.h"
#include "transform.h"
#include "SkyBox.h"

enum class GameState { PLAY, EXIT };

// Week 9: who builds the matrices. Keys 1 and 2 switch at run time.
enum class DrawMode
{
	CPU_UPDATE = 1,    // the Week 8 baseline: the CPU builds the matrices, SubData uploads them
	COMPUTE_MOVE = 2   // a compute shader moves the objects and builds their matrices. The GPU owns the state
};

// Week 7: per-frame data for every shader. Matches CameraBlock (std140).
struct CameraData
{
	glm::mat4 view;        // offset   0
	glm::mat4 projection;  // offset  64
	glm::vec3 position;    // offset 128
	float pad;             // offset 140
};
static_assert(sizeof(CameraData) == 144, "CameraData does not match std140 CameraBlock");

class MainGame
{
public:
	MainGame();
	~MainGame();

	void run(bool benchmark, int startCount);

private:
	void initSystems();
	void initTerrain(); 
	void dispatchTerrain();
	void drawTerrain();
	float gridExtent();
	void placeCamera();
	void setMode(DrawMode newMode);
	int gridSide();
	void layoutGrid();
	void initGpuBuffers();
	void processInput();
	void gameLoop();
	void drawGame();

	void updateCameraBuffer();
	void updateTransforms();               // mode 1
	void uploadTransforms();               // mode 1
	void dispatchMove();                   // mode 2
	void drawInstanced();

	void beginTimer(GLuint* queries);
	void endTimer();
	double readTimer(GLuint* queries);
	void recordFrameTime();

	Display _gameDisplay;
	GameState _gameState;
	Mesh mesh1;            // ball
	Camera myCamera;
	Shader instancedShader; // instanced.vert: model[gl_InstanceID] from the SSBO
	Shader moveShader;      // move.comp
	Skybox skybox;

	bool benchmark;        // --bench: cycle modes 1 and 2, one timing line each, then exit
	int printsThisMode;
	DrawMode mode;
	int objectCount;

	// vec4 not vec3: std430 pads a vec3 array to 16 bytes a row
	std::vector<glm::vec4> positions;
	std::vector<glm::vec4> velocities;
	std::vector<glm::mat4> transforms;    // mode 1 only

	// the move.comp uniform locations, looked up once
	GLint moveTimeLoc, moveDtLoc, moveCountLoc, moveHalfSideLoc, moveBoxHeightLoc;

	GLuint cameraUBO;      // binding 0
	GLuint modelSSBO;      // binding 2, mat4 per object
	GLuint positionSSBO;   // binding 3, vec4 per object
	GLuint velocitySSBO;   // binding 4, vec4 per object
	unsigned int frameIndex;

	// timing: CPU update, CPU submit, GPU compute, GPU draw, frame
	GLuint computeTimer[2];
	GLuint drawTimer[2];
	Uint64 frameStart;
	double updateMsTotal, submitMsTotal, computeMsTotal, drawMsTotal, frameMsTotal;
	int framesTimed;

	Shader terrainCompShader; // terrain.comp
	Shader terrainDrawShader; // terrain.vert and object.frag
	Mesh terrainMesh; // the flat grid
	GLuint heightTexture; // R32F: written by terrain.comp, sampled by terrain.vert
	GLint terrainTimeLoc;
	bool terrainOn; // t key

	float counter;         // seconds since start
	float deltaSeconds;    // last frame length, the dt for move.comp
};
