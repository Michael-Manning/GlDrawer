#version 330 core

layout (location = 1) in vec3 aPos;
layout (location = 2) in vec3 aOffset;
layout (location = 3) in vec4 aColor;

uniform vec2 position;
uniform vec2 iResolution;

out vec2 frag_uv;
out vec4 Color;

void main()
{
    vec2 pos = vec2(aPos.x, aPos.y) * ( vec2(aOffset.z * 2.0) / (iResolution / 2.0));
    pos += aOffset.xy / (iResolution / 2.0);
    pos += position / (iResolution / 2.0);
    vec4 ndcPos = vec4(pos.x, pos.y, 0.0, 1.0);  // vec4(aPos.x * aOffset.z, aPos.y * aOffset.z, 0.0, 1.0) + vec4(aOffset.x, aOffset.y, 0.0, 0.0);
        frag_uv = aPos.xy;// *0.5 + 0.5;
        Color = aColor;
   gl_Position = ndcPos;
}
