#version 330 core
in vec2 frag_uv;
uniform sampler2D Text;
out vec4 FragColor;
void main()
{
   FragColor = vec4(texture(Text, frag_uv));
};



