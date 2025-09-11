using System;using System.Collections.Generic;using System.Numerics;using System.Linq;using Silk.NET.Input;using Silk.NET.OpenGL;using Silk.NET.OpenGL.Extensions.ImGui;using Silk.NET.Windowing;using Silk.NET.Maths;using ImGuiNET;using static System.Math;using V3=System.Numerics.Vector3;using M4=System.Numerics.Matrix4x4;
unsafe class FPS:IDisposable{
// Core
IWindow w;GL g;IInputContext inp;IMouse m;IKeyboard k;ImGuiController ig;
uint va,vb,vi,sh;float[]vd;uint[]id;
// Timing & State
double acc,fps,ft=1.0/60;int fc;bool pause,dead,debug;
// Player
V3 pos=new(0,2,5),vel;float yaw,pitch,health=100,maxHealth=100;
float sensitivity=0.3f,fov=90;bool grnd,jumpCd;
// Weapons - wpn: 0=Revolver, 1=SMG, 2=Shotgun, 3=Katana
int weapon,ammo=30,maxAmmo=30,clipSize=6,kills,score,wave=1;
float fireCd,reloadCd,dmg=34,fireRate=0.5f,spread;
bool reloading;
// Enemies - (pos,vel,hp,maxHp,spd,typ,state,atkCd,ht)
// typ: 0=normal, 1=boss | state: 0=idle, 1=chase, 2=attack, 3=charge
List<Enemy>enemies=new();
// Projectiles - pooled for performance
Projectile[]projs=new Projectile[500];int projCount;
// Obstacles
List<(V3 pos,V3 size,V3 col)>obs=new();
// Spatial Hash for collision optimization
Dictionary<(int,int),List<int>>enemyGrid=new();
Dictionary<(int,int),List<int>>projGrid=new();
const float gridSize=5;
// Effects
float screenShake,damageFlash,hitMarkerTime,slashTime;
V3 slashDir;
List<(V3 pos,float time,string text)>killFeed=new();
// Pre-allocated render buffers
M4[]enemyTransforms=new M4[100];
V3[]enemyColors=new V3[100];
M4[]projTransforms=new M4[500];
// Constants
const float grav=20,jumpPwr=7,moveSpd=8,sprintSpd=12;

class Enemy{
public V3 pos,vel;public float hp,maxHp,spd,atkCd,ht;
public int typ,state;public bool active=true;
}
class Projectile{
public V3 pos,vel;public float life;public int typ;public bool active;
}

// Initialization
public void Init(IWindow win,GL gl){
w=win;g=gl;
g.Enable(EnableCap.DepthTest);
g.ClearColor(.05f,.05f,.1f,1);
// Setup input
inp=w.CreateInput();
if(inp.Mice.Count>0)m=inp.Mice[0];
if(inp.Keyboards.Count>0)k=inp.Keyboards[0];
if(m!=null){
m.Cursor.CursorMode=CursorMode.Raw;
m.MouseMove+=(mouse,delta)=>{
if(!pause&&Abs(delta.X)<1000&&Abs(delta.Y)<1000){
yaw+=delta.X*sensitivity*0.001f;
pitch=Clamp(pitch-delta.Y*sensitivity*0.001f,-1.5f,1.5f);
}};
}
if(k!=null)k.KeyDown+=(k,key,_)=>{
if(key==Key.Escape)pause=!pause;
if(key==Key.Space&&grnd&&!jumpCd){vel.Y=jumpPwr;jumpCd=true;}
if(key==Key.R){if(dead)Reset();else if(reloadCd<=0&&ammo<clipSize)StartReload();}
if(key==Key.F1)debug=!debug;
if(key==Key.F2)SpawnEnemyDebug();
if(key==Key.F3)pause=!pause; // F3 toggles pause as editor placeholder
// Weapon switching
if(key==Key.Number1)SetWeapon(3); // Katana
if(key==Key.Number2)SetWeapon(1); // SMG
if(key==Key.Number3)SetWeapon(2); // Shotgun
if(key==Key.Number4)SetWeapon(0); // Revolver
};
ig=new(g,w,inp);
// Setup vertex data for cube (6 faces * 4 verts * 9 floats)
vd=new[]{
// Front face
-.5f,-.5f,.5f,0,0,1,1,1,1,.5f,-.5f,.5f,0,0,1,1,1,1,
.5f,.5f,.5f,0,0,1,1,1,1,-.5f,.5f,.5f,0,0,1,1,1,1,
// Back face  
-.5f,-.5f,-.5f,0,0,-1,1,1,1,.5f,-.5f,-.5f,0,0,-1,1,1,1,
.5f,.5f,-.5f,0,0,-1,1,1,1,-.5f,.5f,-.5f,0,0,-1,1,1,1,
// Left face
-.5f,-.5f,-.5f,-1,0,0,1,1,1,-.5f,-.5f,.5f,-1,0,0,1,1,1,
-.5f,.5f,.5f,-1,0,0,1,1,1,-.5f,.5f,-.5f,-1,0,0,1,1,1,
// Right face
.5f,-.5f,-.5f,1,0,0,1,1,1,.5f,-.5f,.5f,1,0,0,1,1,1,
.5f,.5f,.5f,1,0,0,1,1,1,.5f,.5f,-.5f,1,0,0,1,1,1,
// Bottom face
-.5f,-.5f,-.5f,0,-1,0,1,1,1,.5f,-.5f,-.5f,0,-1,0,1,1,1,
.5f,-.5f,.5f,0,-1,0,1,1,1,-.5f,-.5f,.5f,0,-1,0,1,1,1,
// Top face
-.5f,.5f,-.5f,0,1,0,1,1,1,.5f,.5f,-.5f,0,1,0,1,1,1,
.5f,.5f,.5f,0,1,0,1,1,1,-.5f,.5f,.5f,0,1,0,1,1,1
};
// Index data (6 faces * 2 triangles * 3 indices)
id=new uint[36];
for(uint f=0;f<6;f++){
var o=f*4;
id[f*6]=o;id[f*6+1]=o+1;id[f*6+2]=o+2;
id[f*6+3]=o+2;id[f*6+4]=o+3;id[f*6+5]=o;
}
// Setup GPU buffers
va=g.GenVertexArray();vb=g.GenBuffer();vi=g.GenBuffer();
g.BindVertexArray(va);
g.BindBuffer(BufferTargetARB.ArrayBuffer,vb);
fixed(void*ptr=vd)g.BufferData(BufferTargetARB.ArrayBuffer,(nuint)(vd.Length*sizeof(float)),ptr,BufferUsageARB.StaticDraw);
g.BindBuffer(BufferTargetARB.ElementArrayBuffer,vi);
fixed(void*ptr=id)g.BufferData(BufferTargetARB.ElementArrayBuffer,(nuint)(id.Length*sizeof(uint)),ptr,BufferUsageARB.StaticDraw);
g.VertexAttribPointer(0,3,VertexAttribPointerType.Float,false,9*sizeof(float),(void*)0);
g.EnableVertexAttribArray(0);
g.VertexAttribPointer(1,3,VertexAttribPointerType.Float,false,9*sizeof(float),(void*)(3*sizeof(float)));
g.EnableVertexAttribArray(1);
g.VertexAttribPointer(2,3,VertexAttribPointerType.Float,false,9*sizeof(float),(void*)(6*sizeof(float)));
g.EnableVertexAttribArray(2);
sh=CreateShader();
// Initialize projectile pool
for(int i=0;i<projs.Length;i++)projs[i]=new Projectile();
// Generate world
GenerateWorld();
SpawnWave();
SetWeapon(3); // Start with Katana
}

// Update
public void Update(double dt){
if(pause||dead&&(k==null||!k.IsKeyPressed(Key.R)))return;
acc+=dt;fps=1/dt;fc++;
while(acc>=ft){
// Input & Movement
var move=V3.Zero;
float spd=(k!=null&&k.IsKeyPressed(Key.ShiftLeft))?sprintSpd:moveSpd;
if(k!=null){
if(k.IsKeyPressed(Key.W))move.Z=-1;
if(k.IsKeyPressed(Key.S))move.Z=1;
if(k.IsKeyPressed(Key.A))move.X=-1;
if(k.IsKeyPressed(Key.D))move.X=1;
}
if(move.LengthSquared()>0){
move=V3.Normalize(move);
var fw=new V3((float)Sin(yaw),0,(float)-Cos(yaw));
var rt=V3.Normalize(V3.Cross(fw,V3.UnitY));
move=rt*move.X+V3.Cross(V3.UnitY,rt)*move.Z;
pos+=move*spd*(float)ft;
}
// Physics
vel.Y-=grav*(float)ft;
pos+=vel*(float)ft;
// Ground collision
if(pos.Y<=2){pos.Y=2;vel.Y=0;grnd=true;jumpCd=false;}else grnd=false;
// Platform collision
foreach(var o in obs){
var min=o.pos-o.size/2;var max=o.pos+o.size/2;
// Landing on top
if(pos.X>=min.X&&pos.X<=max.X&&pos.Z>=min.Z&&pos.Z<=max.Z&&
   pos.Y>=min.Y&&pos.Y<=max.Y+1&&vel.Y<=0){
pos.Y=max.Y+1;vel.Y=0;grnd=true;jumpCd=false;
}
// Side collision - prevent walking through/up walls
else if(pos.X>min.X-0.5f&&pos.X<max.X+0.5f&&pos.Z>min.Z-0.5f&&pos.Z<max.Z+0.5f&&
   pos.Y>min.Y&&pos.Y<max.Y+0.5f){
var dx=pos.X-o.pos.X;var dz=pos.Z-o.pos.Z;
if(Abs(dx)>Abs(dz)){
if(dx>0)pos.X=max.X+0.5f;else pos.X=min.X-0.5f;
}else{
if(dz>0)pos.Z=max.Z+0.5f;else pos.Z=min.Z-0.5f;
}}}
// Shooting
if(m!=null&&m.IsButtonPressed(MouseButton.Left)&&fireCd<=0&&!reloading&&ammo>0&&!dead){
Fire();
}
// Reload
if(reloadCd>0){
reloadCd-=(float)ft;
if(reloadCd<=0){
ammo=clipSize;reloading=false;
}}
fireCd=Max(0,fireCd-(float)ft);
// Update projectiles
projGrid.Clear();
for(int i=0;i<projCount;i++){
var p=projs[i];
if(!p.active)continue;
p.pos+=p.vel*(float)ft;
p.life-=(float)ft;
if(p.life<=0||p.pos.Y<0){
p.active=false;
continue;
}
// Add to spatial grid
var gx=(int)(p.pos.X/gridSize);
var gz=(int)(p.pos.Z/gridSize);
var key=(gx,gz);
if(!projGrid.ContainsKey(key))projGrid[key]=new List<int>();
projGrid[key].Add(i);
}
// Compact active projectiles
int writeIdx=0;
for(int i=0;i<projCount;i++){
if(projs[i].active){
if(i!=writeIdx){
var temp=projs[writeIdx];
projs[writeIdx]=projs[i];
projs[i]=temp;
}
writeIdx++;
}}
projCount=writeIdx;
// Update enemies
enemyGrid.Clear();
for(int i=0;i<enemies.Count;i++){
var e=enemies[i];
if(!e.active)continue;
// Enemy AI
var dir=pos-e.pos;dir.Y=0;
var dist=dir.Length();
// State machine
switch(e.state){
case 0: // Idle
if(dist<20)e.state=1;
break;
case 1: // Chase
if(dist>2&&dist<50){
e.vel=V3.Normalize(dir)*e.spd;
e.pos+=e.vel*(float)ft;
}
if(dist<10&&e.atkCd<=0){
e.state=2;e.atkCd=2;
}
break;
case 2: // Attack
e.atkCd-=(float)ft;
if(e.atkCd<=0){
if(e.typ==1&&dist<15){ // Boss charge
e.state=3;e.vel=V3.Normalize(dir)*20;
}else e.state=1;
}
break;
case 3: // Charge (boss)
e.pos+=e.vel*(float)ft;
e.vel*=0.95f; // Decelerate
if(e.vel.Length()<2)e.state=1;
break;
}
// Hit flash
e.ht=Max(0,e.ht-(float)ft*3);
// Add to spatial grid
var gx=(int)(e.pos.X/gridSize);
var gz=(int)(e.pos.Z/gridSize);
var key=(gx,gz);
if(!enemyGrid.ContainsKey(key))enemyGrid[key]=new List<int>();
enemyGrid[key].Add(i);
}
// Collision detection using spatial hash
var pgx=(int)(pos.X/gridSize);
var pgz=(int)(pos.Z/gridSize);
// Check projectile-enemy collisions
for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++){
var key=(pgx+dx,pgz+dz);
if(!projGrid.ContainsKey(key)||!enemyGrid.ContainsKey(key))continue;
foreach(var pi in projGrid[key]){
var p=projs[pi];
if(!p.active)continue;
foreach(var ei in enemyGrid[key]){
var e=enemies[ei];
if(!e.active)continue;
var sz=e.typ==1?2f:1f; // Boss is bigger
if(V3.Distance(p.pos,e.pos)<sz){
e.hp-=dmg;e.ht=0.2f;
hitMarkerTime=0.2f;
if(e.hp<=0){
e.active=false;
kills++;
score+=100*(e.typ+1);
killFeed.Add((e.pos,3,$"Kill +{100*(e.typ+1)}"));
if(kills%5==0){
health=Min(maxHealth,health+20);
damageFlash=0.3f;
}
}
p.active=false;
screenShake=0.1f;
break;
}}}}
// Enemy melee damage
if(enemyGrid.ContainsKey((pgx,pgz))){
foreach(var ei in enemyGrid[(pgx,pgz)]){
var e=enemies[ei];
if(!e.active)continue;
var sz=e.typ==1?3f:2f;
if(V3.Distance(e.pos,pos)<sz){
health-=5*(float)ft*(e.typ+1);
damageFlash=0.5f;
if(health<=0){health=0;dead=true;}
}}}
// Update effects
screenShake=Max(0,screenShake-(float)ft*5);
damageFlash=Max(0,damageFlash-(float)ft*2);
hitMarkerTime=Max(0,hitMarkerTime-(float)ft*5);
slashTime=Max(0,slashTime-(float)ft*3);
// Update kill feed
for(int i=killFeed.Count-1;i>=0;i--){
var kf=killFeed[i];
kf.time-=(float)ft;
if(kf.time<=0)killFeed.RemoveAt(i);
else killFeed[i]=(kf.pos,kf.time,kf.text);
}
// Wave management
if(!enemies.Any(e=>e.active)){wave++;SpawnWave();}
acc-=ft;
}
ig.Update((float)dt);
}

