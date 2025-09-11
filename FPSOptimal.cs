using System;using System.Collections.Generic;using System.Numerics;using System.Linq;using Silk.NET.Input;using Silk.NET.OpenGL;using Silk.NET.OpenGL.Extensions.ImGui;using Silk.NET.Windowing;using Silk.NET.Maths;using ImGuiNET;using static System.Math;using V3=System.Numerics.Vector3;using M4=System.Numerics.Matrix4x4;
unsafe class FPS:IDisposable{
// State - 20 lines
IWindow w;GL g;IInputContext inp;IMouse m;IKeyboard k;ImGuiController ig;
uint va,vb,vi,sh;float[]vd;uint[]id;double acc,fps,ft=1.0/60;int fc;
V3 p=new(0,2,5),v,look;float hp=100,sens=.001f;bool grnd,dead,pause;
List<(V3 pos,V3 vel,float hp,float spd,int typ,float ht)>en=new();
List<(V3 pos,V3 vel,float life,int typ)>proj=new();
List<(V3 pos,V3 size,V3 col)>obs=new();
Dictionary<(int,int),List<int>>grid=new();
int wave=1,kills,ammo=30,maxAmmo=30,wpn,score;
float fireCd,jumpCd,sprintSpd=12,walkSpd=8,dmg=34;
const float grav=20,jump=7,cellSize=5;

// Initialization - 40 lines
public void Init(IWindow win,GL gl){
w=win;g=gl;g.Enable(EnableCap.DepthTest);g.Enable(EnableCap.CullFace);g.ClearColor(.05f,.05f,.1f,1);
inp=w.CreateInput();m=inp.Mice[0];k=inp.Keyboards[0];
m.Cursor.CursorMode=CursorMode.Raw;
m.MouseMove+=(mouse,delta)=>{
if(!pause&&Abs(delta.X)<100&&Abs(delta.Y)<100){
look.X+=delta.X*sens;
look.Y=Clamp(look.Y-delta.Y*sens,-1.5f,1.5f);
}};
k.KeyDown+=(k,key,_)=>{
if(key==Key.Escape)pause=!pause;
if(key==Key.Space&&grnd&&jumpCd<=0){v.Y=jump;jumpCd=.5f;}
if(key==Key.R&&dead){Reset();SpawnWave();}
if(key==Key.Number1){wpn=0;dmg=34;fireCd=.5f;}
if(key==Key.Number2){wpn=1;dmg=10;fireCd=.1f;}
if(key==Key.Number3){wpn=2;dmg=100;fireCd=1f;}
};
ig=new(g,w,inp);
// Vertex data for cube
vd=new[]{-.5f,-.5f,-.5f,0,0,-1,1,1,1,.5f,-.5f,-.5f,0,0,-1,1,1,1,.5f,.5f,-.5f,0,0,-1,1,1,1,-.5f,.5f,-.5f,0,0,-1,1,1,1,
-.5f,-.5f,.5f,0,0,1,1,1,1,.5f,-.5f,.5f,0,0,1,1,1,1,.5f,.5f,.5f,0,0,1,1,1,1,-.5f,.5f,.5f,0,0,1,1,1,1,
-.5f,-.5f,-.5f,-1,0,0,1,1,1,-.5f,-.5f,.5f,-1,0,0,1,1,1,-.5f,.5f,.5f,-1,0,0,1,1,1,-.5f,.5f,-.5f,-1,0,0,1,1,1,
.5f,-.5f,-.5f,1,0,0,1,1,1,.5f,-.5f,.5f,1,0,0,1,1,1,.5f,.5f,.5f,1,0,0,1,1,1,.5f,.5f,-.5f,1,0,0,1,1,1,
-.5f,-.5f,-.5f,0,-1,0,1,1,1,-.5f,-.5f,.5f,0,-1,0,1,1,1,.5f,-.5f,.5f,0,-1,0,1,1,1,.5f,-.5f,-.5f,0,-1,0,1,1,1,
-.5f,.5f,-.5f,0,1,0,1,1,1,-.5f,.5f,.5f,0,1,0,1,1,1,.5f,.5f,.5f,0,1,0,1,1,1,.5f,.5f,-.5f,0,1,0,1,1,1};
id=new uint[]{0,1,2,2,3,0,4,5,6,6,7,4,8,9,10,10,11,8,12,13,14,14,15,12,16,17,18,18,19,16,20,21,22,22,23,20};
va=g.GenVertexArray();vb=g.GenBuffer();vi=g.GenBuffer();
g.BindVertexArray(va);g.BindBuffer(BufferTargetARB.ArrayBuffer,vb);
fixed(void*ptr=vd)g.BufferData(BufferTargetARB.ArrayBuffer,(nuint)(vd.Length*sizeof(float)),ptr,BufferUsageARB.StaticDraw);
g.BindBuffer(BufferTargetARB.ElementArrayBuffer,vi);
fixed(void*ptr=id)g.BufferData(BufferTargetARB.ElementArrayBuffer,(nuint)(id.Length*sizeof(uint)),ptr,BufferUsageARB.StaticDraw);
g.VertexAttribPointer(0,3,VertexAttribPointerType.Float,false,9*sizeof(float),(void*)0);g.EnableVertexAttribArray(0);
g.VertexAttribPointer(1,3,VertexAttribPointerType.Float,false,9*sizeof(float),(void*)(3*sizeof(float)));g.EnableVertexAttribArray(1);
g.VertexAttribPointer(2,3,VertexAttribPointerType.Float,false,9*sizeof(float),(void*)(6*sizeof(float)));g.EnableVertexAttribArray(2);
sh=CreateShader();
GenerateWorld();SpawnWave();
}

// Complete Update - 80 lines
public void Update(double dt){
if(pause||dead&&k.IsKeyPressed(Key.R)==false)return;
acc+=dt;fps=1/dt;fc++;
while(acc>=ft){
// Input & Movement
var move=V3.Zero;float spd=k.IsKeyPressed(Key.ShiftLeft)?sprintSpd:walkSpd;
if(k.IsKeyPressed(Key.W))move.Z=-1;
if(k.IsKeyPressed(Key.S))move.Z=1;
if(k.IsKeyPressed(Key.A))move.X=-1;
if(k.IsKeyPressed(Key.D))move.X=1;
if(move.LengthSquared()>0){
move=V3.Normalize(move);
var fw=new V3((float)Sin(look.X),0,(float)-Cos(look.X));
var rt=V3.Normalize(V3.Cross(fw,V3.UnitY));
move=rt*move.X+V3.Cross(V3.UnitY,rt)*move.Z;
p+=move*spd*(float)ft;
}
// Physics
v.Y-=grav*(float)ft;
p+=v*(float)ft;
// Ground collision
if(p.Y<=2){p.Y=2;v.Y=0;grnd=true;}else grnd=false;
// Platform collision
foreach(var o in obs){
var min=o.pos-o.size/2;var max=o.pos+o.size/2;
if(p.X>=min.X&&p.X<=max.X&&p.Z>=min.Z&&p.Z<=max.Z&&p.Y>=min.Y&&p.Y<=max.Y+1&&v.Y<=0){
p.Y=max.Y+1;v.Y=0;grnd=true;
}}
// Shooting
if(m.IsButtonPressed(MouseButton.Left)&&fireCd<=0&&ammo>0&&!dead){
ammo--;fireCd=wpn==1?.1f:wpn==2?1f:.5f;
var dir=new V3((float)Sin(look.X),(float)Sin(look.Y),(float)-Cos(look.X));
if(wpn==2)for(float s=-.15f;s<=.15f;s+=.075f)proj.Add((p+V3.UnitY,V3.Normalize(dir+new V3(s,s/2,s))*30,3,2));
else proj.Add((p+V3.UnitY,dir*40,5,wpn));
}
fireCd-=(float)ft;jumpCd-=(float)ft;
// Update projectiles
for(int i=proj.Count-1;i>=0;i--){
var pr=proj[i];pr.pos+=pr.vel*(float)ft;pr.life-=(float)ft;
if(pr.life<=0||pr.pos.Y<0)proj.RemoveAt(i);
else proj[i]=pr;
}
// Update enemies
for(int i=0;i<en.Count;i++){
var e=en[i];
var dir=p-e.pos;dir.Y=0;
if(dir.LengthSquared()>4)e.pos+=V3.Normalize(dir)*e.spd*(float)ft;
e.ht=Max(0,e.ht-(float)ft*3);
en[i]=e;
}
// Collision detection - simpler approach
for(int i=proj.Count-1;i>=0;i--){
var pr=proj[i];
for(int j=en.Count-1;j>=0;j--){
if(V3.Distance(pr.pos,en[j].pos)<1.5f){
var e=en[j];e.hp-=dmg;e.ht=.2f;
if(e.hp<=0){
en.RemoveAt(j);kills++;score+=100*(e.typ+1);
if(kills%10==0){hp=Min(100,hp+20);ammo+=10;}
}else en[j]=e;
proj.RemoveAt(i);
break;
}}}
// Enemy damage
foreach(var e in en)if(V3.Distance(e.pos,p)<2){hp-=5*(float)ft;if(hp<=0){hp=0;dead=true;}}
// Wave management
if(en.Count==0){wave++;SpawnWave();}
acc-=ft;
}
ig.Update((float)dt);
}

// Complete Render - 60 lines
public void Render(double dt){
g.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);
g.UseProgram(sh);
// Calculate matrices
var fw=new V3((float)Sin(look.X),(float)Sin(look.Y),(float)-Cos(look.X));
var view=M4.CreateLookAt(p+V3.UnitY*.5f,p+V3.UnitY*.5f+fw,V3.UnitY);
var proj=M4.CreatePerspectiveFieldOfView(75*MathF.PI/180,w.Size.X/(float)w.Size.Y,.1f,100);
var vl=g.GetUniformLocation(sh,"view");var pl=g.GetUniformLocation(sh,"proj");var ml=g.GetUniformLocation(sh,"model");
var cl=g.GetUniformLocation(sh,"color");var tl=g.GetUniformLocation(sh,"time");
g.UniformMatrix4(vl,1,false,(float*)&view);g.UniformMatrix4(pl,1,false,(float*)&proj);
g.Uniform1(tl,(float)(fc*.01f));
g.BindVertexArray(va);
// Draw world
foreach(var o in obs){
var model=M4.CreateScale(o.size)*M4.CreateTranslation(o.pos);
g.UniformMatrix4(ml,1,false,(float*)&model);
g.Uniform3(cl,o.col.X,o.col.Y,o.col.Z);
g.DrawElements(PrimitiveType.Triangles,(uint)id.Length,DrawElementsType.UnsignedInt,null);
}
// Draw enemies
foreach(var e in en){
var scale=1f+e.ht*2;
var model=M4.CreateScale(scale)*M4.CreateTranslation(e.pos);
g.UniformMatrix4(ml,1,false,(float*)&model);
var col=e.typ==1?new V3(1,.2f,.2f):new V3(1f-e.hp/100,e.hp/200,0);
g.Uniform3(cl,col.X,col.Y,col.Z);
g.DrawElements(PrimitiveType.Triangles,(uint)id.Length,DrawElementsType.UnsignedInt,null);
}
// Draw projectiles  
foreach(var pr in this.proj){
var model=M4.CreateScale(.2f)*M4.CreateTranslation(pr.pos);
g.UniformMatrix4(ml,1,false,(float*)&model);
g.Uniform3(cl,1,1,0);
g.DrawElements(PrimitiveType.Triangles,(uint)id.Length,DrawElementsType.UnsignedInt,null);
}
// UI
ig.Render();
ImGui.SetNextWindowPos(new(10,10));ImGui.SetNextWindowSize(new(250,200));
ImGui.Begin("HUD",ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoScrollbar);
if(!dead){
ImGui.TextColored(new System.Numerics.Vector4(1,hp/100,hp/100,1),$"Health: {hp:0}/100");
ImGui.TextColored(new System.Numerics.Vector4(1,1,0,1),$"Ammo: {ammo}/{maxAmmo}");
ImGui.TextColored(new System.Numerics.Vector4(0,1,1,1),$"Wave: {wave}");
ImGui.TextColored(new System.Numerics.Vector4(1,.5f,0,1),$"Kills: {kills}");
ImGui.TextColored(new System.Numerics.Vector4(0,1,0,1),$"Score: {score}");
ImGui.TextColored(new System.Numerics.Vector4(1,0,0,1),$"Enemies: {en.Count}");
ImGui.Separator();
ImGui.Text($"FPS: {fps:0}");
ImGui.Text($"Weapon: {(wpn==0?"Revolver":wpn==1?"SMG":"Shotgun")}");
}else{
ImGui.TextColored(new System.Numerics.Vector4(1,0,0,1),"YOU DIED!");
ImGui.Text("Press R to Respawn");
ImGui.Text($"Final Score: {score}");
ImGui.Text($"Waves Survived: {wave-1}");
}
if(pause)ImGui.Text("PAUSED - Press ESC");
ImGui.End();
ig.Render();
}

