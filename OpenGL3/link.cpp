#define _CRT_SECURE_NO_WARNINGS

#include <stdio.h>
#include <tchar.h>
#include <math.h>
#include <assert.h>

#include <gl3w/gl3w.h>
#include <GL/GLU.h>
#include <GLFW/glfw3.h>

#define GLFW_EXPOSE_NATIVE_WIN32
#include <GLFW/glfw3native.h>

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>
#define STB_IMAGE_IMPLEMENTATION
#include <stb/stb_image.h>
#define STB_TRUETYPE_IMPLEMENTATION 
#include <stb/stb_truetype.h>


#include <iostream>
#include <iomanip>
#include <string>
#include <fstream>
#include <sstream>
#include <vector>
#include "Input.h"
#include "link.h"
#include "shaders.h"

using namespace std;
using namespace glm;


vector<rect> rectSource;

template <typename F> struct _scope_exit_t {
	_scope_exit_t(F f) : f(f) {}
	~_scope_exit_t() { f(); }
	F f;
};
template <typename F> _scope_exit_t<F> _make_scope_exit_t(F f) {
	return _scope_exit_t<F>(f);
};
#define _concat_impl(arg1, arg2) arg1 ## arg2
#define _concat(arg1, arg2) _concat_impl(arg1, arg2)
#define defer(code) \
	auto _concat(scope_exit_, __COUNTER__) = _make_scope_exit_t([=](){ code; })

#ifdef NDEBUG
#define gl(OPENGL_CALL) \
	gl##OPENGL_CALL
