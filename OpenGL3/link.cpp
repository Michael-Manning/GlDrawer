#define _CRT_SECURE_NO_WARNINGS

#include <stdio.h>
#include <tchar.h>
#include <math.h>
#include <assert.h>

#include <gl3w/gl3w.h>
#include <GL/GLU.h>
#include <GLFW/glfw3.h>

#define GLFW_EXPOSE_NATIVE_WIN32

#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>
#define STB_IMAGE_IMPLEMENTATION
#include <stb/stb_image.h>
#define STB_TRUETYPE_IMPLEMENTATION 
#include <stb/stb_truetype.h>


#include <iostream>
#include <string>
#include <fstream>
#include <sstream>
#include <vector>
#include "Input.h"
#include "link.h"

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
	rects.push_back(r);
	eData.push_back(extraData());
	//ids.push_back(-1);

}
//void BaseGLD::addText(rect * r, char * Text, int length) {
//	rects.push_back(r);
//	eData.push_back(extraData);
//	for (int i = 0; i < fonts.size(); i++)
//	{
//		if (r->path == fonts[i].filePath) {
//			
//		}	
//	}
//}
float RandFloat() {
	return static_cast <float> (rand()) / static_cast <float> (RAND_MAX);
}
vec4 RandColor() {
	return vec4(RandFloat(), RandFloat(), RandFloat(), 1.0f);
}

#pragma endregion

const char * defaultFilepath = "C:\\Users\\Micha\\Desktop\\GLDrawer\\Gl3DrawerCLR\\OpenGL3\\bin\\";

