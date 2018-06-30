#include "Input.h"
#include <GLFW/glfw3.h>
#include <glm/glm.hpp>
#include <ctype.h>
#include <iostream>

using namespace glm;
using namespace std;


void InputManager::onKeyboard(int key, int scancode, int action, int mods) {
	//Likely redundant
	if (keyStates[key] == action)
		return;
	for (int i = 0; i < 10; i++)
		if (keyBuffer[i].read)
			keyBuffer[i] = description{key,action, false };
	keyStates[key] = action;
}
void InputManager::onMouse( int button, int action, int mods) {
	if (button == MouseLeft)
		LeftMouseState = action;
	else if (button == MouseRight)
		RightMouseState = action;
	for (int i = 0; i < 10; i++)
		if (mouseBuffer[i].read)
			mouseBuffer[i] = description{ button,action, false };
}
void InputManager::onCursor() {
	mouseMoveFlag = true;
}

void InputManager::clearStates() {
	fill(keyStates, keyStates + 348, 2);
	LeftMouseState = 2;
	RightMouseState = 2;
}

bool InputManager::getKey(char key) {
	return glfwGetKey(window, toupper(key));
}
bool InputManager::getKeyDown(char key) {
	return (keyStates[toupper(key)] == 1);
}
bool InputManager::getKeyUp(char key) {
	return (keyStates[toupper(key)] == 0);
}
//same thing but int32
bool InputManager::getKey(int key) {
	return glfwGetKey(window, key);
}
bool InputManager::getKeyDown(int key) {
	return (keyStates[key] == 1);
}
bool InputManager::getKeyUp(int key) {
	return (keyStates[key] == 0);
}

vec2 InputManager::getMousePos() {
	double x, y;
	glfwGetCursorPos(window, &x, &y);
	return vec2((float)x, (float)y);
}
bool InputManager::getMouse(int button) {
	return(glfwGetMouseButton(window, button) == 1);
}
bool InputManager::getMouseDown(int button) {
	
	if (button == MouseLeft)
		return (LeftMouseState == 1);
	 if (button == MouseRight)
		return (RightMouseState == 1);
	return false;
}
bool InputManager::getMouseUp(int button) {
	if (button == MouseLeft)
		return (LeftMouseState == 0);
	else if (button == MouseRight)
		return (RightMouseState == 0);
	return false;
}