#else
#define gl(OPENGL_CALL) \
	gl##OPENGL_CALL; \
	{ \
		int potentialGLError = glGetError(); \
		if (potentialGLError != GL_NO_ERROR) \
		{ \
			fprintf(stderr, "OpenGL Error '%s' (%d) gl" #OPENGL_CALL " " __FILE__ ":%d\n", (const char*)gluErrorString(potentialGLError), potentialGLError, __LINE__); \
			assert(potentialGLError == GL_NO_ERROR && "gl" #OPENGL_CALL); \
		} \
	}
#endif

#define asizei(a) (int)(sizeof(a)/sizeof(a[0]))

#pragma region shapes

const unsigned int rectIndices[] = {  // note that we start from 0!
	0, 3, 1,  // first Triangle
	1, 3, 2   // second Triangle
};

const vec3 rectVertices[] = {
	{ -1.0f,  1.0f,  0.0f, },  // top left
{ 1.0f,  1.0f,  0.0f, },  // top right
{ 1.0f, -1.0f,  0.0f },  // bottom right
{ -1.0f, -1.0f,  0.0f, },  // bottom left
{ -1.0f,  1.0f,  0.0f, },  // top left
};

void BaseGLD::addRect(rect * r) {
	while (rectCopyFlag) {}
	rectBuffer.push_back(r);
	eDataBuffer.push_back(extraData());
}

float RandFloat() {
	return static_cast <float> (rand()) / static_cast <float> (RAND_MAX);
}
vec4 RandColor() {
	return vec4(RandFloat(), RandFloat(), RandFloat(), 1.0f);
}

#pragma endregion

const char * defaultFilepath = "../data/"; //use if the shader folder is outside of the bin directory

int BaseGLD::loadShader(const char * vertexFilename, const char * fragmentFilename) {

	const char * vertexShaderSource;
	const char * fragmentShaderSource;
	string str, str2; //can't be local

	//if the packed shaders are being used, the filename is a pointer the actual packed shader. The variable name should be changed
	if (usePackedShaders) {
		vertexShaderSource = vertexFilename;
		fragmentShaderSource = fragmentFilename;
	}
	else {
		ostringstream sstream;

		ifstream fs((string)defaultFilepath + "Shaders\\" + (string)vertexFilename + ".glsl");
		sstream << fs.rdbuf();
		str =sstream.str();
		vertexShaderSource = str.c_str();

		if (str == "") {
			std::cout << "ERROR: Failed to load vertex shader source \"" << vertexFilename << "\"" << "\n" << std::endl;
		}

		ostringstream sstream2;
		ifstream fs2((string)defaultFilepath + "Shaders\\" + (string)fragmentFilename + ".glsl");
		sstream2 << fs2.rdbuf();
	    str2 = sstream2.str();
		fragmentShaderSource = str2.c_str();

		if (str2 == "") {
			std::cout << "ERROR: Failed to load fragment shader source \"" << fragmentFilename << "\"" << "\n" << std::endl;
		}
	}
	int shaderProgram;

	int vertexShader = gl(CreateShader(GL_VERTEX_SHADER));
	gl(ShaderSource(vertexShader, 1, &vertexShaderSource, NULL));
	gl(CompileShader(vertexShader));

	int success;
	char infoLog[512];
	gl(GetShaderiv(vertexShader, GL_COMPILE_STATUS, &success));
	if (!success)
	{
		gl(GetShaderInfoLog(vertexShader, 512, NULL, infoLog));
		std::cout << "ERROR::SHADER::VERTEX::COMPILATION_FAILED\n" << infoLog << std::endl;
	}

	int fragmentShader = gl(CreateShader(GL_FRAGMENT_SHADER));
	gl(ShaderSource(fragmentShader, 1, &fragmentShaderSource, NULL));
	gl(CompileShader(fragmentShader));

	gl(GetShaderiv(fragmentShader, GL_COMPILE_STATUS, &success));
	if (!success)
	{
		gl(GetShaderInfoLog(fragmentShader, 512, NULL, infoLog));
		std::cout << "ERROR::SHADER::FRAGMENT::COMPILATION_FAILED\n" << infoLog << std::endl;
	}

	shaderProgram = gl(CreateProgram());
	gl(AttachShader(shaderProgram, vertexShader));
	gl(AttachShader(shaderProgram, fragmentShader));
	gl(LinkProgram(shaderProgram));

	gl(GetProgramiv(shaderProgram, GL_LINK_STATUS, &success));
	if (!success) {
		gl(GetProgramInfoLog(shaderProgram, 512, NULL, infoLog));
		std::cout << "ERROR::SHADER::PROGRAM::LINKING_FAILED\n" << infoLog << std::endl;
	}

	gl(DeleteShader(vertexShader));
	gl(DeleteShader(fragmentShader));
	return shaderProgram;
}

void BaseGLD::cleanup() {
	if (Cleaned)
		return;

	delete setPixelData;
	rects.clear();
	eData.clear();
	fonts.clear();
	glfwTerminate();
	Cleaned = true;
}
void BaseGLD::setWindowSize(int w, int h) {
	glfwSetWindowSize(window, w, h);
}

void BaseGLD::swapOrder(int a, int b) {
	{
		rect* temp = rects[a];
		rects[a] = rects[b];
		rects[b] = temp;
	}
	{
		extraData temp = eData[a];
		eData[a] = eData[b];
		eData[b] = temp;
	}

}
int BaseGLD::getDrawIndex(rect * r) {
	for (int i = 0; i < rects.size(); i++)
		if (rects[i] == r)
			return i;
	return -1;
}
void BaseGLD::removeNullShapes() {
	for (int i = 0; i < rects.size(); i++)
	{
		if (rects[i]->color.r < 0 || rects[i]->color.r > 1) {
			rects.erase(rects.begin() + i);
			eData.erase(eData.begin() + i);
		}
	}
}
void BaseGLD::setPos(int x, int y) 
{
	glfwSetWindowPos(window, x, y);
}
void BaseGLD::setVisible(bool visible) {
	if (!visible)
		glfwHideWindow(window);
	else
		glfwShowWindow(window);
}
void BaseGLD::focus() {
	if(window)
		glfwFocusWindow(window);
}
void BaseGLD::removeRect(rect * r) {
	removalBuffer.push_back(r);
}
void KeyCallback(GLFWwindow* window, int key, int scancode, int action, int mods) {
	static_cast<BaseGLD*>(glfwGetWindowUserPointer(window))->Input->onKeyboard(key, scancode, action, mods);
}
void mouseButtonCallback(GLFWwindow* window, int button, int action, int mods) {
	static_cast<BaseGLD*>(glfwGetWindowUserPointer(window))->Input->onMouse(button, action, mods);
}
void mouseMoveCallback(GLFWwindow* window, double x, double y) {
	static_cast<BaseGLD*>(glfwGetWindowUserPointer(window))->Input->onCursor();
}
void scroll_callback(GLFWwindow* window, double xoffset, double yoffset)
{
	static_cast<BaseGLD*>(glfwGetWindowUserPointer(window))->Input->onMouse(3, (int)yoffset, 0);
}
//unexpected resize + set pixel = death
void window_size_callback(GLFWwindow* window, int width, int height) {
	BaseGLD * base = static_cast<BaseGLD*>(glfwGetWindowUserPointer(window));
	base->windowSizeChanged = true;
}
rect::rect() {
	pos = vec2();
	scale = vec2(100);
	angle = 0;
	color = vec4();
	borderColor = vec4();
	borderW = 0;
	rotSpeed = 0;
	sides = 4;
	hidden = false;
}
//as polygon
rect::rect(vec2 Pos, vec2 Scale, float Angle, vec4 Color, vec4 BorderCol, float bordW, float RotationSpeed, int Sides) {
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
//as sprite
rect::rect(const char* filePath, vec2 Pos, vec2 Scale, float Angle, vec4 Color, vec4 BorderCol, float bordW, float RotationSpeed) {
	pos = Pos;
	scale = Scale;
	angle = Angle;
	color = Color;
	borderColor = BorderCol;
	borderW = bordW;
	rotSpeed = RotationSpeed;
	sides = 0;
	path = filePath;
	hidden = false;
}
//as text
rect::rect(const char* filePath, string Text, int length, vec2 Pos, float Scale, int Tjustification, rect * Bound, float Angle, vec4 Color, vec4 BorderCol, float bordW, float RotationSpeed) {
	pos = Pos;
	scale = vec2(Scale);
	angle = Angle;
	color = Color;
	borderColor = BorderCol;
	borderW = bordW;
	rotSpeed = RotationSpeed;
	path = filePath;
	text = Text;
	textLength = length;
	justification = Tjustification;
    bound = Bound;
	sides = -1;
	hidden = false;
}
HWND BaseGLD::getNativeHWND() {
	return glfwGetWin32Window(window);
}

void genFramBuffer(GLuint * textID, GLuint * fboID, int width, int height) {
	glGenTextures(1, textID);
	glGenFramebuffers(1, fboID);
	{
		gl(BindTexture(GL_TEXTURE_2D, *textID));
		defer(glBindTexture(GL_TEXTURE_2D, 0));
		gl(TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR));
		gl(TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR));
		gl(TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE));
		gl(TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE));
		gl(TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, 0));

		gl(BindFramebuffer(GL_FRAMEBUFFER, *fboID));
		defer(glBindFramebuffer(GL_FRAMEBUFFER, 0));

		// attach the texture to FBO color attachment point
		gl(FramebufferTexture2D(GL_FRAMEBUFFER,        // 1. fbo target: GL_FRAMEBUFFER 
			GL_COLOR_ATTACHMENT0,  // 2. attachment point
			GL_TEXTURE_2D,         // 3. tex target: GL_TEXTURE_2D
			*textID,             // 4. tex ID
			0));                    // 5. mipmap level: 0(base)

								   // check FBO status
		GLenum status = gl(CheckFramebufferStatus(GL_FRAMEBUFFER));
		assert(status == GL_FRAMEBUFFER_COMPLETE);

		gl(BindTexture(GL_TEXTURE_2D, *textID));
		gl(TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, 0));
		gl(BindTexture(GL_TEXTURE_2D, 0));

		gl(BindFramebuffer(GL_FRAMEBUFFER, *fboID));

		gl(ClearColor(0.0f, 0.0f, 0.0f, 1.0f));
		gl(Clear(GL_COLOR_BUFFER_BIT));
	}
}

