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
	int sides;
	bool hidden;

	//temp
	const char* path;
	string text;
	int textLength;

	rect(vec2 Pos, vec2 Scale, float Angle, vec4 Color, vec4 BorderCol = vec4(), float bordW = 0, float RotationSpeed = 0,  int Sides = 4) {
		pos = Pos;
		scale = Scale;
		angle = Angle;
		color = Color;
		borderColor = BorderCol;
		borderW = bordW;
		rotSpeed = RotationSpeed;
		sides = Sides;
		hidden = false;
	}
	//As a texture
	rect(const char* filePath, vec2 Pos, vec2 Scale, float Angle, vec4 Color, vec4 BorderCol = vec4(), float bordW = 0, float RotationSpeed = 0);
	//As a font
	rect(const char* filePath, string text, int textLength, vec2 Pos, vec2 Scale, float Angle = 0, vec4 Color = vec4(), vec4 BorderCol = vec4(), float bordW = 0, float RotationSpeed = 0);
};

struct extraData {
	//for sprites only
	GLuint id;

	int fd;

	extraData() {
		id = -1;
		fd = NULL;
	}
};

class BaseGLD {
public :
	GLFWwindow * window;
	InputManager * Input;

	int createCanvas(int width, int height, bool borderd, vec3 backCol);
	void setPos(int x, int y);
	void setVisible(bool visible);
	void focus();
	//void loadFont();

	//rect * addEllipse(rect e);

	int resolutionWidth;
	int resolutionHeight;
	float prevTime;
	float currentFPS;
	float LastRenderTime;
	
	int RectShaderProgram, CircleShaderProgram, PolygonShaderProgram, TextureShaderProgram;
	
	vector<rect*> rects;
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

	GLuint TxformUniformLocation;
	GLuint TColorUniformLocation;
	GLuint TaspectUniformLocation;
	GLuint TshapeScaleUniformLocation;
	GLuint TtextureScaleUniformLocation;

	void addRect(rect * r);

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
