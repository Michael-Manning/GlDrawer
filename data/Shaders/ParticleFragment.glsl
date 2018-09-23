#version 330 core

in vec2 frag_uv;
in vec4 Color;
out vec4 FragColor;

void main()
{
    float f =  smoothstep(0.0, 0.1, length(frag_uv  -0.5));
    vec4 c = vec4(Color.xyz, 1.0 -f);//-f + FillColor.w);

    FragColor = c;
}

