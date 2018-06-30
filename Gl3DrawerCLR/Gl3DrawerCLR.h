#pragma once

#include<gl3w/gl3w.h>
#include<GL/GLU.h>
#include<GLFW/glfw3.h>
#include<OpenGL3/link.h>
#include<OpenGL3/Input.h>
#include <glm/glm.hpp>
#include<Windows.h>

using namespace std;
using namespace System;
using namespace System::Runtime::InteropServices;

namespace GL3DrawerCLR {
	public ref struct vec2 {
	public:
		float x, y;
		//implicit conversion to glm vec2
		operator glm::vec2() { return glm::vec2(x, y); }
		vec2(float X, float Y) {
			x = X;
			y = Y;
		}
		vec2() {
			x = 0;
			y = 0;
		}
		vec2(glm::vec2 v2) {
			x = v2.x;
			y = v2.y;
		}
		vec2(vec2 % v) {
			x = v.x;
			y = v.y;
		}
	};

	public ref struct RGBA {
		float r, g, b, a;
		//implicit conversion to glm vec4
		operator glm::vec4() { return glm::vec4(r,g,b,a); }
		RGBA(float R, float G, float B, float A) {
			r = R;
			g = G;
			b = B;
			a = A;
		}
		RGBA(vec4 v4) {
			r = v4.x;
			g = v4.y;
			b = v4.z;
			a = v4.w;
		}
		RGBA(RGBA % coppier) {
			r = coppier.r;
			g = coppier.g;
			b = coppier.b;
			a = coppier.a;
		}
	};
	public ref struct Rect {
	public:
		property vec2 ^ Pos {
			vec2 ^ get() {
				return gcnew vec2(r->pos);
			}
			void set(vec2 ^ v) {
				r->pos = v;
			}
		}
		property vec2 ^ Scale {
			vec2 ^ get() {
				return gcnew vec2(r->scale);
			}
			void set(vec2 ^ value) {
				r->scale = value;
			}
		}
		property float Angle {
			float get() {
				return r->angle;
			}
			void set(float value) {
				r->angle = value;
			}
		}		
		property RGBA ^ Color {
			RGBA ^ get() {
				return gcnew RGBA(r->color);
			}
			void set(RGBA ^ value) {
				r->color = value;
			}
		}
		property RGBA ^ BorderColor {
			RGBA ^ get() {
				return gcnew RGBA(r->borderColor);
			}
			void set(RGBA ^ value) {
				r->borderColor = value;
			}
		}
		property float BordWidth {
			float get() {
				return r->borderW;
			}
			void set(float f) {
				r->borderW = f;
			}
		}
		property float rSpeed {
			float get() {
				return r->rotSpeed;
			}
			void set(float value) {
				r->rotSpeed = value;
			}
		}
		property bool hidden {
			bool get() {
				return r->hidden;
			}
			void set(bool value) {
				r->hidden = value;
			}
		}
		Rect(vec2 ^ pos, vec2 ^ scale, float angle, RGBA ^ color, RGBA ^ borderColor, float borderWidth, float rotationSpeed) {
			r = new rect(pos, scale, angle, color, borderColor, borderWidth, rotationSpeed);
		}
		Rect(vec2 ^ pos, vec2 ^ scale, float angle, RGBA ^ color, RGBA ^ borderColor, float borderWidth, float rotationSpeed, int sides) {
			r = new rect(pos, scale, angle, color, borderColor, borderWidth, rotationSpeed, sides);
		}
		//As texture
		Rect(System::String ^ filePath, vec2 ^ pos, vec2 ^ scale, float angle, RGBA ^ color, RGBA ^ borderColor, float borderWidth, float rotationSpeed) {
			const char* cpath = (const char*)(Marshal::StringToHGlobalAnsi(filePath)).ToPointer();
			r = new rect(cpath, pos, scale, angle, color, borderColor, borderWidth, rotationSpeed);
		}

		void dispose();
		int index;
		rect* r;
	};

	public delegate void keyCallback(int, int, int);
	public delegate void mouseCallback(int,int,int);
	public delegate void mouseMoveCallback();
	public ref class InputManagerCLR {
	public:
		InputManagerCLR(BaseGLD * base);
		
		void setKeyCallback(keyCallback^ fp);
	    static keyCallback^ csharpKeyCallback;
		void setMouseCallback(mouseCallback^ fp);
	    mouseCallback^ csharpMouseCallback;
		void setMouseMoveCallback(mouseMoveCallback^ fp);
		mouseMoveCallback^ csharpMouseMoveCallback;

	    property int LeftMouseState {int get() { return im->LeftMouseState; }}
		property int RightMouseState {int get() { return im->RightMouseState; }}	

		bool getKey(char key);
		bool getKeyDown(char key);
		bool getKeyUp(char key);

		bool getKey(int key);
		bool getKeyDown(int key);
		bool getKeyUp(int key);

		bool getMouse(int button);
		bool getMouseDown(int button);
		bool getMouseUp(int button);
		vec2^ getMousePos();

	private:
		InputManager * im;
		BaseGLD * link;
	};
	public ref class GLDWrapper
	{
	public:
		GLDWrapper();
		InputManagerCLR ^ Input;
		int createCanvas(int width, int height, bool borderd, RGBA ^ backColor);
		void setPos(int x, int y);
		void setVisible(bool visible);
		void focusWindow();
		int addRect(Rect ^);
		void mainloop();
		void cleaarNullRects();
		void removeRect(Rect ^ r);
		void swapOrder(int indexA, int indexB);
		
	private:
		BaseGLD * base;
	};
}
