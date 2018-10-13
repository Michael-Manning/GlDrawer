#pragma once
#include <GLFW/glfw3.h>
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>
#include <Box2D/Box2D.h>
#include <vector>

using namespace std;
using namespace glm;

#define TEXTFILE 256;

struct fontAsset {
	unsigned char bitmapBuffer[512 * 512];
	vec2 uvOffset[95]; //location of the letter within the generated bitmap
	vec2 uvScale[95]; //size ofthe letter within the bitmap
	vec2 quadScale[95]; //size the letter should be displayed
	float alignment[95]; //vertical offset of each letter
	float tallestLetter = 0; //tallest letter in pixels
	float alignmentOffset = 0; //pixels in which the tallest letter sets below the alignemt. Required for precise global positioning
	float spaceOff;
	GLuint id;
	bool init;
	const char * filePath;

	fontAsset(const char * filepath);
	fontAsset() { init = false; }
	void loadTexture();
	void dipose();
};

//while images are small, the same image should be prevented from be loaded multiple times into the same OpnGL context
struct imgAsset {
	const char * filePath;
	GLuint ID;
	bool init;

	imgAsset(const char * filepath);
	imgAsset() { init = false; }
};

class GO;
struct textData {
	string text;
	const char * filepath;
	vec4 color;
	float height;
	int justification;
	bool boundMode;

	int TextLength = 0;
	float * letterTransData;
	float * letterUVData;
	int hashIndex = -1; //privately accessd

	textData(string Text, float textHeight, vec4 Color, int Justification, const char * path, bool bound = false );
	textData() {};
	int getHashIndex();
	void dispose();
};

struct imgData {
	const char * filepath;
	int hashIndex = -1; //privately accessd
	vec4 tint;

	//will include settings for stretch to fit, tilling .ect
	imgData(const char* path, vec4 tint = vec4(0));
	imgData() { tint = vec4(0); }
	int getHashIndex();
};

struct polyData {
	vec4 fColor;
	vec4 bColor;
	
	float bWidth;
	int sides;

	polyData(vec4 fCol = vec4(1), vec4 bCol = vec4(0), float BWidth = 0, int Sides = 4);
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

//everything needs a default with C++/CLI
class ParticleSystem {
public:
	//data
	Particle * container;
	float * positionSizeData;
	unsigned char * colorData;
	float UVScale = 0;
	float * UVData; //per particle UVs
	vec2 * UVS; //UVs of the sprite sheet
	int count = 0;

	//settings
	int MaxParticles = 0;
	vec2 spawnLocation;	
	float startSize = 0, endSize = 0, lifeLength = 0, lifePrecision = 0;
	bool burstMode = false, continuous = true;
	vec4 startCol, endCol;
	float spread = 0, angle = 0, speed = 0, speedPrecision = 0;
	int spawnRate = 0; //#new particles per frame in continuous
	float internalDelta = 0; //smooths out spawn rates, especially at high framerates
	vec2 gravity;
	vec2 extraStartVelocity;
	float radius = 0;
	bool relitivePosition = true;
	bool drawBehindShape = true;

	ParticleSystem(int maxCount, float LifeLength);
	ParticleSystem() {};
	const char * filepath; //used by parent GO imgdata
	void setTexture(const char * Texture);
	void setAnimation(const char * Texture, int resolution, int tilesPerLine);
	void updateParticles(float delta, float extraAngle); //angle to match gameobject
	bool disposed = true;
	void dispose(){
		if (disposed)
			return;
		disposed = true;
		delete container;
		delete positionSizeData;
		delete colorData;
		delete UVData;
		if(textureMode)
			delete UVS;
	}

	imgData img;
	bool textureMode = false;
	bool tileMode = false;
	bool dead = false;
	int tileCount;
	int resolution;
	bool burstTrigger = true;
};

const float phScale = 100; //pixel/unit ratio for physics

bool testCirc(vec2 circPos, float rad, vec2 test);
bool testRect(vec2 rectPos, vec2 scale, float angle, vec2 Test);

class rigBody {
public:
	GO * link;
	b2World * world;
	b2Body * body;
	b2FixtureDef fixtureDef;
	bool kinematic = false;

	void addForce(vec2 force);
	void addTorque(float torque);
	void setVelocity(vec2 velocity);
	void lockRotation();
	vec2 GetVelocity();
	rigBody(b2World * World, GO * Link, int type, float friction = 0.8f, bool Kinimatic = false);
};

//GameObject
class GO {
public:
	GO * parent;
	polyData * p;
	imgData * i;
	textData * t;
	ParticleSystem * ps;

	rigBody * body;

	vec2 position;
	vec2 scale;
	float angle;
	float rSpeed = 0;
	int drawIndex = 0;
	bool hidden = false;

	GO(vec2 pos = vec2(0), vec2 Scale = vec2(0), float Angle = 0, float rotationSpeed = 0);
	GO(polyData * poly, vec2 pos = vec2(0), vec2 Scale = vec2(0), float Angle = 0, float rotationSpeed = 0);
	GO(imgData * img, vec2 pos = vec2(0), vec2 Scale = vec2(0), float Angle = 0, float rotationSpeed = 0);
	GO(textData * text, vec2 pos = vec2(0), vec2 Scale = vec2(0), float Angle = 0, float rotationSpeed = 0);
	GO(ParticleSystem * part, vec2 pos = vec2(0), vec2 Scale = vec2(0), float Angle = 0, float rotationSpeed = 0);
};


extern vector<const char *> fontHashLookup;
extern vector<const char *> imgHashLookup;
extern b2BodyDef bodyDef; //for physics

//for input rollover and events
struct description {
	int btn, action, read;
	bool operator==(const description& b) {
		return (action == b.action && btn == b.btn);
	}
};

class GLCanvas {
public :
	GLFWwindow * window;