// Helper functions - 40 lines
void SpawnWave(){
ammo=maxAmmo;
var count=Min(20,wave*3+2);
var r=new Random();
for(int i=0;i<count;i++){
var pos=new V3(r.Next(-25,25),1,r.Next(-25,25));
var typ=wave>5&&r.NextDouble()>.7?1:0;
en.Add((pos,V3.Zero,typ==1?200:80+wave*10,2f+wave*.3f,typ,0));
}}
void GenerateWorld(){
var r=new Random();
obs.Add((new V3(0,-1,0),new V3(100,.2f,100),new V3(.2f,.2f,.25f)));
for(int x=-30;x<=30;x+=8)for(int z=-30;z<=30;z+=8)
if(r.NextDouble()>.6)obs.Add((new V3(x,r.Next(0,3),z),new V3(r.Next(2,5),r.Next(1,4),r.Next(2,5)),new V3(.3f,.3f,.4f)));
}
void Reset(){p=new(0,2,5);v=V3.Zero;hp=100;ammo=30;wave=1;kills=0;score=0;en.Clear();proj.Clear();dead=false;}
uint CreateShader(){
var vs=@"#version 330 core
layout(location=0)in vec3 aPos;layout(location=1)in vec3 aNorm;layout(location=2)in vec3 aCol;
out vec3 FragPos;out vec3 Normal;out vec3 Color;
uniform mat4 model;uniform mat4 view;uniform mat4 proj;
void main(){
FragPos=vec3(model*vec4(aPos,1.0));Normal=mat3(transpose(inverse(model)))*aNorm;Color=aCol;
gl_Position=proj*view*vec4(FragPos,1.0);}";
var fs=@"#version 330 core
in vec3 FragPos;in vec3 Normal;in vec3 Color;out vec4 FragColor;
uniform vec3 color;uniform float time;
void main(){
vec3 lightPos=vec3(10,20,10);vec3 lightDir=normalize(lightPos-FragPos);
float diff=max(dot(Normal,lightDir),0.0);vec3 diffuse=diff*vec3(1);
vec3 ambient=vec3(0.3);vec3 result=(ambient+diffuse)*color*Color;
FragColor=vec4(result,1.0);}";
var v=g.CreateShader(ShaderType.VertexShader);g.ShaderSource(v,vs);g.CompileShader(v);
var f=g.CreateShader(ShaderType.FragmentShader);g.ShaderSource(f,fs);g.CompileShader(f);
var s=g.CreateProgram();g.AttachShader(s,v);g.AttachShader(s,f);g.LinkProgram(s);
g.DeleteShader(v);g.DeleteShader(f);return s;
}
public void Dispose(){g?.DeleteVertexArray(va);g?.DeleteBuffer(vb);g?.DeleteBuffer(vi);g?.DeleteProgram(sh);ig?.Dispose();}
public static void Main(){var w=Window.Create(WindowOptions.Default with{Size=new(1280,720),Title="FPS"});
var fps=new FPS();w.Load+=()=>{fps.Init(w,GL.GetApi(w));w.Update+=fps.Update;w.Render+=fps.Render;};w.Closing+=fps.Dispose;w.Run();}
}