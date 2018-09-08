#include "stdafx.h"
#include <gl3w/gl3w.h>
#include "Gl3DrawerCLR.h"
#include <link.cpp>
#include <Input.cpp>
#include <GLFW/glfw3.h>
#include <glm/glm.hpp>
using namespace std;

GL3DrawerCLR::GLDWrapper::GLDWrapper(bool packShaders) {
	base = new BaseGLD();
	base->usePackedShaders = packShaders; //the fininall DLL need to be portable. Set this to true on export
	Input = gcnew InputManagerCLR(base);	
}
int GL3DrawerCLR::GLDWrapper::createCanvas(int width, int height, bool borderd, RGBA ^ backColor, bool Vsync) { 
	cout << "Running window from C# wrapper \n";
    return base->createCanvas(width, height, !borderd, vec3(backColor->r, backColor->g, backColor->b), Vsync);
}
void GL3DrawerCLR::Rect::dispose() {
	delete r;
}
void GL3DrawerCLR::GLDWrapper::cleaarNullRects() {
	base->removeNullShapes();
}
void GL3DrawerCLR::GLDWrapper::removeRect(Rect ^ r) {
	base->removeRect(r->r);
}
void GL3DrawerCLR::GLDWrapper::swapOrder(int a, int b) {
	base->swapOrder(a, b);
}
void GL3DrawerCLR::GLDWrapper::setPos(int x, int y) {
	base->setPos(x, y);
}
void GL3DrawerCLR::GLDWrapper::setVisible(bool visible) {
	base->setVisible(visible);
}
void GL3DrawerCLR::GLDWrapper::focusWindow() {
	base->focus();
}
int GL3DrawerCLR::GLDWrapper::addRect(Rect ^ r) {
	base->addRect(r->r);
	return 1; 
}

void GL3DrawerCLR::GLDWrapper::mainloop() {
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
	base->mainloop();
}

bool GL3DrawerCLR::InputManagerCLR::getKey(char key) {
	return im->getKey(key);
}
bool GL3DrawerCLR::InputManagerCLR::getKeyDown(char key) {
	return im->getKeyDown(key);
}
bool GL3DrawerCLR::InputManagerCLR::getKeyUp(char key) {
	return im->getKeyUp(key);
}
bool GL3DrawerCLR::InputManagerCLR::getKey(int key) {
	return im->getKey(key);
}
bool GL3DrawerCLR::InputManagerCLR::getKeyDown(int key) {
	return im->getKeyDown(key);
}
bool GL3DrawerCLR::InputManagerCLR::getKeyUp(int key) {
	return im->getKeyUp(key);
}

bool GL3DrawerCLR::InputManagerCLR::getMouse(int btn) {
	return im->getMouse(btn);
}
bool GL3DrawerCLR::InputManagerCLR::getMouseDown(int btn) {
	return im->getMouseDown(btn);
}
bool GL3DrawerCLR::InputManagerCLR::getMouseUp(int btn) {
	return im->getMouseUp(btn);
}
GL3DrawerCLR::vec2^ GL3DrawerCLR::InputManagerCLR::getMousePos() {
	return gcnew vec2(im->getMousePos());
}

void cppKeyCallback(GLFWwindow * window, int key, int scancode, int action, int mods) {
	if (GL3DrawerCLR::InputManagerCLR::csharpKeyCallback) {
		GL3DrawerCLR::InputManagerCLR::csharpKeyCallback(key, action, scancode);
	}
}
void GL3DrawerCLR::InputManagerCLR::setKeyCallback(keyCallback^ fp) {
	csharpKeyCallback = fp;
}
void GL3DrawerCLR::InputManagerCLR::setMouseCallback(mouseCallback^ fp) {
	csharpMouseCallback = fp;
}
void GL3DrawerCLR::InputManagerCLR::setMouseMoveCallback(mouseMoveCallback^ fp) {
	csharpMouseMoveCallback = fp;
}
GL3DrawerCLR::InputManagerCLR::InputManagerCLR(BaseGLD * base) {
	im = new InputManager();
	base->Input = im;
	link = base;
}