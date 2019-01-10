#version 330 core
in vec2 frag_uv;
uniform sampler2D Text;
// uniform sampler2D Mask;
out vec4 FragColor;
void main()
{
   vec4 texCol = vec4(texture(Text, frag_uv));
  // float textMask = vec4(texture(Mask, frag_uv)).r;
 //  texCol.a *= textMask;
   FragColor =  texCol;
};


