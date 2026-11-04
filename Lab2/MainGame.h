#pragma once
#include <SDL\SDL.h>
#include <GL/glew.h>
#include "Display.h" 
#include "Shader.h"
#include "Mesh.h"
#include "Texture.h"
#include "transform.h"
#include "Audio.h"
#include "SkyBox.h"
#include "GameObject.h"

enum class GameState { PLAY, EXIT };

class MainGame
{
public:
	MainGame();
	~MainGame();

	void run();

private:

	void initSystems();
	void processInput();
	void gameLoop();
	void drawGame();
	void linkFogShader();
	void linkToon();
	void linkRimLighting();
	void linkGeo();
	void linkEmapping();
	void linkShake();

	void drawSkyBox();
	void drawAsteriods();
	void drawShip();
	void drawMissiles();

	void fireMissiles();

	void initModels(GameObject*& asteroid);
	bool collision(glm::vec3 m1Pos, float m1Rad, glm::vec3 m2Pos, float m2Rad);

	void moveCamera();
	void updateDelta();

	void createScreenQuad();

	void generateFBO(float w, float h);
	void bindFBO();
	void unbindFBO();
	void renderFBO();

	void recordFrameTime(); 
	void layoutGrid();
	void updateTransforms();
	void drawPerObject();
	void drawInstanced();
	void updateCameraBuffer();

	//void playAudio(unsigned int Source, glm::vec3 pos);

	Display _gameDisplay;
	GameState _gameState;
	Mesh rockMesh;
	Mesh shipMesh;
	Mesh missileMesh;
	Mesh ballMesh; 
	Camera myCamera;
	Shader fogShader;
	Shader toonShader;
	Shader rimShader;
	Shader geoShader;
	Shader lightShader;
	Shader shaderSkybox;
	Shader eMapping;
	Shader FBOShader;
	Shader screenShake;
	Shader objectShader; // object.vert + object.frag
	Shader instancedShader; // instanced.vert + object.frag

	Transform transform;
	Transform ballTransform;

	GameObject* asteroid = new GameObject[20];
	GameObject* missiles = new GameObject[20];
	GameObject ship;

	Texture texture;
	Texture testTexture;

	GLuint FBO;
	GLuint RBO;
	GLuint CBO;
	GLuint cameraUBO;
	GLuint lightSSBO;
	GLuint modelSSBO; // task 2
	GLuint gpuTimer[2]; // timing

	GLint modelLocation; // task 1

	double updateMsTotal, submitMsTotal, gpuMsTotal, frameMsTotal;

	struct Light
	{
		glm::vec3 position;    // offset 0
		float radius;          // offset 12
		glm::vec3 colour;      // offset 16
		float intensity;       // offset 28
	};
	static_assert(sizeof(Light) == 32, "Light does not match std430");

	//struct DrawElementsIndirectCommand
	//{
	//	GLuint count; // indices in the mesh
	//	GLuint instanceCount; // how many copies
	//	GLuint firstIndex; // 0 for you
	//	GLuint baseVertex; // 0 for you
	//	GLuint baseInstance; // 0 for you
	//};

	//DrawElementsIndirectCommand drawElements;

	std::vector<Light> lights;
	std::vector<glm::vec3> positions; // one array per field, index = object
	std::vector<glm::mat4> transforms; // rebuilt every frame

	int lightCount;
	int mode; // 1 = per object, 2 = instanced
	int objectCount;
	int framesTimed;

	glm::vec3 currentCamPos;

	Skybox skybox;

	vector<std::string> faces;

	//Audio audioDevice;
	bool look = true;
	
	unsigned int whistle;
	unsigned int backGroundMusic;
	unsigned int quadVAO, quadVBO;
	unsigned int frameIndex;

	Uint64 NOW = SDL_GetPerformanceCounter();
	Uint64 LAST = 0;
	Uint64 frameStart = SDL_GetPerformanceCounter();

	float deltaTime = 0;
	float counter = 0.0f;
};

