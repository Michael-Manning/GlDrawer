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
#include "link.h"

using namespace std;
using namespace glm;


int main()
{
	cout << "Running from native C++ \n";

	BaseGLD base;
	base.Input = &InputManager();
	if (base.createCanvas(1300, 900, true, vec3(0.3f)))
		cout << "canvas creation failed\n";
	
	//BaseGLD base2;
//	base2.Input = &InputManager();
	//base2.createCanvas(1000, 800, true, vec3(0.5f));

	rect a(vec2(300, 100), vec2(100, 200), 0, vec4(1, 0, 0, 1), vec4(), 0, 0);
	rect b(vec2(100, 100), vec2(100, 200), 0, vec4(0, 0, 1, 1), vec4(), 0, 0);
	rect c("G:\\pictures\\funny stuff\\arthur-this-isnt-weed.jpg", vec2(100, 800), vec2(300, 300), 0, vec4(1, 0, 0, 1));
	rect f("C:\\Users\\Micha\\Desktop\\OpenGL3\\OpenGL3\\bin\\test_cat.png", vec2(400, 800), vec2(300, 300), 0, vec4(1, 0, 0, 1));
	rect d("c:\\windows\\fonts\\times.ttf", "Times New Roman", 6, vec2(250, 550), vec2(50));

	rect e("c:\\windows\\fonts\\comic.ttf", "SAANS", 6, vec2(200, 400), vec2(30));
	rect g(vec2(250, 550), vec2(406, 50), 0, vec4(0.5), vec4(), 0, 0);
	
	base.addRect(&g);
	base.addRect(&a);
	base.addRect(&b);
	base.addRect(&c);
	base.addRect(&d);
	base.addRect(&e);
	base.addRect(&f);
//	base2.addRect(&a);
//	base2.addRect(&b);
//	base2.addRect(&c);
	while (true) {
		base.mainloop();
	//	base2.mainloop();
	}
	cin.get();


	return 0;
}