uniform ivec2 u_grid;
uniform float u_time;
uniform float u_temporal_time;
uniform int u_effect;
uniform int u_seed;
uniform float u_scale, u_amp, u_fx, u_fy, u_fd, u_fr, u_tf, u_phase;
uniform float u_turb, u_warp, u_dx, u_dy, u_pulse, u_density, u_aspect;
uniform float u_iterations;
uniform float u_fire_h, u_fire_w, u_fire_wind, u_fire_particles, u_fire_psize, u_fire_lift;
uniform float u_ball_count, u_ball_radius, u_ball_gravity, u_ball_bounce, u_ball_speed, u_ball_trails;
uniform float u_firework_count, u_firework_size, u_firework_sparks, u_firework_gravity, u_firework_decay;
uniform float u_rain_count, u_rain_ring_size, u_rain_ring_width, u_rain_decay;
uniform float u_galaxy_arms, u_galaxy_core, u_galaxy_twist, u_galaxy_halo;
uniform float u_donut_major, u_donut_minor, u_donut_spin_x, u_donut_spin_y, u_donut_detail;
uniform float u_star_amount, u_star_size, u_star_depth;
uniform float u_matrix_trail, u_matrix_spacing, u_matrix_head;
uniform float u_tunnel_rings, u_tunnel_twist, u_tunnel_depth;
uniform float u_horizon_height, u_horizon_fov, u_horizon_wave;
uniform float u_radio_thickness, u_radio_decay, u_radio_expand;
uniform float u_ca_rule, u_ca_step_rate, u_ca_history, u_ca_seed_density, u_ca_alive, u_ca_scroll;
uniform float u_wave_count, u_wave_height, u_wave_length, u_wave_direction, u_wave_spread, u_wave_sharpness;
uniform float u_ocean_height, u_ocean_length, u_ocean_layers, u_ocean_direction, u_ocean_choppiness, u_ocean_foam;
uniform float u_tank_sources, u_tank_frequency, u_tank_speed, u_tank_damping, u_tank_motion, u_tank_interference;
uniform float u_scope_waveform, u_scope_frequency, u_scope_amplitude, u_scope_thickness, u_scope_dual, u_scope_phase;
uniform float u_caustic_scale, u_caustic_distortion, u_caustic_speed, u_caustic_sharpness, u_caustic_layers;
uniform float u_aurora_bands, u_aurora_width, u_aurora_flow, u_aurora_curl, u_aurora_shimmer, u_aurora_height;
uniform float u_shape3d_type, u_shape3d_spin_x, u_shape3d_spin_y, u_shape3d_spin_z, u_shape3d_camera, u_shape3d_fov, u_shape3d_light, u_shape3d_mode;
uniform float u_terrain_height, u_terrain_detail, u_terrain_speed, u_terrain_camera, u_terrain_water, u_terrain_grid;
uniform float u_sdf_shape, u_sdf_repeat, u_sdf_twist, u_sdf_smooth, u_sdf_spin, u_sdf_depth;
uniform float u_flow_particles, u_flow_scale, u_flow_strength, u_flow_trails, u_flow_curl, u_flow_speed;
uniform float u_lightning_branches, u_lightning_width, u_lightning_jitter, u_lightning_forks, u_lightning_flash, u_lightning_speed;
uniform float u_blackhole_size, u_blackhole_disk, u_blackhole_spin, u_blackhole_lens, u_blackhole_jets, u_blackhole_stars;
uniform float u_attractor_type, u_attractor_points, u_attractor_zoom, u_attractor_rotation, u_attractor_trail, u_attractor_glow;
uniform float u_voronoi_cells, u_voronoi_speed, u_voronoi_edges, u_voronoi_fill, u_voronoi_warp, u_voronoi_pulse;
uniform float u_snow_amount, u_snow_size, u_snow_wind, u_snow_depth, u_snow_gust, u_snow_twinkle;
uniform float u_dna_turns, u_dna_radius, u_dna_speed, u_dna_rungs, u_dna_tilt, u_dna_depth;
uniform float u_warpgrid_density, u_warpgrid_depth, u_warpgrid_twist, u_warpgrid_wave, u_warpgrid_speed, u_warpgrid_horizon;
uniform int u_shape_mode;

float sat(float x) { return clamp(x, 0.0, 1.0); }
float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32 + float(u_seed % 997) * .001);
    return fract(p.x * p.y);
}
float hash11(float x) { return hash21(vec2(x, x * 1.317 + 17.0)); }
float vnoise(vec2 p) {
    vec2 i = floor(p), f = fract(p); f = f*f*(3.0-2.0*f);
    float a=hash21(i), b=hash21(i+vec2(1,0)), c=hash21(i+vec2(0,1)), d=hash21(i+vec2(1,1));
    return mix(mix(a,b,f.x), mix(c,d,f.x), f.y);
}
float fbm(vec2 p) {
    float s=0.0, a=.5, n=0.0;
    for (int i=0; i<7; ++i) {
        s += vnoise(p + vec2(u_time*(.22+float(i)*.047), -u_time*(.17+float(i)*.031))) * a;
        n += a; p = p*2.02 + vec2(13.7,7.1); a *= .5;
    }
    return s / max(n,.001);
}
float tri(float x) { return abs(fract(x)*2.0-1.0); }
float sdBox(vec2 p, vec2 b) { vec2 d=abs(p)-b; return length(max(d,0.0))+min(max(d.x,d.y),0.0); }
float waveShape(float x, int mode) {
    float s=sin(x);
    if(mode==1) return s>=0.0 ? 1.0 : -1.0;
    if(mode==2) return (2.0/3.14159265)*asin(s);
    if(mode==3) return fract(x/6.2831853)*2.0-1.0;
    if(mode==4) return .62*sin(x)+.26*sin(x*2.0+.7)+.12*sin(x*3.0+1.4);
    return s;
}

vec2 basePoint(vec2 cell) {
    // `cell` is top-origin (y=0 at the first text row). Map it once to
    // the legacy AsciiForge coordinate system: -1 at the top, +1 at the bottom.
    // Do NOT flip Y again here: intensity.frag already converts OpenGL's
    // bottom-origin gl_FragCoord to a top-origin cell coordinate.
    vec2 p = (cell / vec2(u_grid) - .5) * 2.0;
    p.x *= (float(u_grid.x) / max(1.0,float(u_grid.y))) * u_aspect;
    float ph = radians(u_phase);
    if (abs(u_warp) > .0001) {
        vec2 q=p;
        p.x += sin(q.y*max(.05,abs(u_fy))+u_temporal_time+ph)*.10*u_warp;
        p.y += cos(q.x*max(.05,abs(u_fx))-u_temporal_time*.87+ph)*.10*u_warp;
    }
    return p;
}

