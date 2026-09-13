#version 330 core
uniform ivec2 u_grid;
uniform vec2 u_view;
uniform sampler2D u_intensity;
uniform sampler2D u_atlas;
uniform int u_glyph_count;
uniform ivec2 u_atlas_grid;
uniform float u_gamma;
uniform int u_invert;
uniform int u_color_enabled;
uniform int u_palette_count;
uniform vec3 u_palette0;
uniform vec3 u_palette1;
uniform vec3 u_palette2;
uniform vec3 u_palette3;
uniform vec3 u_palette4;
uniform vec3 u_palette5;
uniform vec3 u_palette6;
uniform vec3 u_palette7;
out vec4 fragColor;

vec3 palAt(int i){
    if(i<=0)return u_palette0;if(i==1)return u_palette1;if(i==2)return u_palette2;if(i==3)return u_palette3;
    if(i==4)return u_palette4;if(i==5)return u_palette5;if(i==6)return u_palette6;return u_palette7;
}
vec3 palette(float x){
    if(u_color_enabled==0||u_palette_count<2)return vec3(1.0);
    float pos=clamp(x,0.0,1.0)*float(u_palette_count-1);int a=int(floor(pos)),b=min(u_palette_count-1,a+1);return mix(palAt(a),palAt(b),fract(pos));
}
void main(){
    vec2 cellSize=u_view/vec2(u_grid);
    int cx=clamp(int(floor(gl_FragCoord.x/cellSize.x)),0,u_grid.x-1);
    int cyBottom=clamp(int(floor(gl_FragCoord.y/cellSize.y)),0,u_grid.y-1);
    int cyTop=u_grid.y-1-cyBottom;
    float raw=texelFetch(u_intensity,ivec2(cx,cyBottom),0).r;
    float v=pow(clamp(raw,0.0,1.0),1.0/max(.05,u_gamma));if(u_invert!=0)v=1.0-v;
    int idx=int(round(v*float(max(0,u_glyph_count-1))));
    int col=idx%u_atlas_grid.x,row=idx/u_atlas_grid.x;
    vec2 local=fract(gl_FragCoord.xy/cellSize);local.y=1.0-local.y;
    vec2 uv=(vec2(float(col),float(row))+local)/vec2(u_atlas_grid);
    float glyph=texture(u_atlas,uv).r;
    fragColor=vec4(palette(v)*glyph,1.0);
}