int BaseGLD::createCanvas(int width, int height, bool borderd, vec3 backcol, bool Vsync)
{
	resolutionWidth = width;
	resolutionHeight = height;
	
	backCol = vec4(backcol, 1);
	int glfwInitResult = glfwInit();
	if (glfwInitResult != GLFW_TRUE)
	{
		fprintf(stderr, "glfwInit returned false\n");
		 return 1;
	}
	

	glfwWindowHint(GLFW_DECORATED, borderd);
	glfwWindowHint(GLFW_FLOATING, !borderd);
	glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
	glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 2);
	glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);
	glfwWindowHint(GLFW_OPENGL_FORWARD_COMPAT, GL_TRUE);

	window = glfwCreateWindow(resolutionWidth, resolutionHeight, "demo", NULL, NULL);
	
	if (!window)
	{
		fprintf(stderr, "failed to open glfw window. is opengl 3.2 supported?\n");
		return 1;
	}

	//*Input = InputManager();
	Input->window = window;
	glfwPollEvents();
	glfwMakeContextCurrent(window);

	int gl3wInitResult = gl3wInit();
	//return 1;
	if (gl3wInitResult != 0)
	{
		fprintf(stderr, "gl3wInit returned error code %d\n", gl3wInitResult);
		return 1;
	}
	
	//only usefull during c++ degug?
	glfwSetWindowUserPointer(window, this);
	glfwSetKeyCallback(window, KeyCallback);
	glfwSetMouseButtonCallback(window, mouseButtonCallback);
	glfwSetCursorPosCallback(window, mouseMoveCallback);
	glfwSetWindowSizeCallback(window, window_size_callback);
	glfwSetScrollCallback(window, scroll_callback);

	//If the program is being run from the CLR program, packed shaders will be turned on. 
	//the sources are from shaders.h which is automatically generated from the original .glsl files
	if (usePackedShaders) {
		PolygonShaderProgram = loadShader(RectVert, PolygonFrag);
		fboShaderProgram = loadShader(RectVert, TextureFrag);
	}
	else {
		PolygonShaderProgram = loadShader("RectVertex", "PolygonFragment");
		fboShaderProgram = loadShader("RectVertex", "TextureFragment");
	}

	gl(GenVertexArrays(1, &VAO));
	gl(GenBuffers(1, &VBO));
	gl(GenBuffers(1, &EBO));

	gl(BindVertexArray(VAO));

	gl(BindBuffer(GL_ARRAY_BUFFER, VBO));
	gl(BufferData(GL_ARRAY_BUFFER, sizeof(rectVertices), rectVertices, GL_STATIC_DRAW));

	gl(BindBuffer(GL_ELEMENT_ARRAY_BUFFER, EBO));
	gl(BufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(rectIndices), rectIndices, GL_STATIC_DRAW));

	gl(VertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, 3 * sizeof(float), (void*)(sizeof(float) * 3)));
	gl(EnableVertexAttribArray(0));

	gl(BindBuffer(GL_ARRAY_BUFFER, 0));
	gl(BindVertexArray(0));


	gl(Enable(GL_BLEND));
	gl(BlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA));

	//polygon shader uniforms
	PxformUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "xform"));
	PshapeScaleUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "shapeScale"));
	PColorUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "Color"));
	PbordColorUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "bordColor"));
	PsideCountUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "sideCount"));
	PborderWidthUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "bordWidth"));
	PtextureUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "Text"));
	PtimeUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "iTime"));

	PmPosUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "position"));
	PmScaleUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "scale"));
	PmRotUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "rotation"));
	PUVscaleUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "scaleOffset"));
	PUVposUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "posOffset"));
	PaspectUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "aspect"));

	//fbo shader uniforms
	FtextureUniformLocation = gl(GetUniformLocation(fboShaderProgram, "Text"));

	FmPosUniformLocation = gl(GetUniformLocation(fboShaderProgram, "position"));
	FmScaleUniformLocation = gl(GetUniformLocation(fboShaderProgram, "scale"));
	FmRotUniformLocation = gl(GetUniformLocation(fboShaderProgram, "rotation"));
	FUVscaleUniformLocation = gl(GetUniformLocation(fboShaderProgram, "scaleOffset"));
	FUVposUniformLocation = gl(GetUniformLocation(fboShaderProgram, "posOffset"));
	FaspectUniformLocation = gl(GetUniformLocation(fboShaderProgram, "aspect"));

	if(!Vsync)
		glfwSwapInterval(0);
	else
		glfwSwapInterval(1);

	genFramBuffer(&fboTextIdA, &fboIdA, width, height);
	genFramBuffer(&fboTextIdB, &fboIdB, width, height);

	//used to display debug information on the canvas.
	if (debugMode) {
		infoRect = rect("c:\\windows\\fonts\\arial.ttf", "test info", 6, vec2(170, resolutionV2f.y - 20), 20, 0, NULL, 0, vec4(1));
	//	addRect(&infoRect);
	}
	setPixelData = new unsigned char[width * height * 4];
	clearSetPixelData();

    prevTime = (float)glfwGetTime();
	return 0;
}
void BaseGLD::clearSetPixelData() {
	for (int i = 0; i < resolutionWidth * resolutionHeight * 4; i++)
		setPixelData[i] = 0;
}

