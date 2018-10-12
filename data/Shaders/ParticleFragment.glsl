#version 330 core

in vec2 frag_uv;
in vec4 Color;
uniform sampler2D Text;
out vec4 FragColor;
uniform float UVScale;

void main()
{
    if(UVScale ==0){
        float f =  smoothstep(0.0, 0.5, length(frag_uv  -0.5));
        vec4 c = vec4(Color.xyz, 1.0 -f);//-f + FillColor.w);
        FragColor = c;
        return;
    }
    FragColor = vec4(texture(Text, frag_uv));
}