float mandelbrot(vec2 p, bool burning) {
    float ph=radians(u_phase);
    float breathe=1.0+(.55+.35*u_pulse)*(1.0+sin(u_temporal_time+ph))*max(.1,abs(u_amp));
    float zoom=max(.25,breathe*max(.03,abs(u_scale))*max(.35,abs(u_density)));
    vec2 centerDrift=vec2(sin(u_temporal_time*.19)*u_dx,cos(u_temporal_time*.17)*u_dy)*.025;
    vec2 c = burning ? vec2(-.47,-.56)+centerDrift+p/(1.55*zoom) : vec2(-.74364388703,.13182590421)+centerDrift+p/(2.4*zoom);
    vec2 z=vec2(0);
    int maxIt=int(clamp(u_iterations,4.0,250.0));
    for(int i=0;i<250;i++) {
        if(i>=maxIt) break;
        if(burning) z=abs(z);
        z=vec2(z.x*z.x-z.y*z.y,2.0*z.x*z.y)+c;
        if(dot(z,z)>16.0) return fract(float(i)/12.0+u_time*.045*u_tf);
    }
    return 0.0;
}

float julia(vec2 p) {
    float ph=radians(u_phase); float s=max(.03,abs(u_scale));
    vec2 z=p/max(.2,1.15*s);
    float orbit=.06+.075*abs(u_amp);
    vec2 c=vec2(-.745+orbit*cos(u_temporal_time*.23+ph)+sin(u_temporal_time*.17)*u_dx*.02,.113+orbit*sin(u_temporal_time*.19+ph)+cos(u_temporal_time*.15)*u_dy*.02);
    int maxIt=int(clamp(u_iterations,4.0,250.0));
    for(int i=0;i<250;i++) {
        if(i>=maxIt) break;
        z=vec2(z.x*z.x-z.y*z.y,2.0*z.x*z.y)+c;
        if(dot(z,z)>9.0) return pow(float(i)/float(maxIt),.45);
    }
    return 0.0;
}

float torusRay(vec2 p) {
    float ax=u_temporal_time*u_donut_spin_x+radians(u_phase);
    float ay=u_temporal_time*u_donut_spin_y+radians(u_phase)*.37;
    mat2 rx=mat2(cos(ax),-sin(ax),sin(ax),cos(ax));
    mat2 ry=mat2(cos(ay),-sin(ay),sin(ay),cos(ay));
    vec3 ro=vec3(0,0,4.2), rd=normalize(vec3(p.x,p.y,-2.15));
    float total=0.0;
    for(int i=0;i<72;i++) {
        vec3 q=ro+rd*total;
        q.yz=rx*q.yz; q.xz=ry*q.xz;
        vec2 t=vec2(length(q.xz)-max(.08,abs(u_donut_major)),q.y);
        float d=length(t)-max(.03,abs(u_donut_minor));
        if(d<.004) {
            float eps=.006;
            vec3 e=vec3(eps,0,0);
            vec3 qx=q+e, qy=q+e.yxy, qz=q+e.yyx;
            float d0=d;
            float dx=length(vec2(length(qx.xz)-abs(u_donut_major),qx.y))-abs(u_donut_minor)-d0;
            float dy=length(vec2(length(qy.xz)-abs(u_donut_major),qy.y))-abs(u_donut_minor)-d0;
            float dz=length(vec2(length(qz.xz)-abs(u_donut_major),qz.y))-abs(u_donut_minor)-d0;
            vec3 n=normalize(vec3(dx,dy,dz));
            return .18+.82*max(0.0,dot(n,normalize(vec3(-.4,.8,.55))));
        }
        total+=max(.003,d*.72/max(.2,abs(u_donut_detail)));
        if(total>9.0) break;
    }
    return 0.0;
}

float periodicLine(float coord, float width) {
    float f=fract(coord), d=min(f,1.0-f);
    float aa=max(fwidth(coord)*.55,.006);
    return 1.0-smoothstep(width,width+aa,d);
}

int caSeedState(int x, int block) {
    float density=clamp(u_ca_seed_density,0.0,1.0);
    if(density<=.001) return x==(u_grid.x/2) ? 1 : 0;
    float r=hash21(vec2(float(x)+float(block)*17.31,float(block)*41.73+13.7));
    return r<density ? 1 : 0;
}

float elementaryCA(ivec2 c) {
    int history=int(clamp(round(abs(u_ca_history)),4.0,32.0));
    int scrollSteps=int(floor(u_temporal_time*max(.0,u_ca_step_rate)*max(.0,u_ca_scroll)));
    int globalRow=c.y+scrollSteps;
    int block=globalRow/history;
    int gen=globalRow-block*history;
    if(gen<0){gen+=history;block-=1;}
    int width=gen*2+1;
    int baseX=c.x-gen;
    int state[65];
    for(int i=0;i<65;i++) state[i]=(i<width)?caSeedState(baseX+i,block):0;
    int rule=int(clamp(round(abs(u_ca_rule)),0.0,255.0));
    for(int step=0;step<32;step++){
        if(step>=gen) break;
        int nextWidth=width-2;
        for(int j=0;j<63;j++){
            if(j>=nextWidth) break;
            int n=state[j]*4+state[j+1]*2+state[j+2];
            state[j]=(rule>>n)&1;
        }
        width=nextWidth;
    }
    float alive=float(state[0]);
    // Slight generation shading gives depth without changing the binary rule.
    float shade=.72+.28*(1.0-float(gen)/max(1.0,float(history)));
    return sat(alive*max(.05,u_ca_alive)*shade);
}