int loadShader(const char * vertexFilename, const char * fragmentFilename) {


	ostringstream sstream;

	ifstream fs((string)defaultFilepath + "Shaders\\" + (string)vertexFilename + ".glsl");
	sstream << fs.rdbuf();
	const string str(sstream.str());
	const char * vertexShaderSource = str.c_str();

	if (str == "") {
		std::cout << "ERROR: Failed to load vertex shader source \""<< vertexFilename <<"\"" << "\n" << std::endl;
	}

	ostringstream sstream2;
	ifstream fs2 ((string)defaultFilepath + "Shaders\\" + (string)fragmentFilename + ".glsl");
	sstream2 << fs2.rdbuf();
	const string str2(sstream2.str());
	const char * fragmentShaderSource = str2.c_str();

	if (str2 == "") {
		std::cout << "ERROR: Failed to load fragment shader source \"" << fragmentFilename << "\"" << "\n" << std::endl;
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
	glfwTerminate();
	gl(DeleteVertexArrays(1, &VAO));
	gl(DeleteBuffers(1, &VBO));
	gl(DeleteBuffers(1, &EBO));
	gl(DeleteTextures(1, &textureId));
	gl(DeleteFramebuffers(1, &fboId));
	//delete all tectures here?
}
void BaseGLD::swapOrder(int a, int b) {
	rect* temp = rects[a];
	rects[a] = rects[b];
	rects[b] = temp;
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
	for (int i = 0; i < rects.size(); i++)
	{
		if (rects[i] == r) {
			rects.erase(rects.begin() + i);
			eData.erase(eData.begin() + i);
		}
	}
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
rect::rect(const char* filePath, string Text, int length, vec2 Pos, vec2 Scale, float Angle, vec4 Color, vec4 BorderCol, float bordW, float RotationSpeed) {
	pos = Pos;
	scale = Scale;
	angle = Angle;
	color = Color;
	borderColor = BorderCol;
	borderW = bordW;
	rotSpeed = RotationSpeed;
	path = filePath;
	text = Text;
	textLength = length;
	//tdat = textDat{ filePath, text, textLength };
	sides = -1;
	hidden = false;
}
int BaseGLD::createCanvas(int width, int height, bool borderd, vec3 backCol)
{

#pragma region setup

	resolutionWidth = width;
	resolutionHeight = height;
	
	backCol = vec4(backCol, 1);
	int glfwInitResult = glfwInit() ;
	if (glfwInitResult != GLFW_TRUE)
	{
		fprintf(stderr, "glfwInit returned false\n");
		 return 1;
	}
	

	glfwWindowHint(GLFW_DECORATED, borderd);
	glfwWindowHint(GLFW_FLOATING, !borderd);
	//glfwWindowHint(GLFW_VISIBLE, false);
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
//	glfwSetWindowUserPointer(window, window);


    //RectShaderProgram = loadShader("RectVertex", "RectFragment");
	CircleShaderProgram = loadShader("RectVertex", "CircleFragment");
	PolygonShaderProgram = loadShader("RectVertex", "PolygonFragment");
	TextureShaderProgram = loadShader("RectVertex", "TextureFragment");

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

#pragma endregion

 //   RxformUniformLocation = gl(GetUniformLocation(RectShaderProgram, "xform"));
	//RColorUniformLocation = gl(GetUniformLocation(RectShaderProgram, "Color"));
	//RaspectUniformLocation = gl(GetUniformLocation(RectShaderProgram, "aspect"));
	//RshapeScaleUniformLocation = gl(GetUniformLocation(RectShaderProgram, "shapeScale"));

	ExformUniformLocation = gl(GetUniformLocation(CircleShaderProgram, "xform"));
	EshapeScaleUniformLocation = gl(GetUniformLocation(CircleShaderProgram, "shapeScale"));
	EColorUniformLocation = gl(GetUniformLocation(CircleShaderProgram, "Color"));
	EaspectUniformLocation = gl(GetUniformLocation(CircleShaderProgram, "aspect"));
	EbordWidthUniformLocation = gl(GetUniformLocation(CircleShaderProgram, "bordWidth"));
	
	PxformUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "xform"));
	PshapeScaleUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "shapeScale"));
	PColorUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "Color"));
	PbordColorUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "bordColor"));
	PsideCountUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "sideCount"));
	PborderWidthUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "bordWidth"));
	PaspectUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "aspect"));
	PtextureUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "Text"));
	PUVscaleUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "scaleOffset"));
	PUVposUniformLocation = gl(GetUniformLocation(PolygonShaderProgram, "posOffset"));

	TxformUniformLocation = gl(GetUniformLocation(TextureShaderProgram, "xform"));
	TColorUniformLocation = gl(GetUniformLocation(TextureShaderProgram, "Color"));
	TaspectUniformLocation = gl(GetUniformLocation(TextureShaderProgram, "aspect"));
	TshapeScaleUniformLocation = gl(GetUniformLocation(TextureShaderProgram, "shapeScale"));
	TtextureScaleUniformLocation = gl(GetUniformLocation(TextureShaderProgram, "Text"));

	//Vsync disable
	glfwSwapInterval(0);

	glGenTextures(1, &textureId);
	glGenFramebuffers(1, &fboId);	
	{
		glBindTexture(GL_TEXTURE_2D, textureId);
		defer(glBindTexture(GL_TEXTURE_2D, 0));
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, resolutionWidth, resolutionHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, 0);

		glBindFramebuffer(GL_FRAMEBUFFER, fboId);
		defer(glBindFramebuffer(GL_FRAMEBUFFER, 0));

		// attach the texture to FBO color attachment point
		glFramebufferTexture2D(GL_FRAMEBUFFER,        // 1. fbo target: GL_FRAMEBUFFER 
			GL_COLOR_ATTACHMENT0,  // 2. attachment point
			GL_TEXTURE_2D,         // 3. tex target: GL_TEXTURE_2D
			textureId,             // 4. tex ID
			0);                    // 5. mipmap level: 0(base)

								   // check FBO status
		GLenum status = glCheckFramebufferStatus(GL_FRAMEBUFFER);
		assert(status == GL_FRAMEBUFFER_COMPLETE);

		gl(BindTexture(GL_TEXTURE_2D, textureId));
		glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, resolutionWidth, resolutionHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, 0);
		gl(BindTexture(GL_TEXTURE_2D, 0));

		glBindFramebuffer(GL_FRAMEBUFFER, fboId);

		gl(ClearColor(backCol.r, backCol.g, backCol.b, 1.0f));
		gl(Clear(GL_COLOR_BUFFER_BIT));
	}

    prevTime = (float)glfwGetTime();
	return 0;
}
unsigned char TTBuffer[1 << 20];
fontDat::fontDat(const char * filepath) {
	stbtt_bakedchar cData[96];
	filePath = filepath;
	float tallestC = 0;

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
	}

	for (int i = 32; i < 128; i++)
	{
		x = 0;
		y = 0;
		char c = i;
		stbtt_aligned_quad q;
		stbtt_GetBakedQuad(cData, 512, 512, c - 32, &x, &y, &q, 1);//1=opengl & d3d10+,0=d3d9


		alignment[i - 32] = -q.y1 - (tallestC      + (q.y0 - q.y1)) / 2;
	}
	tallestLetter = tallestC;
	spaceOff = tallestLetter / 3;
	id = -1;
}
void fontDat::loadTexture(){
	gl(GenTextures(1, &id));
	gl(BindTexture(GL_TEXTURE_2D, id));
	//	gl(TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, 512, 512, 0, GL_RGBA, GL_UNSIGNED_BYTE, tempBitmap));
	gl(TexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, 512, 512, 0, GL_DEPTH_COMPONENT, GL_UNSIGNED_BYTE, bitmapBuffer));
	// can free temp_bitmap at this point
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
}