// Render
public void Render(double dt){
g.Clear(ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);
g.UseProgram(sh);
// Camera with screen shake
var shakeOffset=new V3(
(float)(new Random().NextDouble()-.5)*screenShake,
(float)(new Random().NextDouble()-.5)*screenShake,0);
var fw=new V3((float)Sin(yaw),(float)Sin(pitch),(float)-Cos(yaw));
var view=M4.CreateLookAt(pos+V3.UnitY*.5f+shakeOffset,pos+V3.UnitY*.5f+fw,V3.UnitY);
var proj=M4.CreatePerspectiveFieldOfView(fov*MathF.PI/180,w.Size.X/(float)w.Size.Y,.1f,100);
// Set uniforms
var vl=g.GetUniformLocation(sh,"view");
var pl=g.GetUniformLocation(sh,"proj");
var ml=g.GetUniformLocation(sh,"model");
var cl=g.GetUniformLocation(sh,"color");
g.UniformMatrix4(vl,1,false,(float*)&view);
g.UniformMatrix4(pl,1,false,(float*)&proj);
g.BindVertexArray(va);
// Draw world
foreach(var o in obs){
var model=M4.CreateScale(o.size)*M4.CreateTranslation(o.pos);
g.UniformMatrix4(ml,1,false,(float*)&model);
g.Uniform3(cl,o.col.X,o.col.Y,o.col.Z);
g.DrawElements(PrimitiveType.Triangles,36,DrawElementsType.UnsignedInt,null);
}
// Draw enemies (batched)
int eCount=0;
foreach(var e in enemies){
if(!e.active||eCount>=100)continue;
var scale=e.typ==1?2f:1f;
scale*=1f+e.ht*2; // Hit animation
enemyTransforms[eCount]=M4.CreateScale(scale)*M4.CreateTranslation(e.pos);
var healthPct=e.hp/e.maxHp;
enemyColors[eCount]=e.typ==1?new V3(1,.2f,.2f):new V3(1-healthPct,healthPct*.5f,0);
eCount++;
}
for(int i=0;i<eCount;i++){
var et=enemyTransforms[i];
g.UniformMatrix4(ml,1,false,(float*)&et);
g.Uniform3(cl,enemyColors[i].X,enemyColors[i].Y,enemyColors[i].Z);
g.DrawElements(PrimitiveType.Triangles,36,DrawElementsType.UnsignedInt,null);
}
// Draw projectiles (batched)
for(int i=0;i<projCount;i++){
var p=projs[i];
if(!p.active)continue;
projTransforms[i]=M4.CreateScale(.2f)*M4.CreateTranslation(p.pos);
}
for(int i=0;i<projCount;i++){
if(!projs[i].active)continue;
var pt=projTransforms[i];
g.UniformMatrix4(ml,1,false,(float*)&pt);
g.Uniform3(cl,1,1,0);
g.DrawElements(PrimitiveType.Triangles,36,DrawElementsType.UnsignedInt,null);
}
// Katana slash effect
if(slashTime>0){
g.Enable(EnableCap.Blend);
g.BlendFunc(BlendingFactor.SrcAlpha,BlendingFactor.OneMinusSrcAlpha);
var slashPos=pos+V3.UnitY+slashDir*2;
var slashModel=M4.CreateScale(3,2,0.1f)*
    M4.CreateFromYawPitchRoll(yaw,pitch,0)*
    M4.CreateTranslation(slashPos);
var sm=slashModel;
g.UniformMatrix4(ml,1,false,(float*)&sm);
g.Uniform3(cl,1,1,1);
g.DrawElements(PrimitiveType.Triangles,36,DrawElementsType.UnsignedInt,null);
g.Disable(EnableCap.Blend);
}
// Damage flash overlay
if(damageFlash>0){
g.Disable(EnableCap.DepthTest);
g.Enable(EnableCap.Blend);
g.BlendFunc(BlendingFactor.SrcAlpha,BlendingFactor.OneMinusSrcAlpha);
// Draw red tint (simplified, would need screen quad in real impl)
g.Enable(EnableCap.DepthTest);
g.Disable(EnableCap.Blend);
}
// UI
ig.Render();
ImGui.SetNextWindowPos(new(10,10));
ImGui.SetNextWindowSize(new(300,250));
ImGui.Begin("HUD",ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoScrollbar);
if(!dead){
// Stats
ImGui.TextColored(new System.Numerics.Vector4(1,health/maxHealth,health/maxHealth,1),$"Health: {health:0}/{maxHealth}");
ImGui.TextColored(new System.Numerics.Vector4(1,1,0,1),$"Ammo: {ammo}/{clipSize}");
if(reloading)ImGui.TextColored(new System.Numerics.Vector4(1,.5f,0,1),"RELOADING...");
ImGui.TextColored(new System.Numerics.Vector4(0,1,1,1),$"Wave: {wave}");
ImGui.TextColored(new System.Numerics.Vector4(1,.5f,0,1),$"Kills: {kills}");
ImGui.TextColored(new System.Numerics.Vector4(0,1,0,1),$"Score: {score}");
ImGui.TextColored(new System.Numerics.Vector4(1,0,0,1),$"Enemies: {enemies.Count(e=>e.active)}");
ImGui.Separator();
ImGui.Text($"FPS: {fps:0}");
string wpnName=weapon switch{0=>"Revolver",1=>"SMG",2=>"Shotgun",3=>"Katana",_=>"Unknown"};
ImGui.Text($"Weapon: {wpnName}");
// Kill feed
if(killFeed.Count>0){
ImGui.Separator();
ImGui.Text("Recent:");
foreach(var kf in killFeed.Take(3)){
ImGui.TextColored(new System.Numerics.Vector4(1,1,0,kf.time/3),kf.text);
}}
// Debug info
if(debug){
ImGui.Separator();
ImGui.Text($"Pos: {pos.X:0.0}, {pos.Y:0.0}, {pos.Z:0.0}");
ImGui.Text($"Projectiles: {projCount}/500");
ImGui.Text($"Grid Cells: {enemyGrid.Count+projGrid.Count}");
}
}else{
ImGui.TextColored(new System.Numerics.Vector4(1,0,0,1),"YOU DIED!");
ImGui.Text("Press R to Respawn");
ImGui.Text($"Final Score: {score}");
ImGui.Text($"Waves Survived: {wave-1}");
ImGui.Text($"Total Kills: {kills}");
}
if(pause)ImGui.Text("PAUSED - Press ESC");
// Hit marker
if(hitMarkerTime>0){
ImGui.SetNextWindowPos(new(w.Size.X/2-10,w.Size.Y/2-10));
ImGui.SetNextWindowSize(new(20,20));
ImGui.Begin("Hit",ImGuiWindowFlags.NoTitleBar|ImGuiWindowFlags.NoResize|ImGuiWindowFlags.NoMove|ImGuiWindowFlags.NoBackground);
ImGui.TextColored(new System.Numerics.Vector4(1,1,1,hitMarkerTime*5),"X");
ImGui.End();
}
ImGui.End();
ig.Render();
}

