#version 330 core
layout (location = 1) in vec3 aPos;
layout (location = 5) in vec4 aTrans;
layout (location = 6) in vec4 uvOff;

uniform float aspect;
uniform vec2 scaleOffset; //UVs
uniform vec2 scale; //UVs
uniform mat4 xform;
uniform vec2 zoom;
uniform vec2 position;
uniform float rotation;

uniform vec2 posOffset;
out vec2 frag_uv;

mat4 localTransform(){
    //local letter position
    float lx = aTrans.x / aspect;
    float ly = aTrans.y;

    //local letter scale
    float sx = aTrans.z / aspect;
    float sy = aTrans.a;

    mat4 localTranslation = 
    mat4(1.0, 0.0, 0.0,  0.0,
         0.0, 1.0, 0.0,  0.0,
         0.0, 0.0, 1.0,  0.0,
         lx,  ly,  0.0,  1.0);
    mat4 scale = 
    mat4(sx,  0.0, 0.0,  0.0,
         0.0, sy * ((zoom.y < 0.0) ? -1.0 : 1.0),  0.0,  0.0,
         0.0, 0.0, 1.0,  0.0,
         0.0, 0.0, 0.0,  1.0);
    return (localTranslation * scale);
}

mat4 globalTransform(){     
    //global shape position
    float px = position.x; 
    float py = position.y;

    float s = sin(-rotation);
    float c = cos(-rotation);

mat4 globalTranslation = 
    mat4(1.0, 0.0, 0.0,  0.0,
         0.0, 1.0, 0.0,  0.0,
         0.0, 0.0, 1.0,  0.0,
         px,  py,  0.0,  1.0);

    //aspect scale
mat4 Ascale = 
    mat4(aspect, 0.0,  0.0,  0.0,
         0.0, 1.0,  0.0,  0.0,
         0.0, 0.0,  1.0,  0.0,
         0.0, 0.0,  0.0,  1.0);
    //global rotation
mat4 rotation = 
    mat4(c,  -s,  0.0, 0.0,
        s,   c,  0.0, 0.0,
        0.0, 0.0, 1.0, 0.0,
        0.0, 0.0, 0.0, 1.0);
   return (globalTranslation * Ascale * rotation);
}

void main()
{

mat4 camZoom = mat4( zoom.x,  0.0,  0.0, 0.0,
                     0.0, zoom.y,  0.0, 0.0,
                     0.0, 0.0, 1.0, 0.0,
                     0.0, 0.0, 0.0, 1.0);

    mat4 globalT;
    if(xform[0][0] == 0)
        globalT = globalTransform();
    else
        globalT = xform;

   vec4 ndcPos = camZoom * globalT * localTransform() * vec4(aPos.x, aPos.y, aPos.z, 1.0);
        frag_uv = aPos.xy * (uvOff.zw) + (uvOff.xy);
   gl_Position = ndcPos;
}