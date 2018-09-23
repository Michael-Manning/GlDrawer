 #define _CRT_SECURE_NO_WARNINGS

#include <stdio.h>
#include <tchar.h>
#include <math.h>
#include <assert.h>

#include <gl3w/gl3w.h>
#include <GL/GLU.h>
#include <GLFW/glfw3.h>
#include <glm/glm.hpp>
#include <glm/gtc/matrix_transform.hpp>
#include <glm/gtc/type_ptr.hpp>
//#define STB_IMAGE_IMPLEMENTATION
#include <stb/stb_image.h>


#include <iostream>
#include <string>
#include <fstream>
#include <sstream>
#include <vector>
#include "Input.h"
#include "engine.h"
#include <thread>   

using namespace std;
using namespace glm;
int runBenchmark();
int runFontBenchmark();

int main()
{
	cout << "Running from native C++ \n";

	//return runBenchmark();
	//return runFontBenchmark();

	GLCanvas base;
	base.Input = &InputManager();
	if (base.createCanvas(1300, 900, true, vec3(0.3f),  true))
		cout << "canvas creation failed\n";

	shape g(vec2(880, 450), vec2(655, 390), 0, vec4(0.5), vec4(), 0, 0);

	shape a(vec2(300, 100), vec2(100, 200), 0, vec4(1, 0, 0, -1), vec4(), 0, 0);
	shape b(vec2(100, 100), vec2(100, 200), 0, vec4(0, 0, 1, 1), vec4(), 0, 0);
	shape k(vec2(400, 100), vec2(100, 100), 0, vec4(0, 0, 1, -1), vec4(), 0, 0, 1);
	shape c("../data/images/flower.jpg", vec2(100, 800), vec2(300, 300), 0, vec4(1, 0, 0, 1));
	shape f("../data/images/test_cat.png", vec2(400, 800), vec2(300, 300), 0, vec4(1, 0, 0, 1));

	shape d("c:\\windows\\fonts\\times.ttf", "}Times New Roman\nyyyyyyyyyy\nyyyyyyyyyyyyyyyyyyyy\n123456789123456789123456789123456789\n\nSixth line\nseventh Line\neighth line", 6, vec2(400, 550), 50, 2, &g, 0, vec4(1));
	shape e("c:\\windows\\fonts\\comic.ttf", "SAANST{[\nfancy new line which is longer thst line\nmedium sized n}ew line", 6, vec2(300, 400), 30, 2, NULL, 0, vec4(1), vec4(), 0, 1);
	shape w(vec2(300, 400), vec2(480, 90), 0, vec4(0, 0, 0, 1), vec4(), 0, 0, 4);

	shape h(vec2(600, 100), vec2(100, 100), 0, vec4(1, 0, 0, 1), vec4(), 0, 0, 5);

	ParticleSystem p(10000);
	base.testP = &p;

	base.addShape(&g);
	base.addShape(&a);
	base.addShape(&b);
	base.addShape(&c);
	base.addShape(&d);
	base.addShape(&w);
	base.addShape(&e);
	base.addShape(&f);
	base.addShape(&k);
	base.addShape(&h);
	
	for (int j = 0; j < 900; j++)
	{
		for (int i = 0; i < 1300; i++)
		{
			float g = (float)j/ 900.0f ;
			float b = (float)i / 1300.0f ;
			base.setBBPixel(i, j, vec4(1, g, b, 1));
		}
	}

	while (!base.Cleaned) {
		base.mainloop();
		vec2 pos = base.Input->getMousePos();
		if (base.Input->getMouseDown(0)) {
			
			vec3 col = base.getPixel(pos.x, pos.y);
			cout << "\n" << col.r << " " << col.g << " " << col.b;
			col = col / 255.0f;	
		}
		base.setBBShape(shape(vec2(pos.x, -pos.y + 900), vec2(60.0, 60.0) + 30 * (float)(sin(base.currTime * 8) +1.0), 0, vec4(1, 0, 1, -1),vec4(), 0, 0, 1));
		//base.clearColorFlag = true;
	//	base.setBBShape(e);
	}
	return 0;
}

shape onScreen[10000];
int screenCount = 0;
GLCanvas can;
void renderLoop(GLCanvas * can) {
	while (!can->Cleaned) {
		can->mainloop();
	}
}
void add(shape r) {
	onScreen[screenCount] = r;
	can.addShape(onScreen + screenCount);
	screenCount++;
}
int runBenchmark() {


	can.Input = &InputManager();
	if (can.createCanvas(1000, 1000, true, vec3(0.2f), false)) {
		cout << "canvas creation failed\n";
		return 0;
	}
	
	int count = 100;
	add(shape(vec2(250, 250), vec2(500, 500),0, vec4(vec3(0.2), 1.0)));
	for (int i = 0; i < count; i++) {
		for (int j = 0; j < count; j++) {
			float scale = (1000 / count);
			add(shape(vec2(i * scale + scale/2, j * scale + scale/2), vec2(10, 10), 0, vec4(i / (float)count, 0.8, j / (float)count, 1), vec4(), 0, 5));
		}
	}
		
	while (!can.Cleaned) {
		can.mainloop();
	}
	return 1;
}

int runFontBenchmark() {
	can.Input = &InputManager();
	if (can.createCanvas(1000, 1000, true, vec3(0.2f), false)) {
		cout << "canvas creation failed\n";
		return 0;
	}
	//add(shape("c:\\windows\\fonts\\comic.ttf", "l", 0, vec2(500, 500 + 5), 10, 0, NULL, 0, vec4(1), vec4(), 0));
	for (int i = 0; i < 100; i++) {
		add(shape("c:\\windows\\fonts\\comic.ttf", "letters all day, letters all day, letters all day, letters all day, letters all day, letters all day, letters all day, letters all day,"
												  "letters all day, letters all day, letters all day, letters all day, letters all day, letters all day, letters all day, letters all day,", 0, vec2(500, i * 10 + 5), 10, 0, NULL, 0, vec4(1), vec4(), 0));
	}

	while (!can.Cleaned) {
		can.mainloop();
	}
	return 1;
}