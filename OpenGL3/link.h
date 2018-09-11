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

struct extraData {
	GLuint id; 	//sprite texture only
	int fd; //font data

	extraData() {
		id = -1;
		fd = NULL;
	}
};

class BaseGLD {
public :
	GLFWwindow * window;
	InputManager * Input;

	int createCanvas(int width, int height, bool borderd, vec3 backCol, bool Vsync = true);
	void setPos(int x, int y);
	void setVisible(bool visible);
	bool usePackedShaders = false;
	void focus();
	int loadShader(const char * vertexFilename, const char * fragmentFilename); //part of the canvas class to link the packed shaders boolean
	HWND getNativeHWND();
	void drawFont(rect * r, int index);

	int resolutionWidth;
	int resolutionHeight;
	float aspect; //canvas aspect ratio
	vec2 resolutionV2f; //resolution as a vecc2
	vec2 prevWindowSize; //used to detect window resizing
	bool windowSizeChanged = false;
	float currTime; //latest GLFW time response
	float prevTime;
	float currentFPS;
	float LastRenderTime;
	const char * title = "Running from Native C++    ";
	bool titleDetails = true; //toggle for fps, render time, and shape count
	bool closeFlag = false; //for indicating that a clean up happened, and to indicate one should happen
	bool Cleaned = false; //to prevent multiple cleanups
	bool clearColorFlag = false; //resets the back buffer in the next from if true
	bool debugMode = false;
	rect infoRect; //used as a font to display debug info on the canvas
	float debugTimer; //used to time frequency of debug information updates
	float debugUpdateFreq = 0.2; //how many seconds to wait between info updates
	void updateDebugInfo();
	vec3 getPixel(int x, int y);
	std::string debugString;
	
	int RectShaderProgram, CircleShaderProgram, PolygonShaderProgram, TextureShaderProgram;
	
	vector<rect*> rectBuffer;
	vector<extraData> eDataBuffer;
	vector<rect*> removalBuffer;

	vector<rect*> rects;

	//rects to be drawn to the buffer (cleared evert frame)
	vector<rect> BBQue;
	vector<extraData> eData;
	vector<fontDat> fonts;

	vec4 backCol;

	GLuint fboId;
	GLuint VBO, VAO, EBO;
	GLuint texture;
	GLuint textureId;

	GLuint PxformUniformLocation;
	GLuint PshapeScaleUniformLocation;
	GLuint PColorUniformLocation;
	GLuint PbordColorUniformLocation;
	GLuint PsideCountUniformLocation;
	GLuint PborderWidthUniformLocation;
	GLuint PaspectUniformLocation;
	GLuint PtextureUniformLocation;
	GLuint PUVscaleUniformLocation;
	GLuint PUVposUniformLocation;
	GLuint PtimeUniformLocation;

	GLuint PmPosUniformLocation;
	GLuint PmScaleUniformLocation;
	GLuint PmRotUniformLocation;


	void addRect(rect * r);
	void setBBPixel(vec2 pixel, vec4 col);
	void removeNullShapes();
	void swapOrder(int indexA, int indexB);
	void cleanup();
	void removeRect(rect * r);

	void mainloop();
	void checkFont(extraData * ed, rect r) {
		if (ed->fd)
			return;
		for (int i = 0; i < fonts.size(); i++)
		{
			if (fonts[i].filePath == r.path) {
				ed->fd = i;
				return;
			}				
		}

		fonts.push_back(fontDat(r.path));
		fonts[fonts.size() - 1].loadTexture();
		ed->fd = fonts.size()-1;
	}
};