/* My best explenation/guess about what is goin on here:
The font textures are rasterized using stb truetype. stbtt also provides data on how to make actually use of the font texture,
But through my reading, I wasn't able to find/understand any guides on how to implement any of the data that work with this rendering workflow.
After loading a font map texture, the only other usefull data from stbtt I could use was the aligned quad which I name "c".
The "s" and "t" components reveal the location of a letter in te texture map as a percentage.
the "x" and "y" components reveal the scale of the letter. no components reveal the alignment or kerning information.
Since the letters are actually aligned to a grid withen the texture map, the alignedment can be calculated through 
a complicated calculation involving just the 4 floats provided by stb. */
unsigned char TTBuffer[1 << 20];
fontDat::fontDat(const char * filepath) {
	stbtt_bakedchar cData[96];
	filePath = filepath;
	float tallestC = 0; //height of the tallest letter
	char tc; //the tallest letter

	fread(TTBuffer, 1, 1 << 20, fopen(filepath, "rb"));

	stbtt_BakeFontBitmap(TTBuffer, 0, 32.0, bitmapBuffer, 512, 512, 32, 96, cData); // no guarantee this fits!
																				   // can free ttf_buffer at this point
	float xof = 0;
	float x = 0, y = 0;

	//find tallest letter
	for (int i = 32; i < 128; i++)
	{
		x = 0;
		y = 0;
		char c = i;
		stbtt_aligned_quad q;
		stbtt_GetBakedQuad(cData, 512, 512, c - 32, &x, &y, &q, 1);//1=opengl & d3d10+,0=d3d9

		uvOffset[i-32] = vec2(q.s0 + (q.s1 - q.s0) / 2, q.t0 + (q.t1 - q.t0) / 2);
		uvScale[i-32] = vec2((q.x1 - q.x0) / 2 / 512, abs(q.y1 - q.y0) / 2 / 512);
		quadScale[i-32] = vec2(q.x1 - q.x0, q.y0 - q.y1);

		float height = abs(q.y0 - q.y1);
		tallestC = height > tallestC ? height : tallestC;
		if (tallestC == height)
			tc = c;
	}
	//Pre-compute all the position and scale offsets of each letter to save time at runtime
	for (int i = 32; i < 128; i++)
	{
		x = 0;
		y = 0;
		char c = i;
		stbtt_aligned_quad q;
		stbtt_GetBakedQuad(cData, 512, 512, c - 32, &x, &y, &q, 1);


		alignment[i - 32] = -q.y1 - (tallestC + (q.y0 - q.y1)) / 2;
	}
	alignmentOffset = alignment[tc - 32];

	tallestLetter = tallestC;
	spaceOff = tallestLetter / 3;
	id = -1;
}
//this could be moved into the constuctor
void fontDat::loadTexture(){
	gl(GenTextures(1, &id));
	gl(BindTexture(GL_TEXTURE_2D, id));
	//	gl(TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, 512, 512, 0, GL_RGBA, GL_UNSIGNED_BYTE, tempBitmap));
	gl(TexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, 512, 512, 0, GL_DEPTH_COMPONENT, GL_UNSIGNED_BYTE, bitmapBuffer));
	// can free temp_bitmap at this point
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
}
void BaseGLD::checkFont(extraData * ed, rect r) {
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
	ed->fd = fonts.size() - 1;
}