// Weapon system
void SetWeapon(int w){
weapon=w;
switch(w){
case 0: // Revolver
dmg=50;fireRate=0.5f;clipSize=6;spread=0;break;
case 1: // SMG
dmg=15;fireRate=0.1f;clipSize=30;spread=0.05f;break;
case 2: // Shotgun
dmg=20;fireRate=1f;clipSize=8;spread=0.15f;break;
case 3: // Katana
dmg=100;fireRate=0.8f;clipSize=999;spread=0;break;
}
if(ammo>clipSize)ammo=clipSize;
}
void Fire(){
ammo--;fireCd=fireRate;
var dir=new V3((float)Sin(yaw),(float)Sin(pitch),(float)-Cos(yaw));
if(weapon==2){ // Shotgun
for(int i=0;i<8;i++){
var s=new V3((float)(new Random().NextDouble()-.5)*spread,
             (float)(new Random().NextDouble()-.5)*spread,
             (float)(new Random().NextDouble()-.5)*spread);
SpawnProjectile(pos+V3.UnitY,V3.Normalize(dir+s)*30,3,weapon);
}
}else if(weapon==3){ // Katana - melee sweep
// Check enemies in front arc
foreach(var e in enemies){
if(!e.active)continue;
var toEnemy=e.pos-pos;
if(toEnemy.Length()>5)continue; // Range
var angle=MathF.Acos(V3.Dot(V3.Normalize(toEnemy),dir));
if(angle<0.7f){ // ~40 degree cone
e.hp-=dmg;e.ht=0.3f;
if(e.hp<=0){
e.active=false;kills++;score+=150;
killFeed.Add((e.pos,3,"Slash Kill +150"));
}}}
screenShake=0.2f;
slashTime=0.3f;slashDir=dir;
}else{ // Regular projectile
var s=new V3((float)(new Random().NextDouble()-.5)*spread,
             (float)(new Random().NextDouble()-.5)*spread,
             (float)(new Random().NextDouble()-.5)*spread);
SpawnProjectile(pos+V3.UnitY,V3.Normalize(dir+s)*40,5,weapon);
}
}
void StartReload(){reloading=true;reloadCd=2f;}
void SpawnProjectile(V3 p,V3 v,float l,int t){
if(projCount>=projs.Length)return;
var proj=projs[projCount++];
proj.pos=p;proj.vel=v;proj.life=l;proj.typ=t;proj.active=true;
}
// World generation
void SpawnWave(){
ammo=clipSize;
var count=Min(30,wave*3+2);
var r=new Random();
for(int i=0;i<count;i++){
var e=new Enemy{
pos=new V3(r.Next(-30,30),1,r.Next(-30,30)),
typ=wave>5&&r.NextDouble()>.7?1:0,
hp=0,maxHp=0,spd=0,state=0,active=true
};
// Set stats based on type
if(e.typ==1){ // Boss
e.hp=e.maxHp=500+wave*50;e.spd=4;
}else{ // Normal
e.hp=e.maxHp=80+wave*10;e.spd=2+wave*0.2f;
}
enemies.Add(e);
}}
void SpawnEnemyDebug(){
var r=new Random();
for(int i=0;i<5;i++){
var e=new Enemy{
pos=new V3(r.Next(-10,10),1,r.Next(-10,10)),
typ=0,hp=100,maxHp=100,spd=3,state=0,active=true
};
enemies.Add(e);
}}
void GenerateWorld(){
var r=new Random();
obs.Add((new V3(0,-1,0),new V3(100,.2f,100),new V3(.2f,.2f,.25f)));
for(int x=-30;x<=30;x+=8)for(int z=-30;z<=30;z+=8)
if(r.NextDouble()>.6)obs.Add((new V3(x,r.Next(0,3),z),
new V3(r.Next(2,5),r.Next(1,4),r.Next(2,5)),new V3(.3f,.3f,.4f)));
}
void Reset(){
pos=new(0,2,5);vel=V3.Zero;health=maxHealth;ammo=clipSize;
wave=1;kills=0;score=0;enemies.Clear();projCount=0;
dead=false;reloading=false;SpawnWave();
}
// Shader creation
uint CreateShader(){
var vs=@"#version 330 core
layout(location=0)in vec3 aPos;layout(location=1)in vec3 aNorm;layout(location=2)in vec3 aCol;
out vec3 FragPos;out vec3 Normal;out vec3 Color;
uniform mat4 model;uniform mat4 view;uniform mat4 proj;
void main(){
FragPos=vec3(model*vec4(aPos,1.0));
Normal=mat3(transpose(inverse(model)))*aNorm;
Color=aCol;
gl_Position=proj*view*vec4(FragPos,1.0);
}";
var fs=@"#version 330 core
in vec3 FragPos;in vec3 Normal;in vec3 Color;
out vec4 FragColor;
uniform vec3 color;
void main(){
vec3 lightPos=vec3(10,20,10);
vec3 lightDir=normalize(lightPos-FragPos);
float diff=max(dot(Normal,lightDir),0.0);
vec3 diffuse=diff*vec3(1);
vec3 ambient=vec3(0.3);
vec3 result=(ambient+diffuse)*color*Color;
FragColor=vec4(result,1.0);
}";
var v=g.CreateShader(ShaderType.VertexShader);
g.ShaderSource(v,vs);g.CompileShader(v);
var f=g.CreateShader(ShaderType.FragmentShader);
g.ShaderSource(f,fs);g.CompileShader(f);
var s=g.CreateProgram();
g.AttachShader(s,v);g.AttachShader(s,f);
g.LinkProgram(s);
g.DeleteShader(v);g.DeleteShader(f);
return s;
}
public void Dispose(){
g?.DeleteVertexArray(va);g?.DeleteBuffer(vb);
g?.DeleteBuffer(vi);g?.DeleteProgram(sh);ig?.Dispose();
}
public static void Main(){
var w=Window.Create(WindowOptions.Default with{Size=new(1280,720),Title="FPS Enhanced"});
var fps=new FPS();
w.Load+=()=>{fps.Init(w,GL.GetApi(w));w.Update+=fps.Update;w.Render+=fps.Render;};
w.Closing+=fps.Dispose;
w.Run();
}
}