mat3 rotX(float a){float c=cos(a),s=sin(a);return mat3(1,0,0,0,c,-s,0,s,c);}
mat3 rotY(float a){float c=cos(a),s=sin(a);return mat3(c,0,s,0,1,0,-s,0,c);}
mat3 rotZ(float a){float c=cos(a),s=sin(a);return mat3(c,-s,0,s,c,0,0,0,1);}
float sdSphere3(vec3 p,float r){return length(p)-r;}
float sdBox3(vec3 p,vec3 b){vec3 q=abs(p)-b;return length(max(q,0.0))+min(max(q.x,max(q.y,q.z)),0.0);}
float sdOcta3(vec3 p,float s){p=abs(p);return (p.x+p.y+p.z-s)*0.57735027;}
float sdTorus3(vec3 p,vec2 t){vec2 q=vec2(length(p.xz)-t.x,p.y);return length(q)-t.y;}
float sdCyl3(vec3 p,vec2 h){vec2 d=abs(vec2(length(p.xz),p.y))-h;return min(max(d.x,d.y),0.0)+length(max(d,0.0));}
float sdCapsule3(vec3 p,float h,float r){p.y-=clamp(p.y,-h,h);return length(p)-r;}
float sdPyramid3(vec3 p,float h){p.y+=h*.35;float m=max(abs(p.x),abs(p.z));return max(-p.y-h*.55,m-(h*.65-p.y*.52));}
float shapeSdf3(vec3 p,int typ){
 if(typ==0)return sdSphere3(p,.85); if(typ==1)return sdBox3(p,vec3(.68)); if(typ==2)return sdOcta3(p,1.15);
 if(typ==3)return sdTorus3(p,vec2(.62,.25)); if(typ==4)return sdCyl3(p,vec2(.58,.82)); if(typ==5)return sdCapsule3(p,.55,.34); return sdPyramid3(p,1.0);
}
float shapeScene3(vec3 p){
 float ax=u_temporal_time*u_shape3d_spin_x, ay=u_temporal_time*u_shape3d_spin_y, az=u_temporal_time*u_shape3d_spin_z;
 p=(rotZ(az)*rotY(ay)*rotX(ax))*p;
 return shapeSdf3(p,int(clamp(round(u_shape3d_type),0.0,6.0)));
}
vec3 shapeNormal3(vec3 p){float e=.004,d=shapeScene3(p);return normalize(vec3(shapeScene3(p+vec3(e,0,0))-d,shapeScene3(p+vec3(0,e,0))-d,shapeScene3(p+vec3(0,0,e))-d));}
float renderShape3D(vec2 p){
 vec3 ro=vec3(0,0,max(2.2,u_shape3d_camera)); vec3 rd=normalize(vec3(p.x,p.y,-max(.55,u_shape3d_fov)));
 float dsum=0.0; vec3 hit=vec3(0); bool ok=false;
 for(int i=0;i<96;i++){hit=ro+rd*dsum;float d=shapeScene3(hit);if(d<.003){ok=true;break;}dsum+=max(.003,d*.72);if(dsum>12.0)break;}
 if(!ok)return 0.0; vec3 n=shapeNormal3(hit); float light=.22+.78*max(0.0,dot(n,normalize(vec3(-.45,.75,.55))))*max(.0,u_shape3d_light);
 int mode=int(clamp(round(u_shape3d_mode),0.0,2.0)); if(mode==1)light*=.55+.45*(.5+.5*cos(dsum*14.0)); if(mode==2){float rim=pow(1.0-max(0.0,dot(n,-rd)),.35);light=max(light*.28,rim);}
 return sat(light);
}
float terrainHeightF(vec2 xz){float d=max(.15,u_terrain_detail);float n=fbm(xz*(1.2*d)+vec2(0,u_time*u_terrain_speed*.22));return (n-.48)*u_terrain_height;}
float terrainView(vec2 p){
 float hy=-.1+u_terrain_camera*.12; float best=0.0; float prev=-2.0;
 for(int i=1;i<72;i++){float z=float(i)/14.0+.12;float x=p.x*z*1.35;float h=terrainHeightF(vec2(x,z+u_time*u_terrain_speed*.35));float sy=hy+(h/z)*1.65+.42/z-.25;float line=1.0-smoothstep(.012,.035,abs(p.y-sy));if(sy>prev){best=max(best,line);prev=max(prev,sy);} if(h<u_terrain_water){float water=1.0-smoothstep(.012,.03,abs(p.y-(hy+(u_terrain_water/z)*1.65+.42/z-.25)));best=max(best,water*.35);} }
 float grid=.0;if(u_terrain_grid>.001){grid=periodicLine((p.x*12.0)/(max(.15,p.y+1.25)),.035)*u_terrain_grid*.18;}return sat(max(best,grid));
}
float sdfLabScene(vec3 p){
 float rep=max(.0,u_sdf_repeat);if(rep>.05){float cell=2.4/max(.25,rep);p.xz=mod(p.xz+cell*.5,cell)-cell*.5;}
 float a=u_temporal_time*u_sdf_spin; p=rotY(a)*p; float tw=p.y*u_sdf_twist*.35;p.xz=mat2(cos(tw),-sin(tw),sin(tw),cos(tw))*p.xz;
 int typ=int(clamp(round(u_sdf_shape),0.0,4.0)); if(typ==0){float d1=sdSphere3(p-vec3(.42*sin(a),.25*cos(a*.8),0),.48);float d2=sdSphere3(p+vec3(.38*cos(a*.7),.28*sin(a),.15),.44);float k=max(.02,u_sdf_smooth);float h=clamp(.5+.5*(d2-d1)/k,0.0,1.0);return mix(d2,d1,h)-k*h*(1.0-h);} if(typ==1)return sdBox3(p,vec3(.5));if(typ==2)return sdTorus3(p,vec2(.62,.2));if(typ==3)return sdCapsule3(p,.65,.25);return min(sdBox3(p,vec3(.45)),sdSphere3(p-vec3(.35,.25,.25),.48));
}
float renderSdfLab(vec2 p){vec3 ro=vec3(0,0,max(1.5,u_sdf_depth)),rd=normalize(vec3(p,-2.0));float t=0.;for(int i=0;i<88;i++){vec3 q=ro+rd*t;float d=sdfLabScene(q);if(d<.004){float e=.005;vec3 n=normalize(vec3(sdfLabScene(q+vec3(e,0,0))-d,sdfLabScene(q+vec3(0,e,0))-d,sdfLabScene(q+vec3(0,0,e))-d));return .2+.8*max(0.,dot(n,normalize(vec3(-.4,.8,.6))));}t+=max(.003,d*.7);if(t>12.)break;}return 0.;}
float lightningField(vec2 p){
 float phase=floor(u_temporal_time*max(.05,u_lightning_speed));float seed=phase*17.31+float(u_seed);float y=(p.y+1.0)*.5;float path=(hash11(seed)-.5)*.25;float amp=.10*u_lightning_jitter;path+=sin(y*7.0+seed)*amp+sin(y*17.0+seed*1.7)*amp*.45+sin(y*39.0+seed*.7)*amp*.18;float d=abs(p.x-path);float w=max(.003,u_lightning_width);float main=1.0-smoothstep(w,w*2.6,d);float forks=0.;int br=int(clamp(round(u_lightning_branches),1.,12.));for(int i=0;i<12;i++){if(i>=br)break;float fi=float(i);float start=hash11(seed+fi*5.1)*.8+.08;float side=hash11(seed+fi*9.7)>.5?1.:-1.;float yy=y-start;if(yy>0.&&yy<.28){float bx=path+side*yy*(.45+.55*hash11(fi+seed))*u_lightning_forks;float bd=abs(p.x-bx);forks=max(forks,(1.-smoothstep(w*.7,w*2.2,bd))*(1.-yy/.28));}}float flash=exp(-d*7.)*.18*u_lightning_flash;return sat(max(main,forks)+flash);}