//this could be moved into the img constructor, same as font
void loadTexture(const char * path, GLuint * id) {

	stbi_set_flip_vertically_on_load(true);
	int imW, imH, comp;
	unsigned char* image = stbi_load(path, &imW, &imH, &comp, STBI_rgb_alpha);
	if (image == NULL) {
		fprintf(stderr, "failed to load image\n");
		return;
	}
	gl(GenTextures(1, id));
	gl(BindTexture(GL_TEXTURE_2D, *id));
	gl(TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT));
	gl(TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT));
	gl(TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR));
	gl(TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR));
	gl(TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, imW, imH, 0, GL_RGBA, GL_UNSIGNED_BYTE, image));
	gl(BindTexture(GL_TEXTURE_2D, 0));
	
	stbi_image_free(image);
}
void BaseGLD::checkImg(extraData * ed, rect r) {
	if (ed->id != -1)
		return;
	for (int i = 0; i < imgs.size(); i++)
	{
		if (imgs[i].filePath == r.path) {
			ed->id = i;
			return;
		}
	}
	imgs.push_back(imgDat(r.path));
	ed->id = imgs.size() - 1;
}
imgDat::imgDat(const char * FilePath) {
	filePath = FilePath;
	loadTexture(FilePath, &ID);
}

void BaseGLD::setBBPixel(int x, int y, vec4 col) {
	while (setPixelCopyFlag) {

	}
	int gridX = x * 4;
	int gridY = resolutionWidth * y * 4;
	setPixelData[gridY + gridX] = col.r * 255;
	setPixelData[gridY + gridX + 1] = col.g * 255;
	setPixelData[gridY + gridX + 2] = col.b * 255;
	setPixelData[gridY + gridX + 3] = col.a * 255;
	setPixelFlag = true;
}
void BaseGLD::setBBShape(rect r) {
	BBQue.push_back(r);
}

void BaseGLD::setPolygonUniforms(rect * r) {

	gl(Uniform4f(PColorUniformLocation, r->color.r, r->color.g, r->color.b, r->color.a));
	gl(Uniform1i(PsideCountUniformLocation, r->sides));
	gl(Uniform1f(PborderWidthUniformLocation, r->borderW));
	gl(Uniform4f(PbordColorUniformLocation, r->borderColor.r, r->borderColor.g, r->borderColor.b, r->borderColor.a));
	gl(Uniform2f(PshapeScaleUniformLocation, r->scale.x, r->scale.y));
	gl(Uniform1f(PtimeUniformLocation, currTime));

	//transform
	gl(Uniform2f(PmScaleUniformLocation, r->scale.x / resolutionV2f.x, r->scale.y / resolutionV2f.y));
	gl(Uniform2f(PmPosUniformLocation, r->pos.x / resolutionV2f.x * 2.0f - 1.0f, r->pos.y / resolutionV2f.y * 2.0f - 1.0f));
	gl(Uniform1f(PmRotUniformLocation, r->angle - r->rotSpeed * currTime));

	gl(Uniform2f(PUVscaleUniformLocation, 0.5f, 0.5f));
	gl(Uniform2f(PUVposUniformLocation, 0.5f, 0.5f));
}

