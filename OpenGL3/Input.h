#pragma once
#include <GLFW/glfw3.h>
#include <glm/glm.hpp>
#include <iostream>

using namespace glm;
using namespace std;

enum Skey { MouseLeft = 0, MouseRight = 1 };
struct description {
	int btn, action, read;
	bool operator==(const description& b) {
		return (action == b.action && btn == b.btn);
	}
};



class InputManager {
public: 
	GLFWwindow * window;

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

	InputManager() {
		fill(keyStates, keyStates + 348, 2);
		LeftMouseState = 2;
		RightMouseState = 2;
		for (int i = 0; i < 10; i++)
		{
			keyBuffer[i].read = true;
			mouseBuffer[i].read = true;
		}
	}
};


