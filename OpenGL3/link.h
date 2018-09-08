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
	vec2 uvOffset[95];
	vec2 uvScale[95];
	vec2 quadScale[95];
	float alignment[95];
	float tallestLetter = 0;
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
	mat4 transform;

	vec4 color;
	vec4 borderColor;
	
	float borderW;
	float rotSpeed;
	int sides, justification;
	bool hidden;

	//temp
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
	int fd; //fond data

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
	//void loadFont();

	//rect * addEllipse(rect e);

	int resolutionWidth;
	int resolutionHeight;
	float prevTime;
	float currentFPS;
	float LastRenderTime;
	const char * title = "Running from Native C++    ";
	bool titleDetails = true; //toggle for fps, render time, and shape count
	bool closeFlag = false; //for indicating that a clean up happened, and to indicate one should happen
	bool Cleaned = false; //to prevent multiple cleanups
	bool clearColorFlag = false; //resets the back buffer in the next from if true
	
	int RectShaderProgram, CircleShaderProgram, PolygonShaderProgram, TextureShaderProgram;
	
	vector<rect*> rectBuffer;
	vector<extraData> eDataBuffer;

	vector<rect*> rects;

	//rects to be drawn to the buffer (cleared evert frame)
	vector<rect> BBQue;
	vector<extraData> eData;
	//vector<GLuint> ids;
	vector<fontDat> fonts;

	vec4 backCol;

	GLuint fboId;
	GLuint VBO, VAO, EBO;
	GLuint texture;
	GLuint textureId;

	GLuint RxformUniformLocation;
	GLuint RColorUniformLocation;
	GLuint RaspectUniformLocation;
	GLuint RshapeScaleUniformLocation;

	GLuint ExformUniformLocation;
	GLuint EshapeScaleUniformLocation;
	GLuint EColorUniformLocation;
	GLuint EaspectUniformLocation;
	GLuint EbordWidthUniformLocation;

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

	GLuint TxformUniformLocation;
	GLuint TColorUniformLocation;
	GLuint TaspectUniformLocation;
	GLuint TshapeScaleUniformLocation;
	GLuint TtextureScaleUniformLocation;
	GLuint TUVscaleUniformLocation;
	GLuint TUVposUniformLocation;

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
