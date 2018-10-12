#version 330 core
in vec2 frag_uv;
uniform sampler2D Text;
uniform vec4 tint;
out vec4 FragColor;
void main()
{
    vec4 texCol = vec4(texture(Text, frag_uv));
   FragColor = mix(texCol, vec4(tint.rgb, 1.0), texCol.a * tint.a);
};



