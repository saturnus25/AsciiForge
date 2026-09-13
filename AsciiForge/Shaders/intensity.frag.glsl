#version 330 core
/*__EFFECTS__*/
out vec4 fragColor;
void main(){
    // Keep discrete/text coordinates top-origin while the FBO itself remains
    // in OpenGL bottom-origin storage. basePoint() consumes this convention.
    vec2 cell=vec2(gl_FragCoord.x,float(u_grid.y)-gl_FragCoord.y);
    float v=effectIntensity(cell);
    fragColor=vec4(v,v,v,1.0);
}
