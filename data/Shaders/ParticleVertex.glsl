#version 330 core

layout (location = 1) in vec3 aPos;
layout (location = 2) in vec3 aOffset;
layout (location = 3) in vec4 aColor;
layout (location = 4) in vec2 UVoffset;

uniform vec2 position;
uniform vec2 iResolution;
uniform float UVScale;
uniform mat4 xform;

out vec2 frag_uv;
out vec4 Color;

void main()
{
    //scale
    vec2 pos = vec2(aPos.x, aPos.y) * ( vec2(aOffset.z * 2.0) / (iResolution / 2.0));
    //local translation
    pos += aOffset.xy / (iResolution / 2.0);
    //global translation
    pos += position / (iResolution / 2.0);

    vec4 ndcPos = vec4(pos.x, pos.y, 0.0, 1.0);  
    
    //if no texture
    if(UVScale == 0)
        frag_uv = aPos.xy * 0.5 + 0.5;
    else
        frag_uv = aPos.xy * (UVScale / 2) + vec2(1.0 - UVoffset.x, UVoffset.y);
    Color = aColor;
   gl_Position = ndcPos;
}
