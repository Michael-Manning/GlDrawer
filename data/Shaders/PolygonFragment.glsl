#version 330 core

#define PI 3.14159265359
#define TWO_PI 6.28318530718

in vec2 frag_uv;
uniform vec4 Color;
uniform vec4 bordColor;
uniform vec2 shapeScale;
uniform int sideCount;
uniform float bordWidth;
uniform float zoom;
uniform float iTime;
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


mat2 rot(float a){
    return mat2(
        sin(a),cos(a),
        cos(a),-sin(a)
        );
}


void main()
{
    float xblur = 1.5/shapeScale.x;
	float yblur = 1.5/shapeScale.y;
    
    vec4 FillColor = Color;
    vec4 BorderColor = bordColor;
    if(Color.a < 0.0)
    {
        if(Color.a == -1.0)
            FillColor = vec4(0.5 + 0.5*cos(iTime * 1.4 +(frag_uv).xyx+vec3(0,2,4)), 1.0);
        if(Color.a == -2.0){
            vec2 uv = vec2(1.0) * ((frag_uv * zoom) * shapeScale.xy) * rot(-0.78539816339);
            vec3 hc = mix(vec3(0.5), vec3(1), step(0.5, sin((uv.x + iTime * 0.006) *800.0)+0.5));
            FillColor = vec4(hc, 1.0);
        }
    }

    if(bordColor.a < 0.0)
    {
        if(bordColor.a == -1.0)
            BorderColor = vec4(0.5 + 0.5*cos((iTime + 300.0) * 1.4 +(frag_uv).xyx+vec3(0,2,4)), 1.0);
        if(Color.a == -2.0){
            vec2 uv = vec2(1.0) * (frag_uv * zoom) * rot(-0.78539816339);
            vec3 hc = mix(vec3(0.5), vec3(1), step(0.5, sin((uv.x * + (iTime + 20.0) * 0.2) *40.0)+0.5));
            BorderColor  = vec4(hc, 1.0);
        }
    }

    
    if(sideCount == 4){
        vec2 uv =  abs(frag_uv- 0.5) * 2.0;
        float af = fwidth(max(uv.x, uv.y)); 
        float f = smoothstep(1.0, 1.0 - af, max(uv.x, uv.y));
        vec4 c = vec4(BorderColor.xyz, BorderColor.w - (1.0 - f));

        float bordX = (bordWidth / shapeScale.x) * 2.0;
        float bordy = (bordWidth / shapeScale.y) * 2.0;
        f = smoothstep(1.0 - bordX, 1.0 - bordX - af, uv.x);
        f *= smoothstep(1.0 - bordy, 1.0 - bordy - af, uv.y);

        FragColor = mix(c, FillColor, f);
        return;
    }
    if(sideCount == 1){      
        float f; 
        float af = fwidth(length(frag_uv - 0.5));  
        vec4 c = vec4(FillColor.xyz, 0.0);
        if(bordWidth > 0){         
            f = smoothstep(0.5, 0.5 - af, length(frag_uv - 0.5));
            c = vec4(BorderColor.xyz, BorderColor.w - (1.0 - f));
        }

        float bord = bordWidth / shapeScale.x;
        f = smoothstep(0.5 - bord, 0.5 - bord - af, length(frag_uv - 0.5));
        FragColor = mix(c, FillColor, f);
        return;
    }

    float yoff = 0.5;
    float rad = 0.5;
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

    FragColor = c;
}
