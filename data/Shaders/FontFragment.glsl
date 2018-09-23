#version 330 core
in vec2 frag_uv;
uniform sampler2D Text;
uniform vec4 Color;
uniform float iTime;
out vec4 FragColor;
void main()
{ 
    vec4 FillColor = Color;
    if(Color.w == -1.0){
        FillColor = vec4(0.5 + 0.5*cos(iTime * 1.4 +(frag_uv).xyx+vec3(0,2,4)), 1.0);
    }
    FragColor =  vec4(FillColor.xyz, texture(Text, frag_uv).x - (1.0-FillColor.w));
};


//BENCHMARKS:
//40 ms: unchanged
//35 ms: removed vec2 floor
//28 ms: precomputed text length
//15 ms: locally alocated text data from string to char*
//13 ms: pre allocated final position and scale floats, removed needless vec2
//12 ms: inlined uneaded checkFont function


