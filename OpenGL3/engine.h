#pragma once
#include <GLFW/glfw3.h>
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>
#include "Input.h"
#include <vector>

using namespace std;
using namespace glm;

#define TEXTFILE 256;

struct fontDat {
	unsigned char bitmapBuffer[512 * 512];
	vec2 uvOffset[95]; //location of the letter within the generated bitmap
	vec2 uvScale[95]; //size ofthe letter within the bitmap
	vec2 quadScale[95]; //size the letter should be displayed
	float alignment[95]; //vertical offset of each letter
	float tallestLetter = 0; //tallest letter in pixels
	float alignmentOffset = 0; //pixels in which the tallest letter sets below the alignemt. Required for precise global positioning
	float spaceOff;
	int justification;
	GLuint id;
	const char * filePath;
	fontDat(const char * filepath);
	void loadTexture();
};

//while images are small, the same image should be prevented from be loaded multiple times into the same context
struct imgDat {
	const char * filePath;
	GLuint ID;
	imgDat(const char * filepath);
};

struct extraData {
	int id; 	//sprite texture only
	int fd; //font data

	extraData() {
		id = -1;
		fd = -1;
	}
};

//anything that can be drawn is a shape
struct shape {
public:
	vec2 pos;
	vec2 scale;
	float angle;

	vec4 color;
	vec4 borderColor;
	
	float borderW;
	float rotSpeed;
	int sides, justification;
	bool hidden;

	//to be offloaded:
	const char* path;
	string text;
	int textLength;
	shape * bound = NULL;

	shape(vec2 Pos, vec2 Scale, float Angle, vec4 Color, vec4 BorderCol = vec4(), float bordW = 0, float RotationSpeed = 0, int Sides = 4);
	//As a texture
	shape(const char* filePath, vec2 Pos, vec2 Scale, float Angle, vec4 Color, vec4 BorderCol = vec4(), float bordW = 0, float RotationSpeed = 0);
	//As a font
	shape(const char* filePath, string text, int textLength, vec2 Pos, float Scale, int Tjustification = 0, shape * bound = NULL, float Angle = 0, vec4 Color = vec4(), vec4 BorderCol = vec4(), float bordW = 0, float RotationSpeed = 0);
	
	shape();
};


struct Particle {
	vec2 pos, vel;
	unsigned char r, g, b, a; // Color
	float size;
	float life; 
	Particle(vec2 Pos, float x, float y, float Life, float startSize, vec4 startCol) {
		pos = Pos;
		vel = vec2(x, y);
		size = startSize;
		life = Life;
		r = (int)startCol.r * 255;
		g = (int)startCol.g * 255;
		b = (int)startCol.b * 255;
		a = (int)startCol.a * 255;
	}
	Particle() {
		life = 1;
	}
};

float RandomFloat(float a, float b);

class ParticleSystem {
public:
	int count;
	int MaxParticles;
	Particle * container;
	vec2 spawnLocation;
	float * positionSizeData;
	unsigned char * colorData;
	float startSize, endSize, lifeLength, lifePrecision;
	bool burstMode, continuous;
	vec4 startCol, endCol;
	float spread, angle, speed, speedPrecision;
	int spawnRate; //#new particles per frame in continuous
	float internalDelta; //smooths out spawn rates, especially at high framerates
	vec2 gravity;
	vec2 extraStartVelocity;
	ParticleSystem(int maxCount, float LifeLength);
	ParticleSystem() {};
	void updateParticles(float delta);
	void dispose() {
		delete container;
		delete positionSizeData;
		delete colorData;
	}
};

//GameObject
class GO {
public:
	shape * s;
	extraData edata; //Gameobjects cannot have references to more than one canvas anyways
	vec2 position;
	float angle;
	ParticleSystem * ps;
	GO(shape * S, vec2 pos = vec2(0), float Angle = 0) {
		s = S;
		position = pos;
		angle = Angle;
		ps = NULL;
	}
};


class GLCanvas {
public :
	GLFWwindow * window;
	InputManager * Input;

	//backend only
	void setVisible(bool visible);
	bool usePackedShaders = false;
	float aspect; //canvas aspect ratio
	vec2 resolutionV2f; //resolution as a vecc2
	//vec2 prevWindowSize; //used to detect window resizing
	bool windowSizeChanged = false;
	float currTime; //latest GLFW time response
	float prevTime;
	float currentFPS;
	float LastRenderTime;
	shape infoShape; //used as a font to display debug info on the canvas
	extraData infoData; //font data for the info shape must be kept sepreate to prevent vector errors
	float debugTimer = 0; //used to time frequency of debug information updates
	float debugUpdateFreq = 0.2; //how many seconds to wait between info updates
	std::string debugString;
	bool setPixelFlag = false; //used to determin if work needs to be done based on wether a pixel was set since the previous frame
	bool setPixelCopyFlag = false;//used to pause the main thread if it tries to write setpixel data while being coppied
	bool shapeCopyFlag = false; //main thread interupt to prevent loss while coppying the shape buffer for shapes added that frame