//unsigned char ttfBuffer[1 << 20];
//unsigned char tempBitmap[512 * 512];
//float tallestLetter = 0;
//stbtt_bakedchar cdata[96];// ASCII 32..126 is 95 glyphs
//stbtt_fontinfo font;

void loadTexture(const char * path, GLuint * id) {
#if 0
	fread(ttfBuffer, 1, 1 << 20, fopen("c:/windows/fonts/times.ttf", "rb"));
	//stbtt_InitFont(&font, ttfBuffer, stbtt_GetFontOffsetForIndex(ttfBuffer, 0));
	stbtt_BakeFontBitmap(ttfBuffer, 0, 32.0, tempBitmap, 512, 512, 32, 96, cdata); // no guarantee this fits!
				   // can free ttf_buffer at this point

 	gl(GenTextures(1, id));
	gl(BindTexture(GL_TEXTURE_2D, *id));
	//	gl(TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, 512, 512, 0, GL_RGBA, GL_UNSIGNED_BYTE, tempBitmap));
 	gl(TexImage2D(GL_TEXTURE_2D, 0, GL_DEPTH_COMPONENT, 512, 512, 0, GL_DEPTH_COMPONENT, GL_UNSIGNED_BYTE, tempBitmap));
	// can free temp_bitmap at this point
	glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
	
	float x = 0, y = 0;
	//find tallest letter
	for (int i = 32; i < 128; i++)
	{
		char c = i;
		stbtt_aligned_quad q;
		stbtt_GetBakedQuad(cdata, 512, 512, c - 32, &x, &y, &q, 1);//1=opengl & d3d10+,0=d3d9
		float height = abs(q.y0 - q.y1);
		tallestLetter = height > tallestLetter ? height: tallestLetter;
	}
	return;

#endif
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
bool tempinit = false;
void BaseGLD::mainloop() {
	if (!glfwWindowShouldClose(window))
	{
		glfwMakeContextCurrent(window);
		if (glfwGetKey(window, GLFW_KEY_ESCAPE) == GLFW_PRESS)
			glfwSetWindowShouldClose(window, true);


		const float currTime = (float)glfwGetTime();
		const float deltaTime = currTime - prevTime;
		const float fps = 1.0f / deltaTime;
		prevTime = currTime;

		glfwGetWindowSize(window, &resolutionWidth, &resolutionHeight);
		const vec2 resolutionV2f = vec2((float)resolutionWidth, (float)resolutionHeight);
		const float aspect = resolutionV2f.y / resolutionV2f.x;



		gl(BindTexture(GL_TEXTURE_2D, textureId));
		glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, resolutionWidth, resolutionHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, 0);
		gl(BindTexture(GL_TEXTURE_2D, 0));

		glBindFramebuffer(GL_FRAMEBUFFER, fboId);
		gl(Viewport(0, 0, resolutionWidth, resolutionHeight));

		gl(BindVertexArray(VAO));
		gl(UseProgram(CircleShaderProgram));
		gl(Uniform1f(EaspectUniformLocation, aspect));

		//if (Input->getMouse(MouseLeft) || Input->getMouseDown(MouseRight)) {

		//	double x, y;
		//	glfwGetCursorPos(window, &x, &y);


		//	rect r = rect(vec2((float)x, resolutionV2f.y - (float)y), vec2(sin(currTime * 12.0f) * 25 + 50.0f), 0, vec4((sin(currTime * 8) + 1) / 2, (sin(currTime * 12) + 1) / 2, (cos(currTime * 6) + 1) / 2, 1));

		//	//fill drawing
		//	mat4 m(1.0f);
		//	m = translate(m, vec3(r.pos / resolutionV2f * 2.0f - vec2(1.0f, 1.0f), 0.0f));
		//	m = scale(m, vec3(aspect, 1.0f, 1.0f));
		//	m = rotate(m, r.angle + r.rotSpeed * currTime, vec3(0.0f, 0.0f, 1.0f));
		//	mat4 m_inner = scale(m, vec3(r.scale / resolutionV2f, 1.0f));

		//	gl(Uniform1f(EbordWidthUniformLocation, 0.0f));
		//	gl(Uniform4f(EColorUniformLocation, r.color.r, r.color.g, r.color.b, r.color.a));
		//	gl(Uniform2f(EshapeScaleUniformLocation, r.scale.x, r.scale.y));
		//	gl(UniformMatrix4fv(ExformUniformLocation, 1, GL_FALSE, value_ptr(m_inner)));
		//	gl(DrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0));

		//}
		//if (Input->getKey('g'))
		//	fprintf(stderr, "G!\n");

		gl(UseProgram(0));
		gl(BindVertexArray(0));

		glBindFramebuffer(GL_FRAMEBUFFER, 0);
		gl(Disable(GL_CULL_FACE));
		gl(Viewport(0, 0, resolutionWidth, resolutionHeight));

		//draw back buffer 
		{
			mat4 m(1.0f);

			gl(BindVertexArray(VAO));
			gl(UseProgram(TextureShaderProgram));
			gl(ActiveTexture(GL_TEXTURE0 + 0));
			gl(BindTexture(GL_TEXTURE_2D, textureId));
			gl(Uniform1i(TtextureScaleUniformLocation, 0));
			gl(Uniform1f(TaspectUniformLocation, 1.0f));
			gl(Uniform4f(TColorUniformLocation, 1.0f, 1.0f, 1.0f, 1.0f));
			gl(Uniform2f(TshapeScaleUniformLocation, 1.0f, 1.0f));
			gl(UniformMatrix4fv(TxformUniformLocation, 1, GL_FALSE, value_ptr(m)));
			gl(DrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0));
			gl(BindTexture(GL_TEXTURE_2D, 0));
			gl(UseProgram(0));
			gl(BindVertexArray(0));
		}

			gl(BindVertexArray(VAO));
			gl(UseProgram(PolygonShaderProgram));
			gl(Uniform1f(PaspectUniformLocation, aspect));
			for (int i = 0; i < rects.size(); ++i)
			{
				rect * r = rects[i];//rectSource[rectsPTR[i]];
				if (r->hidden)
					continue;

				gl(Uniform4f(PColorUniformLocation, r->color.r, r->color.g, r->color.b, r->color.a));
				gl(Uniform1i(PsideCountUniformLocation, r->sides));
				gl(Uniform1f(PborderWidthUniformLocation, r->borderW));
				gl(Uniform4f(PbordColorUniformLocation, r->borderColor.r, r->borderColor.g, r->borderColor.b, r->borderColor.a));
				gl(Uniform2f(PshapeScaleUniformLocation, r->scale.x, r->scale.y));

				if ( r->sides == -1) {
					checkFont(&eData[i], *r);

					fontDat * selected = &fonts[eData[i].fd];

					gl(BindTexture(GL_TEXTURE_2D, selected->id));
					gl(Uniform1i(PtextureUniformLocation, 0));

					float xof = 0;;
					float x = 0, y = 0;
					float scaleRatio = r->scale.y / selected->tallestLetter;
					float totalWidth = 0;
					char c;
					for (int i = 0; i < r->text.length(); i++)
					{
						c = r->text[i];
						totalWidth += selected->quadScale[c - 32].x;
						if (c == ' ')
							totalWidth += selected->spaceOff;
					}
					totalWidth *= scaleRatio;
					for (int i = 0; i < r->text.length(); i++)
					{

						c = r->text[i];

						gl(Uniform2f(PUVscaleUniformLocation, selected->uvScale[c-32].x, selected->uvScale[c - 32].y));
						gl(Uniform2f(PUVposUniformLocation, selected->uvOffset[c-32].x, selected->uvOffset[c-32].y));;
						
							rect rr = *r;
							rr.scale = selected->quadScale[c-32]; 
							if(i > 0)
								xof += rr.scale.x / 2;
							else 
								xof -= rr.scale.x / 4;
							mat4 m(1.0f);

							m = translate(m, vec3((r->pos + vec2(xof - totalWidth/4, selected->alignment[c-32] + selected->tallestLetter / 4)  * scaleRatio) / resolutionV2f * 2.0f - vec2(1.0f, 1.0f), 0.0f));
							m = scale(m, vec3(aspect, 1.0f, 1.0f));
							m = rotate(m, r->angle + r->rotSpeed * currTime, vec3(0.0f, 0.0f, 1.0f));
							m = scale(m, vec3(rr.scale  * scaleRatio / resolutionV2f, 1.0f));
							if (c == ' ')
								xof += selected->spaceOff;
							xof += rr.scale.x / 2;
							gl(UniformMatrix4fv(PxformUniformLocation, 1, GL_FALSE, value_ptr(m)));

							gl(DrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0));

					}
					continue;
				}
				else {
					if (!r->sides) {
						if (eData[i].id == -1)
							loadTexture(r->path, &eData[i].id);

						gl(BindTexture(GL_TEXTURE_2D, eData[i].id));
						gl(Uniform1i(PtextureUniformLocation, 0));
#if 0
						string temp = "hello. This is my second attempt at text! WA]";
						char text[46];
						strcpy(text, temp.c_str());

						float xof = 0;;
						float x = 0, y = 0;
						for (int i = 0; i < 45; i++)
						{

							char c = text[i];
							//if (c >= 32 && c < 128) 
							stbtt_aligned_quad q;
							stbtt_GetBakedQuad(cdata, 512, 512, c - 32, &x, &y, &q, 1);//1=opengl & d3d10+,0=d3d9


							gl(Uniform2f(PUVscaleUniformLocation, (q.x1 - q.x0) / 2 / 512, abs(q.y1 - q.y0) / 2 / 512));
							gl(Uniform2f(PUVposUniformLocation, q.s0 + (q.s1 - q.s0) / 2, q.t0 + (q.t1 - q.t0) / 2));


							rect rr = *r;
							rr.scale = vec2(q.x1 - q.x0, q.y0 - q.y1);//rr.scale.x = abs(rr.scale.y) * (q.x1 / abs(q.y0));
							xof += rr.scale.x / 2;
							mat4 m(1.0f);
							m = translate(m, vec3((r->pos + vec2(xof + (0 * i), -q.y1 - (tallestLetter + (q.y0 - q.y1)) / 2)) / resolutionV2f * 2.0f - vec2(1.0f, 1.0f), 0.0f));
							m = scale(m, vec3(aspect, 1.0f, 1.0f));
							m = rotate(m, r->angle + r->rotSpeed * currTime, vec3(0.0f, 0.0f, 1.0f));
							m = scale(m, vec3(rr.scale / resolutionV2f, 1.0f));
							if (c == ' ')
								xof += tallestLetter / 3;
							xof += rr.scale.x / 2;
							gl(UniformMatrix4fv(PxformUniformLocation, 1, GL_FALSE, value_ptr(m)));

							gl(DrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0));

						}
#endif 
					}
				//	else {
					mat4 m(1.0f);
					m = translate(m, vec3(r->pos / resolutionV2f * 2.0f - vec2(1.0f, 1.0f), 0.0f));
					m = scale(m, vec3(aspect, 1.0f, 1.0f));
					m = rotate(m, r->angle + r->rotSpeed * currTime, vec3(0.0f, 0.0f, 1.0f));
					m = scale(m, vec3(r->scale / resolutionV2f, 1.0f));


					gl(Uniform4f(PColorUniformLocation, r->color.r, r->color.g, r->color.b, r->color.a));
					gl(Uniform1i(PsideCountUniformLocation, r->sides));
					gl(Uniform1f(PborderWidthUniformLocation, r->borderW));
					gl(Uniform4f(PbordColorUniformLocation, r->borderColor.r, r->borderColor.g, r->borderColor.b, r->borderColor.a));
					gl(Uniform2f(PshapeScaleUniformLocation, r->scale.x, r->scale.y));
					gl(UniformMatrix4fv(PxformUniformLocation, 1, GL_FALSE, value_ptr(m)));

					gl(Uniform2f(PUVscaleUniformLocation, 0.5f,0.5f));
					gl(Uniform2f(PUVposUniformLocation, 0.5f, 0.5f));


						//}
					gl(DrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_INT, 0));
				}	
				gl(BindTexture(GL_TEXTURE_2D, 0));
			}
			gl(UseProgram(0));
			gl(BindVertexArray(0));
	
		//reset on frame key presses
			Input->clearStates();

		glfwSwapBuffers(window);
		glfwPollEvents();
		LastRenderTime = deltaTime;
		currentFPS = fps;
		std::stringstream sstm;
		sstm << "Frame Time: " << deltaTime * 1000.0f << "ms  FPS: " << ceil(fps) << "  Shapes: " << (rects.size()) << "   mouseleft: " << Input->LeftMouseState << " mouseRigh: " << Input->RightMouseState;
		glfwSetWindowTitle(window, sstm.str().c_str());
	}
}