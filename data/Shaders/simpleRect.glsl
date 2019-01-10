#version 330 core
layout (location = 0) in vec3 aPos;
uniform float aspect;
out vec2 frag_uv;

void main()
{
    vec4 ndcPos =  vec4(aPos.x , aPos.y, aPos.z, 1.0);    
    frag_uv = aPos.xy *0.5 + 0.5; 
   gl_Position = ndcPos;
}