#include "stdafx.h"
#include <gl3w/gl3w.h>
#include "GLDrawerCLR.h"
#include <OpenGL3/engine.cpp>
#include <OpenGL3/Input.cpp>
#include <GLFW/glfw3.h>
#include <glm/glm.hpp>

using namespace std;

GLDrawerCLR::unmanaged_Canvas::unmanaged_Canvas(bool packShaders) {
	base = new GLCanvas();
	base->usePackedShaders = packShaders; //the fininall DLL need to be portable. Set this to true on export
	Input = gcnew InputManagerCLR(base);	
}
int GLDrawerCLR::unmanaged_Canvas::createCanvas(int width, int height, bool borderd, unmanaged_color ^ backColor, bool Vsync, bool DebugMode) { 
	cout << "Running window from C# wrapper \n";
	base->debugMode = DebugMode;
    initialized = !base->createCanvas(width, height, !borderd, vec3(backColor->r, backColor->g, backColor->b), Vsync);
	return initialized;
}
void GLDrawerCLR::unmanaged_shape::dispose() {
	delete s;
}
void GLDrawerCLR::unmanaged_Canvas::clearNullRects() {
	base->removeNullShapes();
}
void GLDrawerCLR::unmanaged_Canvas::removeRect(unmanaged_shape ^ s) {
	base->removeShape(s->s);
}
void GLDrawerCLR::unmanaged_Canvas::swapOrder(int a, int b) {
	base->swapOrder(a, b);
}
void GLDrawerCLR::unmanaged_Canvas::setPos(int x, int y) {
	base->setPos(x, y);
}
void GLDrawerCLR::unmanaged_Canvas::setVisible(bool visible) {
	base->setVisible(visible);
}
void GLDrawerCLR::unmanaged_Canvas::focusWindow() {
	base->focus();
}
int GLDrawerCLR::unmanaged_Canvas::addRect(unmanaged_shape ^ s) {
	base->addShape(s->s);
	return 1; 
}
bool GLDrawerCLR::unmanaged_Canvas::checkLoaded(unmanaged_shape ^ s) {
	for (int i = 0; i < base->shapes.size(); i++)
		if (base->shapes[i] == s->s)
			return true;
	return false;
}
void GLDrawerCLR::unmanaged_Canvas::setBBpixel(int x, int y, unmanaged_color ^ col) {
	base->setBBPixel(x, y, vec4(col->r, col->g, col->b, col->a));
}
void GLDrawerCLR::unmanaged_Canvas::setBBShape(unmanaged_shape ^ s) {
	base->setBBShape(*s->s);
}
GLDrawerCLR::unmanaged_color ^ GLDrawerCLR::unmanaged_Canvas::getPixel(int x, int y) {
	vec3 c = base->getPixel(x, y);
	return gcnew unmanaged_color(vec4(c, 1));
}
void GLDrawerCLR::unmanaged_Canvas::addBBShape(unmanaged_shape ^ s) {
	base->BBQue.push_back(*s->s);
}
IntPtr GLDrawerCLR::unmanaged_Canvas::getNativeHWND() {
	HWND hwnd = base->getNativeHWND();
	return IntPtr(hwnd);
}
void GLDrawerCLR::unmanaged_Canvas::clearBB() {
	base->clearColorFlag = true;
}
void GLDrawerCLR::unmanaged_Canvas::clearShapes() {
	base->clearShapeFlag = true;
}
int GLDrawerCLR::unmanaged_Canvas::getRectIndex(unmanaged_shape ^ s) {
	return base->getDrawIndex(s->s);
}
void GLDrawerCLR::unmanaged_Canvas::setWindowSize(int x, int y) {
	base->setWindowSize(x, y);
}
void GLDrawerCLR::unmanaged_Canvas::dispose() {
	base->cleanup();
	//Input->dispose();
//	delete base->Input;
	delete base;
}

void GLDrawerCLR::unmanaged_Canvas::mainloop(bool render) {
	if (shouldClose)
		return;

	//check to see if GLFW has triggered any events (up to 10 per frame) and call the corosponding events in C#
	for (int i = 0; i < 10; i++)
	{
		if (!base->Input->mouseBuffer[i].read) 
			if(Input->csharpMouseCallback)
				Input->csharpMouseCallback(base->Input->mouseBuffer[i].btn, base->Input->mouseBuffer[i].action, 1);
		base->Input->mouseBuffer[i].read = true;

		if (!base->Input->keyBuffer[i].read) {
			if (i > 0 && base->Input->keyBuffer[i - 1] == base->Input->keyBuffer[i]) {
				//make this not stupid
			}

			else if (Input->csharpKeyCallback) {
				Input->csharpKeyCallback(base->Input->keyBuffer[i].btn, base->Input->keyBuffer[i].action, 1);
			}			
		}
		base->Input->keyBuffer[i].read = true;
	}
	if (base->Input->mouseMoveFlag) {
		base->Input->mouseMoveFlag = false;
		Input->csharpMouseMoveCallback();
	}
	base->mainloop(render);
}

bool GLDrawerCLR::InputManagerCLR::getKey(char key) {
	return im->getKey(key);
}
bool GLDrawerCLR::InputManagerCLR::getKeyDown(char key) {
	return im->getKeyDown(key);
}
bool GLDrawerCLR::InputManagerCLR::getKeyUp(char key) {
	return im->getKeyUp(key);
}
bool GLDrawerCLR::InputManagerCLR::getKey(int key) {
	return im->getKey(key);
}
bool GLDrawerCLR::InputManagerCLR::getKeyDown(int key) {
	return im->getKeyDown(key);
}
bool GLDrawerCLR::InputManagerCLR::getKeyUp(int key) {
	return im->getKeyUp(key);
}

bool GLDrawerCLR::InputManagerCLR::getMouse(int btn) {
	return im->getMouse(btn);
}
bool GLDrawerCLR::InputManagerCLR::getMouseDown(int btn) {
	return im->getMouseDown(btn);
}
bool GLDrawerCLR::InputManagerCLR::getMouseUp(int btn) {
	return im->getMouseUp(btn);
}
GLDrawerCLR::vec2^ GLDrawerCLR::InputManagerCLR::getMousePos() {
	return gcnew vec2(im->getMousePos());
}

void cppKeyCallback(GLFWwindow * window, int key, int scancode, int action, int mods) {
	if (GLDrawerCLR::InputManagerCLR::csharpKeyCallback) {
		GLDrawerCLR::InputManagerCLR::csharpKeyCallback(key, action, scancode);
	}
}
void GLDrawerCLR::InputManagerCLR::setKeyCallback(keyCallback^ fp) {
	csharpKeyCallback = fp;
}
void GLDrawerCLR::InputManagerCLR::setMouseCallback(mouseCallback^ fp) {
	csharpMouseCallback = fp;
}
void GLDrawerCLR::InputManagerCLR::setMouseMoveCallback(mouseMoveCallback^ fp) {
	csharpMouseMoveCallback = fp;
}
GLDrawerCLR::InputManagerCLR::InputManagerCLR(GLCanvas * base) {
	im = new InputManager();
	base->Input = im;
	link = base;
}