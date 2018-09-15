#version 330 core
uniform vec4 Color;
in vec2 frag_uv;
out vec4 FragColor;
uniform vec2 shapeScale;
uniform float bordWidth;

void main()
{
	if(bordWidth == 0.0){
		float xblur = 2.5/shapeScale.x;
		float f =  smoothstep(0.5 - xblur, 0.5, length(frag_uv  -0.5));
		FragColor = vec4(Color.xyz, -f + 1.0);
	}
	else{
		float xblur = 2.5/shapeScale.x;
	    float f = 0.0;
        f =  smoothstep(0.5 - (bordWidth/shapeScale.x) - xblur, 0.5 - (bordWidth/shapeScale.x), length(frag_uv  -0.5));
        f -=  smoothstep(0.5 - xblur, 0.5, length(frag_uv  -0.5));
		FragColor = vec4(0.0,0.0,1.0, f);
	}
}
