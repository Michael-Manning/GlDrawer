#version 330 core

layout (location = 1) in vec3 aPos;
layout (location = 2) in vec3 aOffset;
layout (location = 3) in vec4 aColor;

out vec2 frag_uv;
out vec4 Color;

void main()
{
  // vec4 ndcPos = aOffset.z * vec4(aPos.x, aPos.y, aPos.z, 1.0) + aOffset.xy;
    vec4 ndcPos =  vec4(aPos.x * 0.1, aPos.y * 0.1, 0.0, 1.0)+ vec4(aOffset.x, aOffset.y, 0.0, 0.0);
        frag_uv = aPos.xy *0.5 + 0.5;
        Color = aColor;
   gl_Position = ndcPos;
}