	//used by back/middle end
	const char * title = "Running from Native C++    ";
	bool titleDetails = true; //toggle for fps, render time, and shape count
	bool closeFlag = false; //for indicating that a clean up happened, and to indicate one should happen
	bool Cleaned = false; //to prevent multiple cleanups
	bool clearColorFlag = true; //resets the back buffer in the next from if true
	bool debugMode = false;
	void setWindowSize(int w, int h);
	int resolutionWidth;
	int resolutionHeight;
	bool clearShapeFlag = false;
	bool windowTitleFlag = true; //setting the title takes insanely long, so it should only be updated if there's a change
	vec2 camera;
	int ParticleLimit = 10000; //max particles per system. Required for memory allocation
	vec4 backCol;

	//backend only
	int loadShader(const char * vertexFilename, const char * fragmentFilename); //part of the canvas class to link the packed shaders boolean
	void drawFont(shape * r, extraData * fontData);
	void drawTexture(shape * r, extraData * fontData);
	void drawParticleSystem(GO * g, float deltaTime);
	void clearSetPixelData();
	void setPolygonUniforms(shape * r); //saves lots of copy pasting
	void setGOUniforms(GO * s); 
	GLCanvas() {};

	//used by back/middle end
	void setPos(int x, int y);
	int createCanvas(int width, int height, bool borderd, vec3 backCol, bool Vsync);
	void addShape(shape * r);
	void addGO(GO* g);
	void setBBPixel(int x, int y, vec4 col);
	void setBBShape(shape r); //currently only accepting ellipses and shapeangles
	void removeNullShapes();
	void swapOrder(int indexA, int indexB);
	int getDrawIndex(shape * r);
	void cleanup();
	void removeShape(shape * r);
	vec3 getPixel(int x, int y);
	void updateDebugInfo();
	HWND getNativeHWND();
	void focus();
	void mainloop(bool render = true); //steps the program forward (and maybe renders the scene)
	
	vector<shape*> shapeBuffer;
	vector<extraData> eDataBuffer;
	vector<shape*> removalBuffer;
	vector<shape*> shapes;

	vector<GO*> GameObjects;
	vector<GO*> GOBuffer;
	vector<GO*> GORemoveBuffer;

	vector<shape> BBQue;
	vector<extraData> BBEdataQue;
	vector<extraData> eData;
	vector<fontDat> fonts;
	vector<imgDat> imgs;
	unsigned char* setPixelData;
	GLuint setPixelDataID;

	int PolygonShaderProgram, fboShaderProgram, fontShaderProgram, ParticleShaderProgram;
	GLuint VBO, VAO, EBO;
	GLuint PPosVBO, PColVBO, PVertVBO; //for particle systems
	GLuint texture;
	GLuint fboIdA;
	GLuint fboTextIdA;
	GLuint fboIdB;
	GLuint fboTextIdB;

	//polygon shader uniforms
	GLuint PxformUniformLocation;
	GLuint PshapeScaleUniformLocation;
	GLuint PColorUniformLocation;
	GLuint PbordColorUniformLocation;
	GLuint PsideCountUniformLocation;
	GLuint PborderWidthUniformLocation;
	GLuint PtextureUniformLocation;
	GLuint PtimeUniformLocation;

	GLuint PmPosUniformLocation;
	GLuint PmScaleUniformLocation;
	GLuint PmRotUniformLocation;
	GLuint PUVscaleUniformLocation;
	GLuint PUVposUniformLocation;
	GLuint PaspectUniformLocation;

	//fbo shader uniforms
	GLuint FtextureUniformLocation;

	GLuint FmPosUniformLocation;
	GLuint FmScaleUniformLocation;
	GLuint FmRotUniformLocation;
	GLuint FUVscaleUniformLocation;
	GLuint FUVposUniformLocation;
	GLuint FaspectUniformLocation;

	//font shader uniforms
	GLuint FonttextureUniformLocation;
	GLuint FontColorUniformLocation;
	GLuint FonttimeUniformLocation;

	GLuint FontmPosUniformLocation;
	GLuint FontmScaleUniformLocation;
	GLuint FontmRotUniformLocation;
	GLuint FontUVscaleUniformLocation;
	GLuint FontUVposUniformLocation;
	GLuint FontaspectUniformLocation;

	//particle shader uniforms
	GLuint ParticlePosUniformLocation;
	GLuint ParticleResUniformLocation; //particle systems themselves don't have accsess to the canvas resolution
};