void BaseGLD::mainloop(bool render) {
	Input->clearStates();
	glfwPollEvents();

	if (glfwWindowShouldClose(window) || closeFlag) {
		cleanup();
		return;
	}

	glfwMakeContextCurrent(window);
	if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS) {
		glfwSetWindowShouldClose(window, true);
		cleanup();
		return;
	}

	currTime = (float)glfwGetTime();
	const float deltaTime = currTime - prevTime;
	LastRenderTime = floorf(deltaTime * 1000); //possibly reduntant variable
	currentFPS = 1.0f / deltaTime;
	prevTime = currTime;

	glfwGetWindowSize(window, &resolutionWidth, &resolutionHeight);
	resolutionV2f = vec2((float)resolutionWidth, (float)resolutionHeight);
	aspect = resolutionV2f.y / resolutionV2f.x;
		
	gl(Viewport(0, 0, resolutionWidth, resolutionHeight));

	//if the window size is changed, the back buffer and setpixel buffer needs to be re-created 
	if (windowSizeChanged) {
		gl(DeleteTextures(1, &fboTextIdA));
		gl(DeleteTextures(1, &fboTextIdB));
		gl(DeleteFramebuffers(1, &fboTextIdA));
		gl(DeleteFramebuffers(1, &fboTextIdB));

		genFramBuffer(&fboTextIdA, &fboIdA, resolutionWidth, resolutionHeight);
		genFramBuffer(&fboTextIdB, &fboIdB, resolutionWidth, resolutionHeight);

		delete setPixelData;
		setPixelData = new unsigned char[resolutionWidth * resolutionHeight * 4];
		clearSetPixelData();
	}

	//bind the back writing buffer
	gl(BindTexture(GL_TEXTURE_2D, 0));
	gl(BindFramebuffer(GL_FRAMEBUFFER, fboIdA));

	//clear the buffer
	if (clearColorFlag || windowSizeChanged) {
		gl(ClearColor(backCol.r, backCol.g, backCol.b, 1.0f));
		gl(Clear(GL_COLOR_BUFFER_BIT));
		clearColorFlag = false;
		if(render)
			setBBShape(rect(resolutionV2f / 2.0f, resolutionV2f * 2.0f, 0, vec4(backCol.r, backCol.g, backCol.b, 1), vec4(), 0, 0));
	}

	//draw shapes to the back buffer
	gl(BindVertexArray(VAO));
	gl(UseProgram(PolygonShaderProgram));
	if (render) {
		vector<rect> BBQueCopy = BBQue;//copy first for Thread safety
		BBQue.clear();
		for (int i = 0; i < BBQueCopy.size(); i++)
		{
			rect * r = &BBQueCopy[i];
			setPolygonUniforms(r);
			gl(DrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0));

		}
		BBQueCopy.clear();
	}

	//with the writing buffer still bound, create a texture from the setpixel data buffer and paste it to the back buffer
	if (setPixelFlag && render) {
		setPixelCopyFlag = true; //pause other thread while pixels are coppied to buffer
		gl(BindVertexArray(VAO));
		gl(UseProgram(fboShaderProgram));

		gl(GenTextures(1, &setPixelDataID));
		gl(BindTexture(GL_TEXTURE_2D, setPixelDataID));
		gl(TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT));
		gl(TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT));
		gl(TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR));
		gl(TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR));
		gl(TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, resolutionWidth, resolutionHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, setPixelData));

		gl(Uniform2f(FmScaleUniformLocation, 1.0, 1.0f));
		gl(Uniform2f(FmPosUniformLocation, 0, 0));
		gl(Uniform1f(FmRotUniformLocation, 0.0f));

		gl(Uniform2f(FUVscaleUniformLocation, 0.5f, 0.5f));
		gl(Uniform2f(FUVposUniformLocation, 0.5f, 0.5f));
		gl(Uniform1f(FaspectUniformLocation, aspect));

		gl(DrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0));
		gl(BindTexture(GL_TEXTURE_2D, 0));
		gl(DeleteTextures(1, &setPixelDataID));
		clearSetPixelData();
		setPixelFlag = false;
		setPixelCopyFlag = false; //resume other thread
	}

	//transfer the back write buffer to the read buffer
	{
		gl(BindVertexArray(VAO));
		gl(UseProgram(fboShaderProgram));

		gl(BindFramebuffer(GL_FRAMEBUFFER, fboIdB));
		gl(Viewport(0, 0, resolutionWidth, resolutionHeight));

		gl(ActiveTexture(GL_TEXTURE0 + 0));
		gl(BindTexture(GL_TEXTURE_2D, fboTextIdA));

		//transform
		gl(Uniform2f(FmScaleUniformLocation, 1.0, 1.0f));
		gl(Uniform2f(FmPosUniformLocation, 0, 0));
		gl(Uniform1f(FmRotUniformLocation, 0.0f));

		gl(Uniform2f(FUVscaleUniformLocation, 0.5f, 0.5f));
		gl(Uniform2f(FUVposUniformLocation, 0.5f, 0.5f));
		gl(Uniform1f(FaspectUniformLocation, aspect));

		gl(DrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0));
		gl(BindTexture(GL_TEXTURE_2D, 0));

		gl(UseProgram(0));
		gl(BindVertexArray(0));
	}

	//transform the back buffer and draw it as a background for the front buffer
	{
		gl(BindFramebuffer(GL_FRAMEBUFFER, 0));
		gl(Disable(GL_CULL_FACE));
		gl(Viewport(0, 0, resolutionWidth, resolutionHeight));

		gl(BindVertexArray(VAO));
		gl(UseProgram(PolygonShaderProgram));
		gl(Uniform1f(PaspectUniformLocation, aspect));

		gl(ActiveTexture(GL_TEXTURE0 + 0));
		gl(BindTexture(GL_TEXTURE_2D, fboTextIdB));

		gl(Uniform4f(PColorUniformLocation, 1.0f, 1.0f, 1.0f, 1.0f));
		gl(Uniform1i(PsideCountUniformLocation, 0));
		gl(Uniform1f(PborderWidthUniformLocation, 0));
		gl(Uniform2f(PshapeScaleUniformLocation, 1.0f, 1.0f));

		//transform
		gl(Uniform2f(PmScaleUniformLocation, 1.0, 1.0f));
		gl(Uniform2f(PmPosUniformLocation, 0, 0));
		gl(Uniform1f(PmRotUniformLocation, 0.0f));

		gl(Uniform2f(PUVscaleUniformLocation, 0.5f, 0.5f));
		gl(Uniform2f(PUVposUniformLocation, 0.5f, 0.5f));

		gl(DrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0));
		gl(BindTexture(GL_TEXTURE_2D, 0));
		gl(UseProgram(0));
		gl(BindVertexArray(0));
	}

	if (render) {
		//thread safe transfer object buffer to canvas draw list
		rectCopyFlag = true;
		for (int i = 0; i < rectBuffer.size(); i++)
			rects.push_back(rectBuffer[i]);
		rectBuffer.clear();

		for (int i = 0; i < eDataBuffer.size(); i++)
			eData.push_back(eDataBuffer[i]);
		eDataBuffer.clear();

		//thread safe removal of shapes
		for (int i = 0; i < rects.size(); i++)
		{
			for (int j = 0; j < removalBuffer.size(); j++)
			{
				if (rects[i] == removalBuffer[j]) {
					rects.erase(rects.begin() + i);
					eData.erase(eData.begin() + i);
				}
			}
		}
		removalBuffer.clear();
		if (clearShapeFlag) {
			rects.clear();
			eData.clear();
			clearShapeFlag = false;
		}
		rectCopyFlag = false;
	}


	//Draw everything else
	gl(BindVertexArray(VAO));
	gl(UseProgram(PolygonShaderProgram));
	gl(Uniform1f(PaspectUniformLocation, aspect));

	for (int i = 0; i < rects.size(); ++i)
	{
		rect * r = rects[i];
		if (r->hidden)
			continue;

		//draw as a font
		if (r->sides == -1) {
			drawFont(r, &eData[i]);
		}
		//draw as a texture
		else if (!r->sides) {
			checkImg(&eData[i], *r);

			gl(BindTexture(GL_TEXTURE_2D, imgs[eData[i].id].ID));
			gl(Uniform1i(PtextureUniformLocation, 0));
			setPolygonUniforms(r);
			gl(DrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0));
		}
		//draw as a polygon
		else {
			setPolygonUniforms(r);
			gl(DrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0));
		}
		gl(BindTexture(GL_TEXTURE_2D, 0));
	}
	if (debugMode) {
		infoRect.pos = vec2(170, resolutionV2f.y - 30);

		infoRect.text = debugString;
		drawFont(&infoRect, &infoData);
	}
	gl(UseProgram(0));
	gl(BindVertexArray(0));
	
	windowSizeChanged = false;

	glfwSwapBuffers(window);

	updateDebugInfo();

	if(titleDetails)
		glfwSetWindowTitle(window, (title + debugString).c_str());
	else
		glfwSetWindowTitle(window, title);
}

