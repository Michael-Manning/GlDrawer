#version 330 core

#define PI 3.14159265359
#define TWO_PI 6.28318530718

in vec2 frag_uv;
uniform vec4 Color;
uniform vec4 bordColor;
uniform vec2 shapeScale;
uniform int sideCount;
uniform float bordWidth;
uniform float iTime;
uniform sampler2D Text;
out vec4 FragColor;

//scaling and position a #sided polygon precisely within a square based off an equation is impossible, but I've hard coded the first 32
//https://www.desmos.com/calculator/rjo0sbq3wh
float polyOffs[30] = float[] (0.36, 0.5, 0.45, 0.5, 0.475,0.5, 0.484, 0.5,
                          0.49, 0.5, 0.492, 0.5, 0.495, 0.5, 0.495, 0.5,
                          0.497, 0.5, 0.497, 0.5, 0.498, 0.5, 0.498, 0.5,
                          0.499, 0.5, 0.499, 0.5, 0.499, 0.5);

float polyRads[30] = float[] (0.289, 0.5, 0.423, 0.433, 0.465, 0.5, 0.476, 0.475, 
                         0.484, 0.5, 0.489, 0.487, 0.492, 0.5, 0.493, 0.492, 
                         0.494, 0.5, 0.495, 0.495, 0.496, 0.5, 0.497, 0.497,
                         0.498, 0.5, 0.498, 0.498, 0.498, 0.5);


void main()
{
    float xblur = 1.5/shapeScale.x;
	float yblur = 1.5/shapeScale.y;
    
    vec4 FillColor = Color;
    vec4 BorderColor = bordColor;
    if(Color.w == -1.0){
        FillColor = vec4(0.5 + 0.5*cos(iTime * 1.4 +(frag_uv).xyx+vec3(0,2,4)), 1.0);
    }
    if(bordColor.w == -1.0){
        BorderColor = vec4(0.5 + 0.5*cos((iTime + 300.0) * 1.4 +(frag_uv).xyx+vec3(0,2,4)), 1.0);
    }
    
    if(sideCount == 4){
        float f =  smoothstep(1.0 - xblur, 1.0, frag_uv.x);
        f += smoothstep(xblur, 0.0, frag_uv.x);
        f +=  smoothstep(1.0 - yblur, 1.0, frag_uv.y);
        f += smoothstep(yblur, 0.0, frag_uv.y);

        vec4 c = vec4(FillColor.xyz, -f + FillColor.w);

        float bx = (bordWidth / shapeScale.x);
        float by = (bordWidth / shapeScale.y);
        if(bordWidth > 0){
            f =  smoothstep(1.0 - xblur - bx, 1.0 - bx, frag_uv.x);
            f += smoothstep(xblur+ bx, 0.0 + bx, frag_uv.x);
            f +=  smoothstep(1.0 - by - yblur, 1.0 - by, frag_uv.y);
            f += smoothstep(yblur + by, 0.0 + by, frag_uv.y);
            
            f = clamp(f, 0.0, 1.0);
            c = mix(c, BorderColor, f);
        }
        
        FragColor = vec4(c);
        return;
    }
    if(sideCount == 1){

		float f =  smoothstep(0.5 - xblur, 0.5, length(frag_uv  -0.5));
        vec4 c = vec4(FillColor.xyz, -f + FillColor.w);

        f =  smoothstep(0.5 - (bordWidth/shapeScale.x) - xblur, 0.5 - (bordWidth/shapeScale.x), length(frag_uv  -0.5));
        f -=  smoothstep(0.5 - xblur, 0.5, length(frag_uv  -0.5));
        c = mix(c, BorderColor, f);

		FragColor = c;
        return;
    }
    if(sideCount == 0){
         FragColor = vec4(texture(Text, frag_uv));
         return;
    }
    if(sideCount == -1){
        FragColor = vec4(FillColor.xyz, texture(Text, frag_uv).x - (1.0-FillColor.w));
        return;
    }

    float yoff;
    float rad;
    if(sideCount < 33){
        yoff = polyOffs[sideCount -3];
        rad = polyRads[sideCount -3];
    }

    vec2 uv = frag_uv - vec2(0.5, yoff);

    float a = atan(uv.x,uv.y)+PI;
    float r = TWO_PI/float(sideCount);
    float d = cos(floor(.5+a/r)*r-a)*length(uv);
    float f = -smoothstep(rad- xblur, rad,d);

    vec4 c = vec4(BorderColor.xyz, 1.0 + f);

    rad -= bordWidth / shapeScale.x;

    a = atan(uv.x,uv.y)+PI;
    r = TWO_PI/float(sideCount);
    d = cos(floor(.5+a/r)*r-a)*length(uv);
    f = -smoothstep(rad- xblur, rad,d);
    c = mix(c, FillColor, 1.0 + f);

    FragColor =c ;
}