	//input manager stuff
	void onKeyboard(int key, int scancode, int action, int mods);
	void onMouse(int button, int action, int mods);
	void onCursor();
	description keyBuffer[10];
	description mouseBuffer[10];

	bool mouseMoveFlag = false;

	int keyStates[348];
	int LeftMouseState, RightMouseState;
	void clearStates();

	bool getKey(char key);
	bool getKeyDown(char key);
	bool getKeyUp(char key);

	bool getKey(int key);
	bool getKeyDown(int key);
	bool getKeyUp(int key);

	vec2 getMousePos();
	bool getMouse(int button);
	bool getMouseDown(int button);
	bool getMouseUp(int button);


	//backend only
	const int expectedTextLength = 200; //number of letters to preallocated VRAM (can handle larger at runtime)
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
	textData infoText;//used to display debug info on the canvas
	GO infoGO; //used to display debug info on the canvas
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
	bool disposed = false; //to prevent multiple cleanups
	bool clearColorFlag = true; //resets the back buffer in the next from if true
	bool debugMode = false;
	void setWindowSize(int w, int h);
	int resolutionWidth;
	int resolutionHeight;
	bool clearShapeFlag = false;
	bool windowTitleFlag = true; //setting the title takes insanely long, so it should only be updated if there's a change
	bool centerOffset = false;// 0,0 is bottom left of the string
	vec2 camera;
	int ParticleLimit = 10000; //max particles per system. Required for memory allocation
	vec4 backCol;

	//backend only
	int loadShader(const char * vertexFilename, const char * fragmentFilename); //part of the canvas class to link the packed shaders boolean
	void setFont(GO * g);
	void setTexture(GO *, GLuint textureLocation);
	void setPolygon(GO * g); 
	void loadImageAsset(const char * filepath); //like setTexture, but only loads the image to memory for later use
	void drawParticleSystem(GO * g, float deltaTime, mat4 *global = NULL);
	void clearSetPixelData();
	void setGOTransform(GO * s, GLuint aspect, GLuint scale, GLuint pos, GLuint rot);
	void LocalTransformHelper(GO * child, mat4 * m);
	mat4 makeLocalTransform(GO * child);
	void drawGameobjectShape(GO * g); //automatically applies localized transformations
	GLCanvas();

	//used by back/middle end
	void setPos(int x, int y);
	int createCanvas(int width, int height, bool borderd, vec3 backCol, bool Vsync, bool cursorHidden = false);
	void addGO(GO* g);
	void setBBPixel(int x, int y, vec4 col);
	void setBBShape(GO g); 
	void dispose();
	vec3 getPixel(int x, int y);
	void updateDebugInfo();
	HWND getNativeHWND();
	void focus();
	void mainloop(bool render = true); //steps the program forward (and maybe renders the scene)

	//physics
	b2World world = b2World(b2Vec2(0, -10)); //world with default gravity
	int32 velocityIterations = 50; //6
	int32 positionIterations = 20; //3
	float32 timeStep = 1.0f / 60.0f;

	vector<GO*> GameObjects;
	vector<GO*> GOBuffer;
	vector<GO*> GORemoveBuffer;
	vector<GO> BBQue;

	vector<const char *> assetBuffer;
	vector<fontAsset> fonts;
	vector<imgAsset> imgs;
	unsigned char* setPixelData;
	GLuint setPixelDataID;

	int PolygonShaderProgram, textureShaderProgram, fontShaderProgram, ParticleShaderProgram;
	GLuint VBO, VAO, EBO;
	GLuint PPosVBO, PColVBO, PVertVBO, PuvVBO; //for particle systems
	GLuint FTranVBO, FuvVBO; //for text
	GLuint texture;
	GLuint fboIdA;
	GLuint fboTextIdA;
	GLuint fboIdB;
	GLuint fboTextIdB;

	//polygon shader uniforms
	GLuint PxformUniformLocation;
	GLuint PColorUniformLocation;
	GLuint PbordColorUniformLocation;
	GLuint PsideCountUniformLocation;
	GLuint PshapeScaleUniformLocation;
	GLuint PborderWidthUniformLocation;
	//GLuint PtextureUniformLocation;
	GLuint PtimeUniformLocation;

	GLuint PmPosUniformLocation;
	GLuint PmScaleUniformLocation;
	GLuint PmRotUniformLocation;
	GLuint PUVscaleUniformLocation;
	GLuint PUVposUniformLocation;
	GLuint PaspectUniformLocation;

	//texture shader uniforms
	GLuint FtextureUniformLocation;
	GLuint FColorUniformLocation; //tinting

	GLuint FxformUniformLocation;
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
	GLuint FontxformUniformLocation;

	//particle shader uniforms
	GLuint ParticlePosUniformLocation;
	GLuint ParticleResUniformLocation; //particle systems themselves don't have accsess to the canvas resolution
	GLuint ParticleTextureUniformLocation;
	GLuint ParticleUVScaleUniformLocation;
	GLuint ParticlexformUniformLocation;
};