//ready for a huge optimization. %90 of this can be calulated in the vertex shader
void BaseGLD::drawFont(rect * r, extraData * fontData) {

	gl(Uniform4f(PColorUniformLocation, r->color.r, r->color.g, r->color.b, r->color.a));
	gl(Uniform1i(PsideCountUniformLocation, r->sides));
	gl(Uniform1f(PborderWidthUniformLocation, r->borderW));
	gl(Uniform4f(PbordColorUniformLocation, r->borderColor.r, r->borderColor.g, r->borderColor.b, r->borderColor.a));
	gl(Uniform2f(PshapeScaleUniformLocation, r->scale.x, r->scale.y));
	gl(Uniform1f(PtimeUniformLocation, currTime));
	
	checkFont(fontData, *r); //checks if this ttf file already loaded into canvas memory
	fontDat * selected = &fonts[(*fontData).fd];

	gl(BindTexture(GL_TEXTURE_2D, selected->id));
	gl(Uniform1i(PtextureUniformLocation, 0));

	float xof = 0; //x offset of each letter
	float x = 0, y = 0; //required by stbtt
	float scaleRatio = r->scale.y / selected->tallestLetter;
	float totalWidth = 0;
	int lineCount = 1; //number of lines in the string (# of '\n')
	int lineNum = 0;  //current line being worked on
	char c; //used to store each letter for calculations
	bool boundMode = r->bound; //bounding box mode
	int maxLines; //Only used for bounding box mode
	float record = 0; //longest line of text
	int recordIndex = 0; //which line number is the
	int Xreference; // iether scaled boundry.X or record

	//pre calculate the pixel lengths of the final lines
	for (int i = 0; i < r->text.length(); i++)
		if (r->text[i] == '\n')
			lineCount++;
	float * lineLengths = new float[lineCount];

	//pre compute the eventual line lengths
	for (int i = 0, lineCounter = 1; i < r->text.length(); i++)
	{
		c = r->text[i];
		//actual letter lengths
		if (c != '\n' && c != ' ');
			totalWidth += selected->quadScale[c - 32].x;
		//add in distace for space characters
		if (c == ' ')
			totalWidth += selected->spaceOff;
		if (c == '\n' || i == r->text.length() - 1) {
			record = max(record, totalWidth);
			lineLengths[lineCounter - 1] = totalWidth;
			lineCounter++;
			totalWidth = 0;
		}
	}

	if (boundMode) {
		maxLines = r->bound->scale.y / r->scale.y;
		r->pos = r->bound->pos + vec2(-r->bound->scale.x / (float)2, 
									   r->bound->scale.y / (float)2 - selected->tallestLetter / (float)2);
		Xreference = r->bound->scale.x / scaleRatio;
	}
	else Xreference = record;
					
	for (int i = 0; i < r->text.length(); i++)
	{
		c = r->text[i];

		//handle new line
		if (c == '\n') {
			lineNum++;
			if (boundMode && lineNum + 1 > maxLines)
				break;
			continue;
		}
		//set the cursor for the new line
		if (i == 0 || r->text[i - 1] == '\n') {
			if (r->justification == 0 || (boundMode && lineLengths[lineNum] > r->bound->scale.x / scaleRatio))
				xof = 0;
			else if (r->justification == 1)
				xof = (Xreference - lineLengths[lineNum]) / 2;
			else {
				xof = (Xreference - lineLengths[lineNum]);
			}		
		}

		//usually, the position and scale values of the vertex shader are 0.5, but with letters on a texture map, these values must be defined based on the UV data
		gl(Uniform2f(PUVscaleUniformLocation, selected->uvScale[c-32].x, selected->uvScale[c - 32].y));
		gl(Uniform2f(PUVposUniformLocation, selected->uvOffset[c-32].x, selected->uvOffset[c-32].y));;
						
		vec2 letterScale = floor(selected->quadScale[c-32]); 

		xof += letterScale.x / 2; //move the scale by  width of current charector
		if(boundMode && xof *scaleRatio > r->bound->scale.x)
			continue;

		float xTranslate = xof;
		float yTranslate = selected->alignment[c - 32] - selected->tallestLetter * lineNum;
				
		//calculate final position of the letter
		vec2 letterPosition;
		{
			if (!boundMode) {
				xTranslate -= record / 2;
				yTranslate += selected->alignmentOffset;
				yTranslate += (selected->tallestLetter * lineCount) / 2;
			}

			float x = (r->pos.x + xTranslate * scaleRatio) / resolutionV2f.x * 2.0f - 1.0f;
			float y = (r->pos.y + yTranslate * scaleRatio) / resolutionV2f.y * 2.0f - 1.0f;
			letterPosition = vec2(x, y);				
		}
		//final scale of the letter
		vec2 finalScale;
		{
			float x = letterScale.x * scaleRatio / resolutionV2f.x;
			float y = letterScale.y * scaleRatio / resolutionV2f.y;
			finalScale = vec2(x, y);
		}

		//transform
		gl(Uniform2f(PmScaleUniformLocation, finalScale.x, finalScale.y));
		gl(Uniform2f(PmPosUniformLocation, letterPosition.x, letterPosition.y));
		gl(Uniform1f(PmRotUniformLocation, r->angle - r->rotSpeed * currTime));

		if (c == ' ')
			xof += selected->spaceOff;
		xof += letterScale.x / 2;

		gl(DrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0));

	}			
	delete lineLengths;
}

vec3 BaseGLD::getPixel(int x, int y){
	y = -y + resolutionHeight;
	unsigned char pixel[3] = { 0 };
	gl(ReadPixels(x, y, 1, 1, GL_RGB, GL_UNSIGNED_BYTE, &pixel));
	return vec3(pixel[0], pixel[1], pixel[2]);
}

void BaseGLD::updateDebugInfo() {
	if (debugTimer < currTime - debugUpdateFreq || debugTimer == 0)
		debugTimer = currTime;
	else
		return;
	std::stringstream sstm;
	sstm.precision(0);
	sstm.setf(std::ios::fixed);
	sstm << "Render Time: " << LastRenderTime << "ms  FPS: " << ceil(currentFPS) << "  Shapes: " << (rects.size());
	debugString = sstm.str();
}