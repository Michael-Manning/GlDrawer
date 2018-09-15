#version 330 core
uniform vec4 Color;
in vec2 frag_uv;
out vec4 FragColor;
uniform vec2 shapeScale;
void main()
{
	float xblur = 2.5/shapeScale.x;
	float yblur = 2.5/shapeScale.y;

	float f =  smoothstep(1.0 - xblur, 1.0, frag_uv.x);
	f += smoothstep(xblur, 0.0, frag_uv.x);
	f +=  smoothstep(1.0 - yblur, 1.0, frag_uv.y);
	f += smoothstep(yblur, 0.0, frag_uv.y);

   FragColor = vec4(Color.xyz,  -f + 1.0);
};
e