float voronoiField(vec2 p){vec2 q=p*(2.2+u_voronoi_cells*.18);vec2 g=floor(q),f=fract(q);float d1=9.,d2=9.;for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++){vec2 o=vec2(x,y);vec2 r=vec2(hash21(g+o),hash21(g+o+17.3));r=.5+.38*sin(u_temporal_time*u_voronoi_speed+6.2831*r);float d=length(o+r-f);if(d<d1){d2=d1;d1=d;}else d2=min(d2,d);}float edge=sat((d2-d1)*7.*u_voronoi_edges);float fill=(.5+.5*cos(d1*8.+u_temporal_time*u_voronoi_pulse))*u_voronoi_fill;return sat(max(1.-edge,fill*.55));}

float effectIntensity(vec2 cell) {
    vec2 p=basePoint(cell);
    float t=u_time, ft=u_temporal_time, ph=radians(u_phase), pulse=1.0+sin(ft+ph)*.18*u_pulse;
    float s=max(.001,abs(u_scale)), v=0.0;

    if(u_effect==1) {
        vec2 q=p*2.2*s*pulse;
        float z=sin(q.x*u_fx+ft*(1.5+u_dx*.30)+ph)+sin(q.y*u_fy-ft*(1.1-u_dy*.30)+ph*.7)+sin((q.x+q.y)*u_fd+ft*.8-ph)+cos(length(q+vec2(sin(ft),cos(ft)))*u_fr-ft);
        v=.5+z*.125*max(.05,abs(u_amp));
    } else if(u_effect==2) v=mandelbrot(p,false);
    else if(u_effect==3) v=julia(p);
    else if(u_effect==4) v=mandelbrot(p,true);
    else if(u_effect==5) {
        vec2 flow=vec2(u_dx,u_dy)*t*.55; v=vnoise((p+flow)*vec2(u_fx,u_fy)*8.0*s+vec2(t*.7*u_tf,-t*.4*u_tf)); v=.5+(v-.5)*max(.05,abs(u_density))*pulse;
    } else if(u_effect==6) {
        vec2 flow=vec2(u_dx,u_dy)*t*.42; v=fbm((p+flow)*vec2(u_fx,u_fy)*4.2*s); v=.5+(v-.5)*(1.3+abs(u_turb)*.2)*max(.05,abs(u_density))*pulse;
    } else if(u_effect==7) {
        float r=max(.035,length(p)/s), a=atan(p.y,p.x)+sin(ft*.21)*u_dx*.45, depth=max(.05,abs(u_tunnel_depth))/r;
        v=.5+.25*sin(depth*2.7*max(.05,abs(u_tunnel_rings))-ft*(3.4+u_dy*.18))+.25*sin(a*9.0*u_tunnel_twist+depth*.8+ft*1.1);
    } else if(u_effect==8) {
        float best=0.0; int layers=int(clamp(3.0+abs(u_star_depth)*2.0,2.0,10.0));
        for(int i=0;i<10;i++){ if(i>=layers)break; float z=.1+fract(hash11(float(i)+float(u_seed))+t*(.06+float(i)*.012))*.9; vec2 g=(p+vec2(u_dx,u_dy)*t*.08)*z*18.0/max(.05,s*abs(u_star_depth)); vec2 ig=round(g); float rnd=hash21(ig+float(i)*37.7); float d=length(g-ig); float hit=step(max(.80,1.0-.035*abs(u_star_amount)),rnd); best+=hit*max(0.0,1.0-d*(2.9/max(.05,abs(u_star_size))))*(1.0-z*.55); } v=best;
    } else if(u_effect==9) {
        vec2 drift=vec2(sin(ft*.23)*u_dx,cos(ft*.19)*u_dy)*.22;
        vec2 a=vec2(.6*sin(t*.43),.55*cos(t*.37))+drift,b=vec2(.65*cos(t*.31+2.1),.45*sin(t*.51))+drift*.75,c=vec2(.35*sin(t*.61+4.2),.70*cos(t*.29))-drift*.55;
        v=.5+(sin(length(p-a)*u_fr-t*1.4)+sin(length(p-b)*(u_fr+2.0)-t*1.7)+sin(length(p-c)*(u_fr+4.0)-t*2.0))/6.0;
    } else if(u_effect==10) {
        float z=0.0; int count=int(clamp(3.0+abs(u_density)*3.0,1.0,20.0));
        for(int i=0;i<20;i++){if(i>=count)break;float fi=float(i),aa=t*(.23+fi*.035)+fi*1.7;vec2 cc=vec2(sin(aa*(1.2+fi*.04))*(.35+fi*.025),cos(aa*(1.55-fi*.03))*(.30+fi*.022));vec2 d=(p-cc)*s;z+=.055/(dot(d,d)+.025);} v=(z-.28)*.55;
    } else if(u_effect==11) {
        float r=length(p)*s,a=atan(p.y,p.x),seg=max(2.0,round(abs(u_fd)));float folded=abs(fract(a/6.2831853*seg+.5)-.5)*2.0;v=.5+.25*(sin(r*u_fr*2.0-t*1.8)+cos(folded*12.566+r*5.0+t));
    } else if(u_effect==12) {
        vec2 drift=vec2(sin(ft*.18)*u_dx,cos(ft*.16)*u_dy)*.24;
        float a=sin(length(p-(vec2(.45*sin(t*.4),0)+drift))*u_fr*s-t*2.1);float b=sin(length(p+(vec2(.45*cos(t*.33),-.2*sin(t))-drift*.7))*(u_fr*.82)*s+t*1.6);v=.5+.25*(a+b);
    } else if(u_effect==13) {
        float hn=.5+p.y*.5;
        // Wind bends the flame but never translates the whole fire forever.
        float wind=u_fire_wind; vec2 q=p; q.x+=wind*(1.0-hn)*.55+sin(ft*.72)*wind*.035;
        // General drift only moves the internal texture, so the flame body stays anchored.
        vec2 flow=vec2(u_dx,u_dy)*t*.42;
        float n=fbm(vec2((q.x+flow.x)*u_fx*2.0,(q.y+flow.y)*u_fy*3.0-ft*.75)*s);
        float width=max(.03,abs(u_fire_w))*(.20+hn*.80),mask=sat(1.0-abs(q.x)/width);float base=hn*(1.12+.20*abs(u_density))+(u_fire_h-1.0)*.38-.30;float tongues=sin(q.x*u_fx+n*(4.0+abs(u_turb)*2.0)+ft*1.7)*(.04+.04*abs(u_amp));
        vec2 sparkFlow=vec2(t*(.10+u_dx*.08),-t*(u_fire_lift*.22-u_dy*.06));
        float sparks=step(1.0-sat(abs(u_fire_particles)*.018),hash21(floor((p+sparkFlow)*vec2(u_grid)/max(1.0,abs(u_fire_psize)*2.0))));
        float sparkMask=sat(1.35-abs(q.x)/max(.10,width*2.6));
        v=(base+(n-.5)*(.35+.20*abs(u_turb))+tongues)*mask*pulse;
        v=max(v,sparks*sparkMask*sat((1.0-hn)*1.25)*.85);
    } else if(u_effect==14) {
        float spacing=max(1.0,round(abs(u_matrix_spacing))); if(mod(floor(cell.x),spacing)>.1) return 0.0; float col=floor(cell.x), speed=(3.5+hash11(col)*8.0), trail=(5.0+hash11(col+11.0)*float(u_grid.y)*.45)*max(.05,abs(u_matrix_trail));float head=mod(ft*speed+hash11(col+23.0)*float(u_grid.y),float(u_grid.y)+trail)-trail;float d=head-cell.y;v=(d>=-trail&&d<=0.0)?sat(1.0+d/max(.001,trail))*max(.1,abs(u_matrix_head)):0.0;
    } else if(u_effect==15) {
        int x=int(cell.x), y=int(cell.y), k=int(t*6.0*u_tf+u_phase);int a=(x*max(1,int(abs(u_fx))))^(y*max(1,int(abs(u_fy))))^k;int b=(x+k)&(y*max(1,int(abs(u_fd)+1.0))+k*2);int mask=int(clamp(15.0+abs(u_density)*24.0,3.0,255.0));v=float((a+b)&mask)/float(mask);
    } else if(u_effect==16) {
        vec2 q=p*vec2(u_fx,u_fy)*s;float a=sin(q.x+sin(t*.5)*q.y),b=sin(q.x*cos(t*.17)+q.y*sin(t*.17)+t*1.2),c=cos(length(q)*.85-t);v=.5+(a+b+c)/6.0;
    } else if(u_effect==17) {
        float total=0.0;
        int bursts=int(clamp(abs(u_firework_count),1.0,20.0));
        int sparks=int(clamp(abs(u_firework_sparks),4.0,80.0));
        for(int i=0;i<20;i++){
            if(i>=bursts) break;
            float fi=float(i);
            float cycle=1.9+hash11(fi+4.0)*2.4;
            float age=fract(ft/cycle+hash11(fi+5.0));
            vec2 center=vec2(hash11(fi+6.0)*1.65-.825+u_dx*.08, hash11(fi+7.0)*.75-.62+u_dy*.05);
            const float launch=.24;
            if(age<launch){
                float q=age/launch;
                float ease=1.0-pow(1.0-q,1.7);
                float sy=mix(.96,center.y,ease);
                float rr=.025+.012*abs(u_firework_size);
                total=max(total,max(0.0,1.0-length((p-vec2(center.x,sy))*s)/rr));
                // Short rocket trail; no history buffer required.
                for(int tr=1;tr<=3;tr++){
                    float qt=max(0.0,q-float(tr)*.055);
                    float sty=mix(.96,center.y,1.0-pow(1.0-qt,1.7));
                    float glow=max(0.0,1.0-length((p-vec2(center.x,sty))*s)/(rr*.8));
                    total=max(total,glow*(.55-float(tr)*.11));
                }
            } else {
                float a=(age-launch)/(1.0-launch);
                float fade=pow(max(0.0,1.0-a),max(.05,abs(u_firework_decay)));
                float core=max(0.0,1.0-length((p-center)*s)/(.045+.025*abs(u_firework_size)))*(1.0-smoothstep(0.0,.13,a));
                total+=core*1.4;
                for(int j=0;j<80;j++){
                    if(j>=sparks) break;
                    float fj=float(j);
                    float jitter=(hash11(fi*97.0+fj*3.17)-.5)*.28;
                    float ang=6.2831853*(fj/float(max(1,sparks)))+jitter+hash11(fi+51.0)*6.2831853;
                    float velocity=.72+hash11(fi*131.0+fj+71.0)*.62;
                    float radius=a*(.28+.44*abs(u_firework_size))*velocity;
                    vec2 pos=center+vec2(cos(ang),sin(ang))*radius;
                    pos.y+=max(0.0,u_firework_gravity)*a*a*.28;
                    float ps=(.016+.012*abs(u_firework_size))*(1.0-.30*a);
                    float spark=max(0.0,1.0-length((p-pos)*s)/max(.006,ps));
                    float twinkle=.72+.28*sin(ft*13.0+fj*2.31+fi*1.7);
                    total+=spark*fade*twinkle;
                }
            }
        }
        v=total*(.72+.28*abs(u_amp));
    } else if(u_effect==18) {
        vec2 cc=vec2(sin(ft*.37)*.28*u_dx,cos(ft*.31)*.22*u_dy);float r=length((p-cc)*s),wavelength=max(.03,4.0/max(.1,abs(u_fr)));float wave=.5+.5*cos(((r-ft*.55*u_radio_expand)/wavelength)*6.2831853);float band=max(0.0,1.0-abs(wave-1.0)/max(.005,abs(u_radio_thickness)));v=band/(1.0+r*max(0.0,u_radio_decay))*max(.2,abs(u_amp))*pulse;
    } else if(u_effect==19) {
        float total=0.0;int count=int(clamp(abs(u_rain_count),1.0,30.0));float ringw=max(.002,abs(u_rain_ring_width));for(int i=0;i<30;i++){if(i>=count)break;float fi=float(i);vec2 cc=vec2(hash11(fi+17.0)*2.4-1.2,hash11(fi+29.0)*1.8-.9);float age=fract(ft*.18+hash11(fi+41.0)*3.0),radius=age*(1.15+.55*abs(u_scale))*max(.05,abs(u_rain_ring_size)),d=length((p-cc)*s),edge=max(0.0,1.0-abs(d-radius)/ringw),fade=pow(1.0-age,max(.05,abs(u_rain_decay)));total+=edge*fade;}v=total*(.55+.25*abs(u_amp));
    } else if(u_effect==20) {
        float r=length(p)*s,a=atan(p.y,p.x)-ft*.28,arms=max(1.0,round(abs(u_galaxy_arms)));float spiral=.5+.5*cos(a*arms-r*(3.0+u_galaxy_twist)+ph),core=exp(-r/max(.025,abs(u_galaxy_core))),disk=max(0.0,1.0-r/1.35),stars=pow(hash21(floor((p+2.0)*vec2(u_grid)*.35)),18.0)*u_galaxy_halo;v=(spiral*.62+core*.75)*disk+stars;
    } else if(u_effect==21) v=torusRay(p);
    else if(u_effect==22) {
        float a=ft*.75+ph;mat2 r=mat2(cos(a),-sin(a),sin(a),cos(a));vec2 q=r*p/max(.15,s);float d=0.0;if(u_shape_mode==1)d=abs(abs(q.x)+abs(q.y)-.72);else if(u_shape_mode==2){float an=atan(q.y,q.x),rr=length(q),rad=.62+.18*cos(5.0*an);d=abs(rr-rad);}else if(u_shape_mode==3)d=abs(max(abs(q.x)*.866+abs(q.y)*.5,abs(q.y))-.62);else if(u_shape_mode==4)d=abs(min(max(abs(q.x)-.18,abs(q.y)-.72),max(abs(q.x)-.72,abs(q.y)-.18)));else d=abs(max(abs(q.x),abs(q.y))-.62);float thick=.045+.035*max(.1,abs(u_density));v=sat(1.0-d/thick)*pulse;
    } else if(u_effect==23) {
        float h=clamp(u_horizon_height,-.65,.55);
        float groundY=p.y-h;
        float horizonLine=1.0-smoothstep(.012,.055,abs(groundY));
        if(groundY<0.0){
            // Sparse, stable sky. The road itself starts below the horizon.
            float star=step(.9965,hash21(floor(cell+vec2(float(u_seed%31),0.0))));
            float skyFade=sat((-groundY)*.55);
            v=max(star*.65*skyFade,horizonLine*.88);
        } else {
            float q=max(.018,groundY);
            float fov=max(.12,abs(u_horizon_fov));
            float depth=min(60.0,fov/q);
            // Ground-space coordinates. Constant-X lines converge naturally at the vanishing point.
            float bend=sin(depth*.20+ft*.32+p.x*2.7)*u_horizon_wave*.014;
            float worldX=(p.x+bend)*depth;
            float worldZ=depth+ft*(.75+.025*abs(u_fy));
            float gx=periodicLine(worldX*max(.12,abs(u_fx))*.105,.050);
            float gz=periodicLine(worldZ*max(.12,abs(u_fy))*.070,.042);
            // Suppress the infinitely dense part right at the horizon; the explicit horizon line remains.
            float nearFade=smoothstep(.045,.16,q);
            float distanceFade=.58+.42*sat(q*1.2);
            float grid=max(gx,gz)*nearFade*distanceFade;
            // A subtle central road guide reinforces the perspective without dominating presets like Calm Sea.
            float guide=(1.0-smoothstep(.018,.055,abs(p.x)))*smoothstep(.10,.32,q)*.28*sat(u_density);
            v=max(max(grid,guide),horizonLine*.92);
        }
    } else if(u_effect==24) {
        float best=0.0;int count=int(clamp(abs(u_ball_count),1.0,64.0));for(int i=0;i<64;i++){if(i>=count)break;float fi=float(i),hx=hash11(fi+7.1),hy=hash11(fi+19.7),sp=u_ball_speed*(.32+.55*hash11(fi+31.3)),xp=tri(t*sp+hx)*2.0-1.0,q=fract(t*sp*.73+hy),arch=4.0*q*(1.0-q),yp=.82-arch*(.65+.35*u_ball_bounce)*pow(max(.05,abs(u_ball_gravity)),.35),rr=max(.005,abs(u_ball_radius)*(.65+.7*hash11(fi+53.2)));best=max(best,sat(1.0-length(p-vec2(xp,yp))/rr));if(u_ball_trails>.001){float xp2=tri((t-.08*u_ball_trails)*sp+hx)*2.0-1.0,q2=fract((t-.08*u_ball_trails)*sp*.73+hy),yp2=.82-4.0*q2*(1.0-q2)*(.65+.35*u_ball_bounce);best=max(best,sat(1.0-length(p-vec2(xp2,yp2))/(rr*.8))*u_ball_trails*.55);}}v=best;
    } else if(u_effect==25) {
        v=elementaryCA(ivec2(floor(cell)));
    } else if(u_effect==26) {
        int count=int(clamp(round(abs(u_wave_count)),1.0,8.0));
        float sum=0.0, baseK=6.2831853/max(.06,abs(u_wave_length));
        float center=(float(count)-1.0)*.5;
        for(int i=0;i<8;i++){
            if(i>=count) break;
            float fi=float(i);
            float ang=radians(u_wave_direction+(fi-center)*u_wave_spread);
            vec2 dir=vec2(cos(ang),sin(ang));
            float phase=dot(p,dir)*baseK*(1.0+fi*.075)-ft*(.85+fi*.09);
            float wv=sin(phase);
            float shaped=sign(wv)*pow(abs(wv),1.0/max(.15,abs(u_wave_sharpness)));
            sum+=shaped;
        }
        float z=sum/max(1.0,float(count));
        v=.5+.48*z*clamp(abs(u_wave_height),0.0,2.5);
    } else if(u_effect==27) {
        int layers=int(clamp(round(abs(u_ocean_layers)),1.0,8.0));
        float sum=0.0,norm=0.0;
        float baseK=6.2831853/max(.08,abs(u_ocean_length));
        for(int i=0;i<8;i++){
            if(i>=layers) break;
            float fi=float(i), amp=pow(.56,fi);
            float angle=radians(u_ocean_direction)+(hash11(fi+31.0)-.5)*(.45+.35*u_ocean_choppiness)+fi*.19;
            vec2 dir=vec2(cos(angle),sin(angle));
            float k=baseK*pow(1.72,fi);
            float distort=sin(dot(p,vec2(-dir.y,dir.x))*k*.22+ft*(.28+fi*.05))*u_ocean_choppiness*.32;
            float phase=dot(p,dir)*k+distort-ft*(.62+sqrt(k)*.085+fi*.035);
            sum+=sin(phase)*amp; norm+=amp;
        }
        float sea=sum/max(.001,norm);
        float crest=smoothstep(.42,.86,sea)*max(0.0,u_ocean_foam);
        float micro=(vnoise(p*18.0+vec2(ft*.24,-ft*.18))-.5)*.12*u_ocean_choppiness;
        v=.5+sea*.40*abs(u_ocean_height)+micro+crest*.40;
    } else if(u_effect==28) {
        int count=int(clamp(round(abs(u_tank_sources)),1.0,12.0));
        float sum=0.0;
        for(int i=0;i<12;i++){
            if(i>=count) break;
            float fi=float(i);
            vec2 base=vec2(hash11(fi+11.0)*1.7-.85,hash11(fi+37.0)*1.45-.72);
            vec2 motion=vec2(sin(ft*(.19+fi*.013)+fi*1.7),cos(ft*(.17+fi*.011)+fi*2.3))*u_tank_motion*.16;
            float r=length(p-(base+motion));
            float wave=sin(r*max(.1,u_tank_frequency)-ft*max(.01,u_tank_speed)*2.4+fi*.73);
            float fade=exp(-r*max(0.0,u_tank_damping));
            sum+=wave*fade;
        }
        float z=sum/max(1.0,sqrt(float(count)));
        v=.5+.44*z*max(0.0,u_tank_interference);
    } else if(u_effect==29) {
        vec2 q=(cell/vec2(u_grid)-.5)*2.0;
        float x=q.x, freq=max(.05,abs(u_scope_frequency));
        int mode=int(clamp(round(abs(u_scope_waveform)),0.0,4.0));
        float y1=waveShape(x*3.14159265*freq+ft*2.0,mode)*clamp(abs(u_scope_amplitude),.01,.98);
        float thick=max(.002,abs(u_scope_thickness));
        float line1=1.0-smoothstep(thick,thick+max(fwidth(q.y)*1.2,.004),abs(q.y-y1));
        float line2=0.0;
        if(u_scope_dual>.001){
            float y2=waveShape(x*3.14159265*(freq*.73)-ft*1.45+radians(u_scope_phase),mode)*clamp(abs(u_scope_amplitude)*.82,.01,.98);
            line2=(1.0-smoothstep(thick,thick+max(fwidth(q.y)*1.2,.004),abs(q.y-y2)))*clamp(u_scope_dual,0.0,1.0);
        }
        float grid=.10*(periodicLine((x+1.0)*5.0,.025)+periodicLine((q.y+1.0)*4.0,.025));
        v=max(max(line1,line2),grid);
    } else if(u_effect==30) {
        int layers=int(clamp(round(abs(u_caustic_layers)),1.0,6.0));
        vec2 q=p*max(.05,abs(u_caustic_scale));
        float total=0.0;
        for(int i=0;i<6;i++){
            if(i>=layers) break;
            float fi=float(i);
            vec2 dir=vec2(cos(fi*1.71+.4),sin(fi*1.71+.4));
            float warp=sin(dot(q,vec2(-dir.y,dir.x))*(1.2+fi*.31)+ft*u_caustic_speed*(.65+fi*.07))*u_caustic_distortion*.35;
            float a=sin(dot(q,dir)*(2.2+fi*.58)+warp+ft*u_caustic_speed*(.8+fi*.09));
            float line=pow(max(0.0,1.0-abs(a)),max(.15,abs(u_caustic_sharpness)));
            total+=line;
        }
        v=pow(total/max(1.0,float(layers)),.72);
    } else if(u_effect==31) {
        float bands=max(1.0,round(abs(u_aurora_bands)));
        float height=max(.05,abs(u_aurora_height));
        float yfade=1.0-smoothstep(.15,1.05,abs(p.y+.18)/height);
        float n=fbm(vec2(p.x*1.5+ft*u_aurora_flow*.12,p.y*2.1-ft*u_aurora_flow*.08));
        float bend=sin(p.y*(2.2+u_aurora_curl)+ft*u_aurora_flow+n*2.4)*u_aurora_curl*.12;
        float stripe=.5+.5*cos((p.x+bend+n*.10)*bands*3.14159265);
        float curtain=pow(stripe,max(.35,2.2/max(.03,abs(u_aurora_width)*5.0)));
        float shimmer=1.0+(vnoise(p*22.0+vec2(ft*1.2,-ft*.5))-.5)*u_aurora_shimmer;
        v=curtain*yfade*shimmer*(.62+.38*n);
    } else if(u_effect==32) {
        v=renderShape3D(p/max(.15,s));
    } else if(u_effect==33) {
        v=terrainView(p/max(.2,s));
    } else if(u_effect==34) {
        v=renderSdfLab(p/max(.2,s));
    } else if(u_effect==35) {
        float total=0.0;int count=int(clamp(round(u_flow_particles),4.0,80.0));
        for(int i=0;i<80;i++){if(i>=count)break;float fi=float(i);vec2 seed=vec2(hash11(fi*7.1+3.0)*2.0-1.0,hash11(fi*11.7+9.0)*2.0-1.0);seed.x+=u_temporal_time*u_flow_speed*.08;seed=mod(seed+1.0,2.0)-1.0;vec2 q=seed;float best=10.0;for(int j=0;j<18;j++){if(float(j)>6.0*u_flow_trails)break;float ang=fbm(q*u_flow_scale+u_temporal_time*.03)*6.2831*u_flow_curl+sin(q.y*2.3)*u_flow_strength;vec2 vel=vec2(cos(ang),sin(ang))*.055;best=min(best,length(p-q));q+=vel;}total+=exp(-best*95.0);}
        v=sat(total*.75);
    } else if(u_effect==36) {
        v=lightningField(p);
    } else if(u_effect==37) {
        float r=length(p),bh=max(.05,u_blackhole_size),ang=atan(p.y,p.x);float lens=max(.0,u_blackhole_lens)*bh*bh/max(.002,r*r);float warpedR=r+lens*.18;float stars=step(.994/max(.2,u_blackhole_stars),hash21(floor((p/max(.25,1.0-lens*.08)+2.0)*vec2(u_grid)*.23)))*u_blackhole_stars*.75;float diskR=abs(warpedR-(bh+.22*u_blackhole_disk));float disk=(1.-smoothstep(.015,.12,diskR))*smoothstep(bh*.8,bh*1.2,r);disk*=.55+.45*sin(ang*7.0-u_temporal_time*u_blackhole_spin*3.0+r*25.0);float shadow=1.-smoothstep(bh*.82,bh*1.08,r);float jets=(1.-smoothstep(.02,.09,abs(p.x)))*smoothstep(bh*.7,1.2,abs(p.y))*u_blackhole_jets*.45;v=max(max(stars*(1.-shadow),disk),jets);v*=1.-shadow*.95;
    } else if(u_effect==38) {
        int count=int(clamp(round(u_attractor_points),12.0,96.0));float best=10.0;float typ=round(u_attractor_type);vec2 q=vec2(.1,.1);vec2 prev=q;float a=1.4+.25*sin(u_temporal_time*.17),b=-2.3+.2*cos(u_temporal_time*.13),c=2.4,d=-2.1;float rot=u_temporal_time*u_attractor_rotation;mat2 rr=mat2(cos(rot),-sin(rot),sin(rot),cos(rot));
        for(int i=0;i<96;i++){if(i>=count)break;prev=q;if(typ<.5)q=vec2(sin(a*q.y)-cos(b*q.x),sin(c*q.x)-cos(d*q.y));else if(typ<1.5)q=vec2(sin(a*q.y)+c*cos(a*q.x),sin(b*q.x)+d*cos(b*q.y));else if(typ<2.5){float xx=q.x,yy=q.y;q=vec2(yy-sign(xx)*sqrt(abs(b*xx-c)),a-xx);}else q=vec2(sin(q.y*2.2+float(i)*.13),sin(q.x*2.7+float(i)*.09));vec2 qp=rr*(q*.36/max(.1,u_attractor_zoom));best=min(best,length(p-qp));}
        float line=exp(-best*(45.0/max(.2,u_attractor_trail)));v=sat(line*(.55+.45*u_attractor_glow));
    } else if(u_effect==39) {
        vec2 q=p+vec2(sin(p.y*3.+u_temporal_time)*u_voronoi_warp*.05,cos(p.x*3.-u_temporal_time)*u_voronoi_warp*.05);v=voronoiField(q);
    } else if(u_effect==40) {
        float total=0.;int layers=int(clamp(2.+u_snow_depth*2.,2.,8.));for(int l=0;l<8;l++){if(l>=layers)break;float fl=float(l);float sc=8.+fl*6.;vec2 q=p*sc;q.y-=u_time*(1.2+.27*fl);q.x-=u_time*u_snow_wind*(.18+.04*fl)+sin(q.y*.13+u_time)*u_snow_gust*.35;vec2 id=floor(q),fr=fract(q)-.5;float rnd=hash21(id+fl*31.7);float hit=step(1.-.13*u_snow_amount,rnd);float sz=(.08+.03*u_snow_size)/(1.+fl*.2);float flake=hit*exp(-dot(fr,fr)/(sz*sz));flake*=1.+sin(u_time*5.+rnd*20.)*.25*u_snow_twinkle;total+=flake/(1.+fl*.3);}v=sat(total);
    } else if(u_effect==41) {
        float y=p.y+sin(p.x*.8)*u_dna_tilt*.1;float phase=y*u_dna_turns*3.14159-u_temporal_time*u_dna_speed;float z1=cos(phase),z2=-z1;float x1=sin(phase)*u_dna_radius,x2=-x1;float depth1=.55+.45*(z1*u_dna_depth*.5+.5),depth2=.55+.45*(z2*u_dna_depth*.5+.5);float d1=abs(p.x-x1),d2=abs(p.x-x2);float strand=max(exp(-d1*75.)*depth1,exp(-d2*75.)*depth2);float rungPhase=fract((y+1.)*u_dna_rungs*.5);float rungGate=1.-smoothstep(.08,.18,min(rungPhase,1.-rungPhase));float lo=min(x1,x2),hi=max(x1,x2);float rung=step(lo,p.x)*step(p.x,hi)*rungGate*.55;v=sat(max(strand,rung));
    } else if(u_effect==42) {
        float h=clamp(u_warpgrid_horizon,-.6,.6),gy=p.y-h;if(gy<=.015){v=.0;}else{float dep=max(.1,u_warpgrid_depth)/gy;float twist=sin(dep*.18+u_temporal_time*.3)*u_warpgrid_twist*.08;float wx=(p.x+twist)*dep;float wz=dep+u_temporal_time*u_warpgrid_speed;float wave=sin(wx*.45+wz*.18)*u_warpgrid_wave*.15;float den=max(2.,u_warpgrid_density);float gx=periodicLine(wx*den*.06,.045),gz=periodicLine((wz+wave)*den*.045,.04);v=max(gx,gz)*smoothstep(.02,.14,gy);}
    }
    return sat(v);
}
