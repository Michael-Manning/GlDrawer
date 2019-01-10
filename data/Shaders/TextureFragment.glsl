#version 330 core
in vec2 frag_uv;
uniform sampler2D Text;
uniform vec4 tint;
uniform float opacity;
uniform vec2 scaleOffset;
uniform vec2 posOffset;
uniform vec2 zoom;
out vec4 FragColor;
void main()
{
   vec4 texCol;
   if(zoom.y >= 0)
      texCol = vec4(texture(Text, frag_uv * scaleOffset + posOffset));
   else
      texCol = vec4(texture(Text, (1.0 -frag_uv) * scaleOffset + posOffset));

   texCol.a *= opacity;
   FragColor = mix(texCol, vec4(tint.rgb, 1.0), texCol.a * tint.a);
};



