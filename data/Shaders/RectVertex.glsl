#version 330 core
layout (location = 0) in vec3 aPos;
uniform float aspect;
uniform mat4 xform;
uniform vec2 scale;
uniform vec2 position;
uniform float rotation;
uniform float zoom;
out vec2 frag_uv;

mat4 trans(){
    float sx = scale.x;
    float sy = scale.y;
     
    float px = position.x;
    float py = position.y;

    float s = sin(-rotation);
    float c = cos(-rotation);

    //translation
mat4 tran = mat4(1.0, 0.0, 0.0,  0.0,
                 0.0, 1.0, 0.0,  0.0,
                 0.0, 0.0, 1.0,  0.0,
                 px,  py,  0.0,  1.0);

    //aspect scale
mat4 ascal = mat4(aspect, 0.0,  0.0,  0.0,
                     0.0, 1.0,  0.0,  0.0,
                     0.0, 0.0,  1.0,  0.0,
                     0.0, 0.0,  0.0,  1.0);

    //scale
mat4 scal = mat4(sx,  0.0, 0.0,  0.0,
                 0.0, sy,  0.0,  0.0,
                 0.0, 0.0, 1.0,  0.0,
                 0.0, 0.0, 0.0,  1.0);
    //rotation
mat4 rot = mat4( c,  -s,  0.0, 0.0,
                 s,   c,  0.0, 0.0,
                0.0, 0.0, 1.0, 0.0,
                0.0, 0.0, 0.0, 1.0);
    return (tran * ascal * rot * scal);
}

void main()
{
    vec4 ndcPos;

mat4 camZoom = mat4( zoom,  0.0,  0.0, 0.0,
                     0.0,   zoom,  0.0, 0.0,
                     0.0, 0.0, 1.0, 0.0,
                     0.0, 0.0, 0.0, 1.0);

    if(xform[0][0] == 0)
        ndcPos = camZoom * trans() * vec4(aPos.x / aspect, aPos.y, aPos.z, 1.0);    
    else
        ndcPos =  (camZoom * xform * vec4(aPos.x / aspect, aPos.y, aPos.z, 1.0));

    frag_uv = aPos.xy *0.5 + 0.5; 
   gl_Position = ndcPos;
}