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

float aastep(float threshold, float value) {
  #ifdef GL_OES_standard_derivatives
    float afwidth = length(vec2(dFdx(value), dFdy(value))) * 0.70710678118654757;
    return smoothstep(threshold-afwidth, threshold+afwidth, value);
  #else
    return step(threshold, value);
  #endif  
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

    //rect
    if(sideCount == 4){
        float bordX = (bordWidth / shapeScale.x) * 1.0;
        float bordy = (bordWidth / shapeScale.y) * 1.0;

        float vx = 1.0 / shapeScale.x;
        float vy = 1.0 / shapeScale.y;
        vec2 uv = vec2(aastep(vx, frag_uv.x), aastep(vx, frag_uv.y));
        uv *= vec2(aastep(vy, 1.0 - frag_uv.x), aastep(vy, 1.0 - frag_uv.y));    
        float f = uv.x * uv.y;

        vec4 c = vec4(BorderColor.xyz, BorderColor.w - (1.0 - f));

        uv = vec2(aastep( bordX, frag_uv.x), aastep(bordy, frag_uv.y));
        uv *= vec2(aastep(bordX, 1.0 - frag_uv.x), aastep(bordy, 1.0 - frag_uv.y));
        f = uv.x * uv.y;

        FragColor = mix(c, FillColor, f);
        return;
    }

    //elipse
    if(sideCount == 1){      
        float f; 
        float af = fwidth(length(frag_uv - 0.5));  
        vec4 c = vec4(FillColor.xyz, 0.0);
        if(bordWidth > 0){         
            f = 1.0 - aastep(0.5,  length(frag_uv - 0.5));  //f = smoothstep(0.5, 0.5 - af, length(frag_uv - 0.5));
            c = vec4(BorderColor.xyz, BorderColor.w - (1.0 - f));
        }

        float bord = bordWidth / shapeScale.x;
        f = 1.0 - aastep(0.5 - bord, length(frag_uv - 0.5)); //smoothstep(0.5 - bord, 0.5 - bord - af, length(frag_uv - 0.5));
        FragColor = mix(c, FillColor, f);
        return;
    }

    //polygon
    // float yoff = 0.5;
    // float rad = 0.5;
    // if(sideCount < 33){
    //     yoff = polyOffs[sideCount -3];
    //     rad = polyRads[sideCount -3];
    // }

    // vec2 st = frag_uv - vec2(0.5, yoff);//(frag_uv - 0.5) * 2.0;
    // float angle = atan(st.x,st.y) + PI;
    // float slice = TWO_PI / sideCount;
    // float f = aastep(rad, cos(floor(0.5 + angle / slice ) * slice - angle) * length(st));

    // vec4 c = vec4(BorderColor.xyz, BorderColor.w - (1.0 - f));
    // rad -= bordWidth / shapeScale.x;
    
    // angle = atan(st.x,st.y) + PI;
    // slice = TWO_PI / sideCount;
    // f = aastep(rad, cos(floor(0.5 + angle / slice ) * slice - angle) * length(st));
    // vec4 d = vec4(FillColor.xyz, FillColor.w - (1.0 - f));
    // c = mix(c, d,  -f);
    
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
