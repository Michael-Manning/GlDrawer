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
		fd = NULL;
	}
};

struct rect {
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
	rect * bound = NULL;

	rect(vec2 Pos, vec2 Scale, float Angle, vec4 Color, vec4 BorderCol = vec4(), float bordW = 0, float RotationSpeed = 0, int Sides = 4);
	//As a texture
	rect(const char* filePath, vec2 Pos, vec2 Scale, float Angle, vec4 Color, vec4 BorderCol = vec4(), float bordW = 0, float RotationSpeed = 0);
	//As a font
	rect(const char* filePath, string text, int textLength, vec2 Pos, float Scale, int Tjustification = 0, rect * bound = NULL, float Angle = 0, vec4 Color = vec4(), vec4 BorderCol = vec4(), float bordW = 0, float RotationSpeed = 0);
	
	rect();
};

class BaseGLD {
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
	rect infoRect; //used as a font to display debug info on the canvas
	extraData infoData; //font data for the info rect must be kept sepreate to prevent vector errors
	float debugTimer = 0; //used to time frequency of debug information updates
	float debugUpdateFreq = 0.2; //how many seconds to wait between info updates
	std::string debugString;
	bool setPixelFlag = false; //used to determin if work needs to be done based on wether a pixel was set since the previous frame
	bool setPixelCopyFlag = false;//used to pause the main thread if it tries to write setpixel data while being coppied
	bool rectCopyFlag = false; //main thread interupt to prevent loss while coppying the rect buffer for shapes added that frame

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

	//backend only
	int loadShader(const char * vertexFilename, const char * fragmentFilename); //part of the canvas class to link the packed shaders boolean
	void drawFont(rect * r, extraData * fontData);
	void checkFont(extraData * ed, rect r);// checks if a font is already loaaded to canvas memory
	void checkImg(extraData * ed, rect r);// checks if an image is already loaaded to canvas memory
	void clearSetPixelData();
	void setPolygonUniforms(rect * r); //saves lots of copy pasting

	//used by back/middle end
	void setPos(int x, int y);
	int createCanvas(int width, int height, bool borderd, vec3 backCol, bool Vsync);
	void addRect(rect * r);
	void setBBPixel(int x, int y, vec4 col);
	void setBBShape(rect r); //currently only accepting ellipses and rectangles
	void removeNullShapes();
	void swapOrder(int indexA, int indexB);
	int getDrawIndex(rect * r);
	void cleanup();
	void removeRect(rect * r);
	vec3 getPixel(int x, int y);
	void updateDebugInfo();
	HWND getNativeHWND();
	void focus();
	bool loaded(rect * r);//check if the shape is in the draw list
	void mainloop(bool render = true); //steps the program forward (and maybe renders the scene)
	
	vector<rect*> rectBuffer;
	vector<extraData> eDataBuffer;
	vector<rect*> removalBuffer;

	vector<rect*> rects;

	vector<rect> BBQue;
	vector<extraData> eData;
	vector<fontDat> fonts;
	vector<imgDat> imgs;
	unsigned char* setPixelData;
	GLuint setPixelDataID;

	vec4 backCol;

	int PolygonShaderProgram, fboShaderProgram;
	GLuint VBO, VAO, EBO;
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
};
