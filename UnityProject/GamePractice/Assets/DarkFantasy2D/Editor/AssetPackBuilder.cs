using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DarkFantasy2D.Editor
{
 public static class AssetPackBuilder
 {
  const string Root = "Assets/DarkFantasy2D";
  static readonly string Out = Environment.GetEnvironmentVariable("DARKFANTASY_EXPORT_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "DarkFantasy2D_AssetSet");
  static Material spriteMaterial;
  static Font font;
  static Sprite[] icons;
  static Dictionary<string, Sprite> shapes = new Dictionary<string, Sprite>();
  static GameObject[] effects;
  static GameObject worldMap;
  // World map dimensions in units; roughly 3x wider than the 16x9 camera view so the camera only ever sees a portion.
  const float MapW = 48f, MapH = 24f;
  static Color ink = new Color(.035f,.055f,.11f,1), panel = new Color(.13f,.18f,.31f,.98f), cyan = new Color(.2f,.75f,1), gold = new Color(1,.75f,.3f);
  [MenuItem("Tools/Dark Fantasy 2D/Rebuild Asset Pack")]
  public static void Build()
  {
   foreach (var dir in new[] { "Art/Parts", "Art/Generated", "Materials", "Animations", "Prefabs/Characters", "Prefabs/VFX", "Prefabs/UI", "Prefabs/Environment", "Scenes", "Documentation" }) Directory.CreateDirectory(Root + "/" + dir);
   AssetDatabase.Refresh();
   spriteMaterial = new Material(Shader.Find("DarkFantasy2D/UnlitTransparent"));
   AssetDatabase.CreateAsset(spriteMaterial, Root + "/Materials/CharacterUnlit.mat");
   font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Art/Interface.ttf");
   if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
   icons = Slice("UIIcons", 4, new[]{ "Gem","Coin","SlashSkill","HealSkill","Lock","Settings","Chest","Hourglass","Shop","Summon","Dungeon","Event","Chat","Shield","Skull","Close" });
   CreateShapes(); effects = CreateEffects();
   worldMap = CreateEnvironment();
   var chars = new List<GameObject>();
   chars.Add(CreateCharacter("Hero", 1f, 0)); chars.Add(CreateCharacter("Goblin", .86f, 1)); chars.Add(CreateCharacter("Ogre", 1.25f, 2));
   var hud = CreateHUD(chars[0]);
   CreateUIComponents();
   CreateHeroPortrait(chars[0]);
   CreateGrowthPanel();
   CreateScene(chars.ToArray(), hud);
   Validate(chars, hud);
   AssetDatabase.SaveAssets();
   Directory.CreateDirectory(Out);
   AssetDatabase.ExportPackage(Root, Out + "/DarkFantasy2D.unitypackage", ExportPackageOptions.Recurse);
   File.WriteAllText(Out + "/BUILD_COMPLETE.txt", "Unity " + Application.unityVersion + "\nBuilt " + DateTime.Now.ToString("O") + "\n3 character prefabs; 15 animation clips; 8 VFX prefabs; 9 UI prefabs.\nValidation: passed.\n");
   Debug.Log("DARK_FANTASY_BUILD_SUCCESS");
  }
  static Sprite[] Slice(string source, int grid, string[] names)
  {
   var path = Root + "/Art/" + source + ".png";
   var importer = (TextureImporter)AssetImporter.GetAtPath(path);
   importer.isReadable = true; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = 4096; importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.SaveAndReimport();
   var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path); var pixels = tex.GetPixels32();
   if (!pixels.Any(p => p.a == 0)) throw new Exception(source + " has no transparent pixels");
   var result = new Sprite[names.Length];
   for (int i=0;i<names.Length;i++)
   {
    int x0 = (i%grid)*tex.width/grid, x1=(i%grid+1)*tex.width/grid;
    int y0 = tex.height-(i/grid+1)*tex.height/grid, y1=tex.height-(i/grid)*tex.height/grid;
    if(grid==3){float[] xb={0,.355f,.655f,1};float[] yb={0,.355f,.67f,1};x0=(int)(xb[i%3]*tex.width);x1=(int)(xb[i%3+1]*tex.width);y0=tex.height-(int)(yb[i/3+1]*tex.height);y1=tex.height-(int)(yb[i/3]*tex.height);}
    int minX=x1,minY=y1,maxX=x0,maxY=y0;
    for(int y=y0;y<y1;y++) for(int x=x0;x<x1;x++) if(pixels[y*tex.width+x].a>20) { minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y); }
    minX=Math.Max(x0,minX-2);minY=Math.Max(y0,minY-2);maxX=Math.Min(x1-1,maxX+2);maxY=Math.Min(y1-1,maxY+2);
    var sub = new Texture2D(maxX-minX+1,maxY-minY+1,TextureFormat.RGBA32,false);
    sub.SetPixels(tex.GetPixels(minX,minY,sub.width,sub.height));sub.Apply();
    string outPath=Root+"/Art/Parts/"+source+"_"+names[i]+".png";File.WriteAllBytes(outPath,sub.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(sub);
    AssetDatabase.ImportAsset(outPath); var ti=(TextureImporter)AssetImporter.GetAtPath(outPath);
    ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=100;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.filterMode=FilterMode.Bilinear;ti.SaveAndReimport();
    result[i]=AssetDatabase.LoadAssetAtPath<Sprite>(outPath);
   }
   return result;
  }
  static Transform Bone(Transform parent,string name,Vector2 pos)
  { var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=pos;return t; }
  static void Skin(Transform bone,Sprite sprite,float height,int order,Vector2 pivot)
  {
   var go=new GameObject("Skin",typeof(SpriteRenderer));go.transform.SetParent(bone,false);
   var sr=go.GetComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=spriteMaterial;sr.sortingOrder=order;
   float scale=height/sprite.bounds.size.y;go.transform.localScale=Vector3.one*scale;
   go.transform.localPosition=new Vector3((.5f-pivot.x)*sprite.bounds.size.x*scale,(.5f-pivot.y)*height,0);
  }
  static GameObject CreateCharacter(string id,float scale,int type)
  {
   var p=Slice(id,3,new[]{"Head","Torso","Hair","UpperArm","Forearm","Weapon","RearLeg","FrontLeg","Cape"});
   var go=new GameObject(id+"_Right_Rig");
   var body=Bone(go.transform,"Root",new Vector2(0,.55f));
   var rear=Bone(body,"RearLeg",new Vector2(-.16f,0));Skin(rear,p[6],.55f,1,new Vector2(.45f,1));
   var front=Bone(body,"FrontLeg",new Vector2(.18f,0));Skin(front,p[7],.55f,4,new Vector2(.45f,1));
   var torso=Bone(body,"Torso",new Vector2(0,.58f));Skin(torso,p[1],.73f,5,new Vector2(.5f,1));
   var cape=Bone(torso,"Cape",new Vector2(-.12f,-.12f));Skin(cape,p[8],.70f,0,new Vector2(.85f,1));
   var head=Bone(torso,"Head",new Vector2(.03f,-.05f));Skin(head,p[0],type==2?1.40f:1.43f,10,new Vector2(.5f,.05f));
   var hair=Bone(head,"Hair",new Vector2(-.10f,.85f));Skin(hair,p[2],type==0?.72f:.45f,2,new Vector2(.5f,1));
   if(type==2) { hair.SetParent(torso,false);hair.localPosition=new Vector2(0,-.05f);hair.localRotation=Quaternion.identity; }
   var backArm=Bone(torso,"BackArm",new Vector2(-.20f,-.16f));Skin(backArm,p[3],.30f,2,new Vector2(.5f,1));backArm.localRotation=Quaternion.Euler(0,0,-6);
   var backHand=Bone(backArm,"Forearm",new Vector2(0,-.24f));Skin(backHand,p[4],.32f,2,new Vector2(.40f,1));backHand.localRotation=Quaternion.Euler(0,0,-8);
   // Rest pose: the weapon is held as drawn (in front, original sorting) with only a relaxed
   // shoulder and slight elbow bend for naturalness; brief face overlap mid-swing is accepted.
   var arm=Bone(torso,"Arm",new Vector2(.19f,-.15f));Skin(arm,p[3],.32f,12,new Vector2(.5f,1));arm.localRotation=Quaternion.Euler(0,0,6);
   var forearm=Bone(arm,"Forearm",new Vector2(0,-.24f));Skin(forearm,p[4],.34f,13,new Vector2(.40f,1));forearm.localRotation=Quaternion.Euler(0,0,8);
   var weapon=Bone(forearm,"Weapon",new Vector2(.09f,-.26f));Skin(weapon,p[5],type==0?1.6f:type==1?.95f:1.55f,12,new Vector2(.24f,.24f));weapon.localRotation=Quaternion.Euler(0,0,type==0?10:type==1?15:12);
   var socket=Bone(go.transform,"EffectSocket",new Vector2(1,.95f));
   var anim=go.AddComponent<Animator>(); anim.runtimeAnimatorController=CreateAnimator(id,go);
   var rig=go.AddComponent<CharacterRig>();rig.maxHealth=type==0?100:type==1?60:200;rig.effectSocket=socket;rig.slashPrefab=effects[0];rig.hitPrefab=effects[1];rig.deathPrefab=effects[3];
   var collider=go.AddComponent<CapsuleCollider2D>();collider.size=new Vector2(.85f,2.5f);collider.offset=new Vector2(0,1.25f);collider.isTrigger=true;
   go.transform.localScale=Vector3.one*scale;
   var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/Characters/"+go.name+".prefab");UnityEngine.Object.DestroyImmediate(go);return prefab;
  }
  static AnimatorController CreateAnimator(string id,GameObject rig)
  {
   string path=Root+"/Animations/"+id;Directory.CreateDirectory(path);AssetDatabase.Refresh();
   var ctrl=AnimatorController.CreateAnimatorControllerAtPath(path+"/"+id+".controller");ctrl.AddParameter("Speed",AnimatorControllerParameterType.Float);
   foreach(var trigger in new[]{"Attack","Hit","Die","Revive"})ctrl.AddParameter(trigger,AnimatorControllerParameterType.Trigger);
   var sm=ctrl.layers[0].stateMachine;var states=new Dictionary<string,AnimatorState>();
   foreach(var mode in new[]{"Idle","Walk","Attack","Hit","Death"})
   {
    float dur=mode=="Idle"?1.6f:mode=="Walk"?.65f:mode=="Attack"?.65f:mode=="Hit"?.3f:.75f;
    var clip=new AnimationClip{name=id+"_"+mode,frameRate=60};
    // Every clip keys the complete rest pose to make transitions and revival deterministic.
    foreach(var t in rig.GetComponentsInChildren<Transform>())
    {
     if(t==rig.transform||t.name=="Skin"||t.name=="EffectSocket")continue;
     var rel=AnimationUtility.CalculateTransformPath(t,rig.transform);
     float rest=RestZ(t);
     Curve(clip,rel,"localEulerAnglesRaw.z",dur,rest,rest,rest,rest,rest);
     Curve(clip,rel,"m_LocalPosition.x",dur,t.localPosition.x,t.localPosition.x,t.localPosition.x,t.localPosition.x,t.localPosition.x);
     Curve(clip,rel,"m_LocalPosition.y",dur,t.localPosition.y,t.localPosition.y,t.localPosition.y,t.localPosition.y,t.localPosition.y);
    }
    string body="Root",torso="Root/Torso",arm=torso+"/Arm",head=torso+"/Head";
    if(mode=="Idle") {Curve(clip,body,"m_LocalPosition.y",dur,.55f,.58f,.55f,.53f,.55f);Rot(clip,rig,head,dur,0,-2,0,2,0);Rot(clip,rig,torso+"/Cape",dur,0,4,0,-3,0);Rot(clip,rig,arm,dur,0,2,0,-2,0);}
    if(mode=="Walk")
    {Curve(clip,body,"m_LocalPosition.y",dur,.55f,.61f,.55f,.61f,.55f);Rot(clip,rig,body+"/RearLeg",dur,-23,0,23,0,-23);Rot(clip,rig,body+"/FrontLeg",dur,23,0,-23,0,23);Rot(clip,rig,arm,dur,-12,0,12,0,-12);Rot(clip,rig,torso+"/BackArm",dur,12,0,-12,0,12);}
    if(mode=="Attack")
    {Rot(clip,rig,arm,dur,0,-95,65,25,0);Rot(clip,rig,arm+"/Forearm",dur,0,-35,15,10,0);Rot(clip,rig,torso,dur,0,12,-17,-5,0);Curve(clip,body,"m_LocalPosition.x",dur,0,-.10f,.18f,.06f,0);AnimationUtility.SetAnimationEvents(clip,new[]{new AnimationEvent{time=dur*.48f,functionName="Strike"}});}
    if(mode=="Hit") {Rot(clip,rig,torso,dur,0,16,10,5,0);Curve(clip,body,"m_LocalPosition.x",dur,0,-.18f,-.12f,-.05f,0);}
    if(mode=="Death") {Rot(clip,rig,body,dur,0,10,45,80,88);Curve(clip,body,"m_LocalPosition.y",dur,.55f,.45f,.32f,.2f,.18f);}
    var setting=AnimationUtility.GetAnimationClipSettings(clip);setting.loopTime=mode=="Idle"||mode=="Walk";AnimationUtility.SetAnimationClipSettings(clip,setting);
    AssetDatabase.CreateAsset(clip,path+"/"+mode+".anim");var state=sm.AddState(mode);state.motion=clip;states[mode]=state;
   }
   sm.defaultState=states["Idle"];
   var walk=states["Idle"].AddTransition(states["Walk"]);walk.hasExitTime=false;walk.duration=.12f;walk.AddCondition(AnimatorConditionMode.Greater,.1f,"Speed");
   var idle=states["Walk"].AddTransition(states["Idle"]);idle.hasExitTime=false;idle.duration=.12f;idle.AddCondition(AnimatorConditionMode.Less,.1f,"Speed");
   foreach(var src in new[]{"Idle","Walk","Attack","Hit"}) foreach(var pair in new[]{new[]{"Attack","Attack"},new[]{"Hit","Hit"},new[]{"Die","Death"}})
   {
    if(src==pair[1])continue;var tr=states[src].AddTransition(states[pair[1]]);tr.hasExitTime=false;tr.duration=.06f;tr.AddCondition(AnimatorConditionMode.If,0,pair[0]);
   }
   foreach(var src in new[]{"Attack","Hit"}) {var tr=states[src].AddTransition(states["Idle"]);tr.hasExitTime=true;tr.exitTime=1;tr.duration=.08f;}
   var revive=sm.AddAnyStateTransition(states["Idle"]);revive.hasExitTime=false;revive.duration=.05f;revive.canTransitionToSelf=true;revive.AddCondition(AnimatorConditionMode.If,0,"Revive");
   return ctrl;
  }
  static void Curve(AnimationClip c,string path,string property,float duration,params float[] values)
  {var keys=new Keyframe[values.Length];for(int i=0;i<keys.Length;i++)keys[i]=new Keyframe(duration*i/(keys.Length-1),values[i]);c.SetCurve(path,typeof(Transform),property,new AnimationCurve(keys));}
  static float RestZ(Transform t) {float z=t.localEulerAngles.z;return z>180?z-360:z;}
  // Rotation curves are authored relative to the rest pose so bones keep their natural resting angles.
  static void Rot(AnimationClip c,GameObject rig,string path,float duration,params float[] values)
  {float rest=RestZ(rig.transform.Find(path));var offset=new float[values.Length];for(int i=0;i<values.Length;i++)offset[i]=values[i]+rest;Curve(c,path,"localEulerAnglesRaw.z",duration,offset);}
  static void CreateShapes()
  {
   foreach(var id in new[]{"Panel","Button","Circle","Soft","Spark","Ring","Cross","Slash"})
   {
    int n=256;var tex=new Texture2D(n,n,TextureFormat.RGBA32,false);var px=new Color[n*n];
    for(int y=0;y<n;y++)for(int x=0;x<n;x++)
    {
     float u=(x+.5f)/n*2-1,v=(y+.5f)/n*2-1,r=Mathf.Sqrt(u*u+v*v),a=0;Color color=Color.white;
     if(id=="Panel"||id=="Button")
     {float edge=Mathf.Max(Mathf.Abs(u),Mathf.Abs(v));a=edge<.99f?1:0;float bevel=Mathf.Clamp01((edge-.90f)/.07f);Vector2 q=new Vector2(Mathf.Abs(u)-.79f,Mathf.Abs(v)-.79f);float d=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-.2f;a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.01f,.01f,d));color=Color.white;if(d>-.045f)color=new Color(.52f,.57f,.69f);if(d>-.02f)color=new Color(.12f,.15f,.23f);}
     if(id=="Circle")a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.95f,1,r));
     if(id=="Soft")a=Mathf.Pow(Mathf.Clamp01(1-r),2.5f);
     if(id=="Spark") {float ray=Mathf.Min(Mathf.Abs(u)*2.4f+Mathf.Abs(v)*.25f,Mathf.Abs(v)*2.4f+Mathf.Abs(u)*.25f);a=Mathf.Clamp01(1-ray*4)*Mathf.Clamp01((1-r)*5);}
     if(id=="Ring")a=Mathf.Clamp01(1-Mathf.Abs(r-.76f)/.055f);
     if(id=="Cross")a=(Mathf.Abs(u)<.19f&&Mathf.Abs(v)<.7f||Mathf.Abs(v)<.19f&&Mathf.Abs(u)<.7f)?1:0;
     if(id=="Slash") {float inner=Mathf.Sqrt((u+.21f)*(u+.21f)+(v-.04f)*(v-.04f));a=Mathf.Clamp01((.97f-r)*30)*Mathf.Clamp01((inner-.79f)*30);a*=Mathf.Clamp01((u+.5f)*3);}
     color.a=a;px[y*n+x]=color;
    }
    tex.SetPixels(px);tex.Apply();var path=Root+"/Art/Generated/"+id+".png";File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
    var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=100;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.spriteBorder=(id=="Panel"||id=="Button")?new Vector4(28,28,28,28):Vector4.zero;ti.SaveAndReimport();shapes[id]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
   }
  }
  static Sprite GenSprite(string name,int size,Func<float,float,Color> paint,float ppu=100)
  {
   var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);var px=new Color[size*size];
   for(int y=0;y<size;y++)for(int x=0;x<size;x++)px[y*size+x]=paint((x+.5f)/size*2-1,(y+.5f)/size*2-1);
   tex.SetPixels(px);tex.Apply();var path=Root+"/Art/Generated/"+name+".png";File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(path);
   var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.spritePixelsPerUnit=ppu;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.filterMode=FilterMode.Bilinear;ti.SaveAndReimport();
   return AssetDatabase.LoadAssetAtPath<Sprite>(path);
  }
  static float FBM(float x,float y)
  {float s=0,a=.5f,f=1;for(int o=0;o<4;o++){s+=Mathf.PerlinNoise(x*f,y*f)*a;a*=.5f;f*=2.1f;}return s/.9375f;}
  static GameObject CreateEnvironment()
  {
   // Hand-painted forest floor in the reference style: domain-warped mossy zones over grey-green
   // earth, a warm reddish dirt arena, an asymmetric lime accent region and heavy dark borders.
   int W=4096,H=2048;
   var tex=new Texture2D(W,H,TextureFormat.RGBA32,false);var px=new Color[W*H];
   for(int y=0;y<H;y++)for(int x=0;x<W;x++)
   {
    float u=(x+.5f)/W,v=(y+.5f)/H,nx=u*2f;
    // Domain warp makes every zone boundary flow organically instead of reading as raw noise blobs.
    float wx=nx+(FBM(nx*1.6f+13.7f,v*1.6f+41.2f)-.5f)*.55f;
    float wy=v+(FBM(nx*1.6f+71.9f,v*1.6f+5.3f)-.5f)*.55f;
    float zone=FBM(wx*1.3f+3.1f,wy*1.3f+9.7f);
    float detail=FBM(wx*10f+23f,wy*10f+17f);
    float det2=FBM(wx*24f+47f,wy*24f+61f);
    float grain=Mathf.PerlinNoise(nx*70f+9f,v*70f+33f);
    var rockGreen=Color.Lerp(new Color(.17f,.21f,.16f),new Color(.25f,.29f,.21f),detail);
    var midMoss=Color.Lerp(new Color(.19f,.31f,.15f),new Color(.27f,.39f,.17f),detail);
    var brightMoss=Color.Lerp(new Color(.33f,.47f,.18f),new Color(.45f,.55f,.22f),detail);
    var c=zone<.42f?Color.Lerp(rockGreen,midMoss,Mathf.InverseLerp(.22f,.42f,zone))
         :Color.Lerp(midMoss,brightMoss,Mathf.InverseLerp(.42f,.72f,zone));
    // Asymmetric lime accent field (right of the arena in the reference).
    float acc=FBM((wx-1.45f)*1.1f+55f,(wy-.42f)*1.1f+81f);
    if(acc>.60f&&wx>1.05f)c=Color.Lerp(c,new Color(.58f,.58f,.24f),Mathf.Clamp01((acc-.60f)/.22f)*.75f);
    // Neutral grey-brown dirt arena (the reference's red tint is a skill effect, not ground color).
    float dxn=(u-.5f)/.11f,dyn=(v-.5f)/.14f;
    float d=Mathf.Sqrt(dxn*dxn+dyn*dyn)+(FBM(nx*3.4f+77f,v*3.4f+66f)-.5f)*.34f;
    if(d<1.16f)c=Color.Lerp(c,new Color(.42f,.46f,.20f),Mathf.Clamp01((1.16f-d)/.16f)*.8f);
    if(d<1f)
    {
     var dirt=Color.Lerp(new Color(.40f,.33f,.27f),new Color(.52f,.45f,.37f),detail);
     dirt=Color.Lerp(dirt,new Color(.56f,.49f,.41f),Mathf.Clamp01((.55f-d)/.55f)*.5f);
     dirt=Color.Lerp(dirt,new Color(.33f,.27f,.22f),Mathf.Clamp01((d-.74f)/.26f)*.6f);
     c=Color.Lerp(c,dirt,Mathf.Clamp01((1f-d)/.06f));
    }
    // Painterly value mottling at two scales + fine canvas grain, no posterize banding.
    c*=.84f+detail*.32f;c*=.90f+det2*.20f;c*=.965f+grain*.07f;
    // Dark border mass: vignette plus warped shadow blobs that read as rocks and roots.
    float edge=Mathf.Max(Mathf.Abs(u-.5f),Mathf.Abs(v-.5f))*2f;
    float shadow=FBM(wx*2.4f+91f,wy*2.4f+37f);
    if(edge>.55f)
    {
     float k=Mathf.SmoothStep(0,1,(edge-.55f)/.45f);
     c*=1f-k*.62f;
     if(shadow>.52f)c*=1f-k*Mathf.Clamp01((shadow-.52f)/.3f)*.5f;
    }
    c.r=Mathf.Clamp01(c.r);c.g=Mathf.Clamp01(c.g);c.b=Mathf.Clamp01(c.b);
    c.a=1;px[y*W+x]=c;
   }
   tex.SetPixels(px);tex.Apply();var groundPath=Root+"/Art/Generated/WorldGround.png";File.WriteAllBytes(groundPath,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(groundPath);
   var gi=(TextureImporter)AssetImporter.GetAtPath(groundPath);gi.textureType=TextureImporterType.Sprite;gi.spriteImportMode=SpriteImportMode.Single;gi.spritePixelsPerUnit=W/MapW;gi.maxTextureSize=4096;gi.mipmapEnabled=false;gi.alphaIsTransparency=true;gi.textureCompression=TextureImporterCompression.Uncompressed;gi.filterMode=FilterMode.Bilinear;gi.SaveAndReimport();
   var ground=AssetDatabase.LoadAssetAtPath<Sprite>(groundPath);
   var rock=GenSprite("Prop_Rock",96,(gx,gy)=>
   {
    float ang=Mathf.Atan2(gy,gx);float wob=1f+.12f*Mathf.Sin(ang*3.3f+1.2f)+.08f*Mathf.Sin(ang*5.7f);
    float r=Mathf.Sqrt(gx*gx+gy*gy*1.55f)/wob;if(r>.82f)return Color.clear;
    var col=Color.Lerp(new Color(.30f,.33f,.40f),new Color(.55f,.58f,.66f),Mathf.Clamp01(.5f+(gy+ -gx*.3f)*.8f));
    if(r>.70f)col*= .55f;return new Color(col.r,col.g,col.b,1);
   });
   var flower=GenSprite("Prop_Flower",64,(gx,gy)=>
   {
    for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5+.5f;float fx=gx-Mathf.Cos(a)*.42f,fy=gy-Mathf.Sin(a)*.42f;if(fx*fx+fy*fy<.09f)return new Color(.95f,.93f,.88f,1);}
    if(gx*gx+gy*gy<.08f)return new Color(1f,.8f,.3f,1);return Color.clear;
   });
   var tuft=GenSprite("Prop_Grass",96,(gx,gy)=>
   {
    float yy=(gy+1)/2;
    for(int i=-1;i<=1;i++){float bx=gx-i*(.18f+.38f*yy*yy);float w=.15f*(1f-yy)*(1f-yy*.4f);if(Mathf.Abs(bx)<w)
     return Color.Lerp(new Color(.13f,.25f,.11f,1),new Color(.32f,.46f,.17f,1),yy);}
    return Color.clear;
   });
   var root=new GameObject("WorldMap");
   var g=new GameObject("Ground",typeof(SpriteRenderer));g.transform.SetParent(root.transform,false);
   var gr=g.GetComponent<SpriteRenderer>();gr.sprite=ground;gr.sharedMaterial=spriteMaterial;gr.sortingOrder=-100;
   // Deterministic prop scatter; the dirt arena stays mostly clear so combat reads well.
   var rng=new System.Random(20240915);var props=new[]{rock,flower,tuft};int placed=0;
   while(placed<260)
   {
    float wx=(float)(rng.NextDouble()*(MapW-3)-(MapW-3)/2),wy=(float)(rng.NextDouble()*(MapH-3)-(MapH-3)/2);
    float ax=wx/(.11f*MapW),ay=wy/(.14f*MapH);bool inArena=ax*ax+ay*ay<1.45f;
    int kind=rng.Next(0,10);var sprite=kind<3?props[0]:kind<6?props[1]:props[2];
    if(inArena&&!(kind<3&&rng.Next(0,4)==0))continue;
    var prop=new GameObject("Prop_"+placed,typeof(SpriteRenderer));prop.transform.SetParent(root.transform,false);
    var pr=prop.GetComponent<SpriteRenderer>();pr.sprite=sprite;pr.sharedMaterial=spriteMaterial;pr.sortingOrder=-90;pr.flipX=rng.Next(0,2)==0;
    float s=sprite==props[0]?.45f+(float)rng.NextDouble()*.5f:sprite==props[1]?.30f+(float)rng.NextDouble()*.22f:.40f+(float)rng.NextDouble()*.40f;
    prop.transform.localPosition=new Vector3(wx,wy,0);prop.transform.localScale=Vector3.one*s;
    float tint=.8f+(float)rng.NextDouble()*.25f;pr.color=new Color(tint,tint,tint,1);
    placed++;
   }
   var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Environment/WorldMap.prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
  }
  static ParticleSystem Particle(Transform parent,string name,string shape,Color color,int count,float life,float size,float speed,bool loop=false)
  {
   var go=new GameObject(name,typeof(ParticleSystem));if(parent)go.transform.SetParent(parent,false);var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var main=ps.main;main.duration=loop?1.5f:1.2f;main.loop=loop;main.startLifetime=life;main.startSpeed=speed;main.startSize=new ParticleSystem.MinMaxCurve(size*.7f,size);main.startColor=color;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.maxParticles=160;main.playOnAwake=true;
   var emission=ps.emission;emission.rateOverTime=loop?count:0;if(!loop)emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
   var sh=ps.shape;sh.shapeType=ParticleSystemShapeType.Circle;sh.radius=.12f;sh.radiusThickness=1;
   var col=ps.colorOverLifetime;col.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(.7f,.5f),new GradientAlphaKey(0,1)});col.color=gradient;
   var sz=ps.sizeOverLifetime;sz.enabled=true;sz.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.4f),new Keyframe(.2f,1),new Keyframe(1,.1f)));
   var renderer=ps.GetComponent<ParticleSystemRenderer>();var matPath=Root+"/Materials/VFX_"+shape+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(spriteMaterial);mat.mainTexture=shapes[shape].texture;AssetDatabase.CreateAsset(mat,matPath);}renderer.sharedMaterial=mat;renderer.sortingOrder=50;renderer.renderMode=ParticleSystemRenderMode.Billboard;
   ps.useAutoRandomSeed=false;ps.randomSeed=42;return ps;
  }
  static GameObject[] CreateEffects()
  {
   var result=new List<GameObject>();int index=0;
   foreach(var id in new[]{"Slash","HitSpark","Impact","DeathSmoke","FootstepDust","Heal","SkillCharge","Portal"})
   {
    var root=new GameObject("VFX_"+id);ParticleSystem ps=null;
    if(index==0) {ps=Particle(root.transform,"Crescent","Slash",new Color(1,.12f,.25f),1,.28f,2.5f,0);Particle(root.transform,"Embers","Spark",new Color(1,.35f,.3f),16,.35f,.18f,3);}
    if(index==1) {ps=Particle(root.transform,"WhiteCore","Spark",Color.white,2,.15f,.95f,0);Particle(root.transform,"CrimsonSparks","Spark",new Color(1,.24f,.18f),24,.4f,.25f,3);}
    if(index==2) {ps=Particle(root.transform,"ShockRing","Ring",new Color(1,.7f,.3f),1,.45f,2.3f,0);Particle(root.transform,"Debris","Spark",new Color(1,.38f,.16f),30,.6f,.24f,3.2f);}
    if(index==3) {ps=Particle(root.transform,"Smoke","Soft",new Color(.45f,.40f,.55f,.8f),26,1.1f,1.3f,1.2f);var vel=ps.velocityOverLifetime;vel.enabled=true;vel.y=.8f;}
    if(index==4) {ps=Particle(root.transform,"Dust","Soft",new Color(.65f,.61f,.7f,.55f),12,.6f,.65f,.7f);var sh=ps.shape;sh.scale=new Vector3(1,.15f,1);}
    if(index==5) {ps=Particle(root.transform,"Crosses","Cross",new Color(.3f,1,.45f),12,.85f,.3f,.5f);var vel=ps.velocityOverLifetime;vel.enabled=true;vel.y=1;Particle(root.transform,"HealRing","Ring",new Color(.25f,1,.4f),1,.85f,2,0);}
    if(index==6) {ps=Particle(root.transform,"Orbit","Ring",new Color(.1f,.5f,1),2,1,1.9f,0);Particle(root.transform,"Energy","Spark",new Color(.3f,.65f,1),30,1,.18f,-1.1f);var sh=root.transform.GetChild(1).GetComponent<ParticleSystem>().shape;sh.radius=1.1f;}
    if(index==7) {ps=Particle(root.transform,"PortalRing","Ring",new Color(.45f,.3f,1),3,1.1f,2,0);Particle(root.transform,"Motes","Soft",new Color(.3f,.6f,1),35,1,.24f,.5f);}
    // Root lifetime exceeds every child's final live particle; safe standalone auto-cleanup.
    var cleanup=root.AddComponent<EffectLifetime>();cleanup.seconds=2.8f;
    var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/VFX/"+root.name+".prefab");result.Add(prefab);UnityEngine.Object.DestroyImmediate(root);index++;
   }
   return result.ToArray();
  }
  static RectTransform Rect(Transform parent,string name,Vector2 pos,Vector2 size,Vector2 anchor)
  {
   var go=new GameObject(name,typeof(RectTransform));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=anchor;rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=pos;rt.sizeDelta=size;return rt;
  }
  static Image ImageUI(Transform parent,string name,Vector2 pos,Vector2 size,Color color,Sprite sprite=null,Vector2? anchor=null)
  {var rt=Rect(parent,name,pos,size,anchor??new Vector2(.5f,.5f));var img=rt.gameObject.AddComponent<Image>();img.sprite=sprite?sprite:shapes["Panel"];img.color=color;img.type=img.sprite.border!=Vector4.zero?Image.Type.Sliced:Image.Type.Simple;img.raycastTarget=false;return img;}
  static Text Label(Transform parent,string name,string text,Vector2 pos,Vector2 size,int sizeFont,Color color,TextAnchor alignment=TextAnchor.MiddleLeft,Vector2? anchor=null)
  {var rt=Rect(parent,name,pos,size,anchor??new Vector2(.5f,.5f));var t=rt.gameObject.AddComponent<Text>();t.font=font;t.text=text;t.fontSize=sizeFont;t.color=color;t.alignment=alignment;t.raycastTarget=false;t.supportRichText=true;var outline=rt.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.03f,.04f,.07f,.9f);outline.effectDistance=new Vector2(1,-1);return t;}
  static Button ButtonUI(Transform parent,string name,string label,Vector2 pos,Vector2 size,Color color,Sprite icon=null,Vector2? anchor=null)
  {
   var img=ImageUI(parent,name,pos,size,color,shapes["Button"],anchor);img.raycastTarget=true;var btn=img.gameObject.AddComponent<Button>();btn.targetGraphic=img;
   var colors=btn.colors;colors.highlightedColor=new Color(1.2f,1.2f,1.2f);colors.pressedColor=new Color(.65f,.75f,.9f);colors.disabledColor=new Color(.5f,.5f,.5f,.6f);btn.colors=colors;
   if(icon)ImageUI(img.transform,"Icon",new Vector2(0,label.Length>0?8:0),new Vector2(size.y*.62f,size.y*.62f),Color.white,icon);
   if(label.Length>0)Label(img.transform,"Label",label,new Vector2(0,icon?-size.y*.32f:0),new Vector2(size.x-8,26),icon?13:15,Color.white,TextAnchor.MiddleCenter);
   return btn;
  }
  static Image Bar(Transform parent,string name,Vector2 pos,Vector2 size,Color color,float amount)
  {var bg=ImageUI(parent,name,pos,size,new Color(.02f,.03f,.08f),shapes["Panel"]);var fill=ImageUI(bg.transform,"Fill",Vector2.zero,size-new Vector2(6,6),color,shapes["Panel"]);fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillAmount=amount;return fill;}
  static GameObject CreateHUD(GameObject hero)
  {
   var root=new GameObject("CombatHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CombatHUD));
   var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
   var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
   var hud=root.GetComponent<CombatHUD>();
   var top=ImageUI(root.transform,"TopHUD",new Vector2(0,-69),new Vector2(1536,110),panel,anchor:new Vector2(.5f,1));
   var portrait=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Parts/Hero_Head.png");
   ImageUI(top.transform,"PortraitFrame",new Vector2(-697,0),new Vector2(92,92),cyan,shapes["Circle"]);
   ImageUI(top.transform,"PortraitDark",new Vector2(-697,0),new Vector2(85,85),ink,shapes["Circle"]);
   var face=ImageUI(top.transform,"Portrait",new Vector2(-697,3),new Vector2(84,84),Color.white,portrait);face.preserveAspect=true;
   Label(top.transform,"Name","은빛 사신",new Vector2(-473,30),new Vector2(340,24),18,Color.white);
   hud.levelText=Label(top.transform,"Level","LV. 1",new Vector2(-697,-36),new Vector2(85,25),15,gold,TextAnchor.MiddleCenter);
   hud.healthFill=Bar(top.transform,"Health",new Vector2(-475,3),new Vector2(335,27),new Color(.95f,.12f,.23f),1);
   hud.healthText=Label(hud.healthFill.transform.parent,"HealthValue","100 / 100",Vector2.zero,new Vector2(325,26),16,Color.white,TextAnchor.MiddleCenter);
   hud.experienceFill=Bar(top.transform,"Experience",new Vector2(-475,-22),new Vector2(335,12),new Color(.35f,.85f,.25f),.6f);
   ImageUI(top.transform,"Coin",new Vector2(-222,16),new Vector2(36,36),Color.white,icons[1]);hud.coinsText=Label(top.transform,"Coins","1,004",new Vector2(-152,16),new Vector2(95,30),19,gold);
   ImageUI(top.transform,"Gem",new Vector2(-222,-22),new Vector2(32,32),Color.white,icons[0]);hud.gemsText=Label(top.transform,"Gems","500",new Vector2(-152,-22),new Vector2(95,30),19,cyan);
   hud.stageText=Label(top.transform,"Stage","1-1 단계",new Vector2(94,20),new Vector2(320,30),21,Color.white,TextAnchor.MiddleCenter);
   Label(top.transform,"Progress","처치 목표    25 / 25",new Vector2(94,-15),new Vector2(310,25),14,cyan,TextAnchor.MiddleCenter);
   for(int i=0;i<4;i++)ButtonUI(top.transform,"Menu_"+i,new[]{"상점","소환","던전","이벤트"}[i],new Vector2(370+i*94,0),new Vector2(82,87),new Color(.16f,.25f,.4f),icons[8+i]);
   var bottom=ImageUI(root.transform,"ActionDock",new Vector2(0,76),new Vector2(1536,112),panel,anchor:new Vector2(.5f,0));
   ImageUI(bottom.transform,"QuestIcon",new Vector2(-701,0),new Vector2(62,62),Color.white,icons[0]);
   Label(bottom.transform,"QuestTitle","가이드 3",new Vector2(-439,23),new Vector2(440,24),17,gold);
   Label(bottom.transform,"QuestText","훈련 - 공격력을 올려보세요",new Vector2(-439,-3),new Vector2(440,24),15,Color.white);
   Bar(bottom.transform,"QuestProgress",new Vector2(-440,-30),new Vector2(435,12),new Color(.25f,.85f,.4f),.66f);
   hud.autoButton=ButtonUI(bottom.transform,"AutoButton","자동",new Vector2(352,0),new Vector2(86,86),new Color(.2f,.28f,.5f));
   hud.healButton=ButtonUI(bottom.transform,"HealButton","회복",new Vector2(458,0),new Vector2(92,92),new Color(.12f,.32f,.24f),icons[3]);
   hud.attackButton=ButtonUI(bottom.transform,"AttackButton","공격",new Vector2(570,0),new Vector2(106,96),new Color(.90f,.65f,.15f),icons[2]);
   hud.cooldownFill=ImageUI(hud.attackButton.transform,"Cooldown",Vector2.zero,new Vector2(102,92),new Color(.03f,.03f,.07f,.7f),shapes["Circle"]);hud.cooldownFill.type=Image.Type.Filled;hud.cooldownFill.fillMethod=Image.FillMethod.Radial360;hud.cooldownFill.fillAmount=0;
   ButtonUI(bottom.transform,"ChatButton","채팅",new Vector2(687,0),new Vector2(82,86),new Color(.16f,.25f,.4f),icons[12]);
   var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/UI/CombatHUD.prefab");UnityEngine.Object.DestroyImmediate(root);return prefab;
  }
  static void SaveUI(GameObject go,string name) {PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/UI/"+name+".prefab");UnityEngine.Object.DestroyImmediate(go);}
  static void CreateUIComponents()
  {
   SaveUI(ButtonUI(null,"PrimaryButton","확인",Vector2.zero,new Vector2(220,64),new Color(1,.65f,.16f)).gameObject,"PrimaryButton");
   SaveUI(ButtonUI(null,"SecondaryButton","취소",Vector2.zero,new Vector2(220,64),new Color(.19f,.29f,.46f)).gameObject,"SecondaryButton");
   SaveUI(ButtonUI(null,"CloseButton","",Vector2.zero,new Vector2(64,64),new Color(.15f,.23f,.38f),icons[15]).gameObject,"CloseButton");
   var frame=ImageUI(null,"ItemFrame",Vector2.zero,new Vector2(96,96),gold);ImageUI(frame.transform,"Inner",Vector2.zero,new Vector2(83,83),ink);SaveUI(frame.gameObject,"ItemFrame");
   var health=Bar(null,"WorldHealthBar",Vector2.zero,new Vector2(180,20),new Color(1,.16f,.25f),1);SaveUI(health.transform.parent.gameObject,"HealthBar");
   var toggleBg=ImageUI(null,"Toggle",Vector2.zero,new Vector2(100,48),new Color(.14f,.27f,.45f));var check=ImageUI(toggleBg.transform,"Check",new Vector2(24,0),new Vector2(38,38),cyan,shapes["Circle"]);var toggle=toggleBg.gameObject.AddComponent<Toggle>();toggle.targetGraphic=toggleBg;toggle.graphic=check;toggle.isOn=true;toggleBg.raycastTarget=true;SaveUI(toggleBg.gameObject,"Toggle");
   var sliderBg=ImageUI(null,"Slider",Vector2.zero,new Vector2(260,22),panel);var fill=ImageUI(sliderBg.transform,"Fill",Vector2.zero,new Vector2(250,12),cyan);var handle=ImageUI(sliderBg.transform,"Handle",Vector2.zero,new Vector2(30,30),Color.white,shapes["Circle"]);var slider=sliderBg.gameObject.AddComponent<Slider>();slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.value=.6f;sliderBg.raycastTarget=true;handle.raycastTarget=true;SaveUI(sliderBg.gameObject,"Slider");
  }
  static void CreateHeroPortrait(GameObject prefab)
  {
   var copy=UnityEngine.Object.Instantiate(prefab);copy.transform.position=new Vector3(100,0,0);
   foreach(var t in copy.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
   var go=new GameObject("PortraitCamera",typeof(Camera));var cam=go.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=1.55f;cam.transform.position=new Vector3(100.38f,1.23f,-10);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;cam.cullingMask=1<<30;
   string path=Root+"/Art/Generated/HeroPortrait.png";RenderPreview(cam,path,640,640);UnityEngine.Object.DestroyImmediate(copy);UnityEngine.Object.DestroyImmediate(go);
   AssetDatabase.ImportAsset(path);var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.alphaIsTransparency=true;ti.mipmapEnabled=false;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
  }
  static void CreateGrowthPanel()
  {
   var root=Rect(null,"GrowthPanel",Vector2.zero,new Vector2(1600,900),new Vector2(.5f,.5f)).gameObject;
   var canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;root.AddComponent<GraphicRaycaster>();var scaler=root.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
   var dim=ImageUI(root.transform,"Dim",Vector2.zero,new Vector2(2400,1400),new Color(0,0,0,.65f),shapes["Panel"]);dim.raycastTarget=true;
   var box=ImageUI(root.transform,"Window",Vector2.zero,new Vector2(1400,730),new Color(.12f,.16f,.29f));box.raycastTarget=true;
   ImageUI(box.transform,"Header",new Vector2(0,316),new Vector2(1386,90),new Color(.17f,.23f,.38f));
   Label(box.transform,"Title","성장",new Vector2(-542,316),new Vector2(230,55),32,gold);
   var gp=root.AddComponent<GrowthPanel>();gp.close=ButtonUI(box.transform,"Close","",new Vector2(647,316),new Vector2(66,66),new Color(.17f,.23f,.38f),icons[15]);
   ImageUI(box.transform,"Coin",new Vector2(440,316),new Vector2(44,44),Color.white,icons[1]);gp.currencyText=Label(box.transform,"Currency","1,004",new Vector2(525,316),new Vector2(116,40),23,Color.white);
   var tab=ButtonUI(box.transform,"TrainingTab","훈련",new Vector2(-583,205),new Vector2(204,80),new Color(.25f,.3f,.6f));
   foreach(var pair in new[]{new[]{"특성","110"},new[]{"속성 강화","15"}}){var b=ButtonUI(box.transform,"LockedTab",pair[0],new Vector2(-583,float.Parse(pair[1])),new Vector2(204,80),new Color(.09f,.13f,.22f));b.interactable=false;ImageUI(b.transform,"Lock",new Vector2(75,0),new Vector2(30,30),Color.white,icons[4]);}
   ImageUI(box.transform,"PortraitPanel",new Vector2(-231,-35),new Vector2(460,582),new Color(.15f,.17f,.35f));
   var portrait=ImageUI(box.transform,"HeroPortrait",new Vector2(-231,32),new Vector2(350,370),Color.white,AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Generated/HeroPortrait.png"));portrait.preserveAspect=true;
   Label(box.transform,"HeroLevel","Lv. 1",new Vector2(-231,-156),new Vector2(180,40),27,gold,TextAnchor.MiddleCenter);
   Bar(box.transform,"Experience",new Vector2(-231,-206),new Vector2(356,25),new Color(.28f,.9f,.1f),1);
   var level=ButtonUI(box.transform,"LevelUp","레벨업",new Vector2(-231,-275),new Vector2(242,63),new Color(1,.79f,.28f));level.interactable=false;ImageUI(level.transform,"Lock",new Vector2(101,12),new Vector2(31,31),Color.white,icons[4]);
   ImageUI(box.transform,"Details",new Vector2(326,-35),new Vector2(620,582),new Color(.05f,.07f,.12f));
   Label(box.transform,"Rank","1단계",new Vector2(319,206),new Vector2(540,48),30,Color.white);
   Label(box.transform,"RankHelp","모든 훈련 MAX 달성 시 다음 단계 승급",new Vector2(319,157),new Vector2(540,42),18,new Color(.65f,.82f,.9f));
   for(int i=0;i<2;i++)
   {
    var row=ImageUI(box.transform,"StatRow"+i,new Vector2(326,52-i*119),new Vector2(590,105),new Color(.18f,.24f,.39f));ImageUI(row.transform,"Icon",new Vector2(-248,0),new Vector2(64,64),Color.white,icons[i==0?2:13]);
    var text=Label(row.transform,"Stat",i==0?"공격력\n+3  Lv.1":"체력\n+10  Lv.1",new Vector2(-71,0),new Vector2(230,78),24,Color.white);
    var train=ButtonUI(row.transform,"Train","훈련  /  5",new Vector2(199,0),new Vector2(169,80),new Color(1,.79f,.26f));
    if(i==0){gp.attackText=text;gp.attackTrain=train;}else{gp.healthText=text;gp.healthTrain=train;}
   }
   Label(box.transform,"Hint","훈련 버튼으로 능력치와 재화 변화를 확인하세요",new Vector2(327,-276),new Vector2(547,45),16,new Color(.66f,.75f,.9f));
   SaveUI(root,"GrowthPanel");
  }
  static void CreateScene(GameObject[] prefabs,GameObject hudPrefab)
  {
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var cameraGO=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraGO.tag="MainCamera";var cam=cameraGO.GetComponent<Camera>();cam.orthographic=true;cam.orthographicSize=4.5f;cam.transform.position=new Vector3(0,0,-10);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=ink;
   // The world map is much larger than the camera view, so the camera only ever frames a slice of it.
   var map=(GameObject)PrefabUtility.InstantiatePrefab(worldMap);map.transform.position=Vector3.zero;
   for(int i=0;i<3;i++)
   {
    float x=(i-1)*5;
    var floor=new GameObject("ShadowBlob_"+i,typeof(SpriteRenderer));var fs=floor.GetComponent<SpriteRenderer>();fs.sprite=shapes["Soft"];fs.sharedMaterial=spriteMaterial;fs.color=new Color(0,0,0,.45f);fs.sortingOrder=-10;floor.transform.position=new Vector3(x,-1.55f,0);floor.transform.localScale=new Vector3(1.5f,.4f,1);
   }
   var director=new GameObject("Showcase",typeof(AssetShowcase)).GetComponent<AssetShowcase>();director.characters=new CharacterRig[3];
   for(int i=0;i<3;i++){var go=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);go.transform.position=new Vector3((i-1)*5,-1.5f,0);director.characters[i]=go.GetComponent<CharacterRig>();}
   var hudGO=(GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);director.hud=hudGO.GetComponent<CombatHUD>();
   var canvas=hudGO.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=5;
   var growth=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/UI/GrowthPanel.prefab"));var modalCanvas=growth.GetComponent<Canvas>();modalCanvas.renderMode=RenderMode.ScreenSpaceCamera;modalCanvas.worldCamera=cam;modalCanvas.planeDistance=1;modalCanvas.sortingOrder=1000;growth.SetActive(false);director.growthPanel=growth;director.growthButton=ButtonUI(hudGO.transform,"GrowthButton","성장",new Vector2(-30,76),new Vector2(126,70),new Color(1,.78f,.25f),anchor:new Vector2(.5f,0));
   var overlay=Rect(hudGO.transform,"ShowcaseControls",Vector2.zero,new Vector2(1600,900),new Vector2(.5f,.5f));
   Label(overlay,"CollectionTitle","문폴  /  캐릭터 · 전투 에셋 세트",new Vector2(-280,282),new Vector2(960,38),25,Color.white);
   Label(overlay,"Edition","SD 스타일  ·  우측 기본 방향",new Vector2(570,282),new Vector2(380,32),14,cyan,TextAnchor.MiddleRight);
   var titles=new[]{"01 / 은빛 사신","02 / 고블린 정찰병","03 / 오우거 전사"};
   var subs=new[]{"영웅  /  낫","경량 근접  /  단검","중량 근접  /  곤봉"};
   for(int i=0;i<3;i++)
   {Label(overlay,"Title"+i,titles[i],new Vector2((i-1)*500,206),new Vector2(420,32),21,i==0?gold:cyan,TextAnchor.MiddleCenter);Label(overlay,"Subtitle"+i,subs[i],new Vector2((i-1)*500,-189),new Vector2(440,26),13,new Color(.65f,.74f,.87f),TextAnchor.MiddleCenter);}
   director.animationButtons=new Button[6];for(int i=0;i<6;i++)director.animationButtons[i]=ButtonUI(overlay,"Anim_"+i,new[]{"대기","이동","공격","피격","사망","부활"}[i],new Vector2(-635+i*149,-249),new Vector2(137,42),i==2?new Color(.88f,.65f,.18f):new Color(.18f,.28f,.43f));
   director.statusText=Label(overlay,"Status","애니메이션과 이펙트 버튼을 눌러 확인하세요",new Vector2(-245,-290),new Vector2(930,24),13,cyan);
   director.effects=effects;director.effectButtons=new Button[8];for(int i=0;i<8;i++)director.effectButtons[i]=ButtonUI(overlay,"FX_"+i,new[]{"베기","피격","충격","연기","먼지","회복","충전","포털"}[i],new Vector2(334+(i%4)*111,-240-(i/4)*46),new Vector2(101,37),new Color(.14f,.24f,.36f));
   director.effectOrigin=Bone(null,"VFX Preview Origin",new Vector2(0,-.3f));
   var events=new GameObject("EventSystem",typeof(EventSystem),typeof(ShowcaseInputSetup));
#if ENABLE_INPUT_SYSTEM
   events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
   events.AddComponent<StandaloneInputModule>();
#endif
   EditorSceneManager.SaveScene(scene,Root+"/Scenes/AssetShowcase.unity");
   EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"/Scenes/AssetShowcase.unity",true)};
   RenderPreview(cam,Out+"/Preview.png",1600,900);
   growth.SetActive(true);RenderPreview(cam,Out+"/Preview_Growth.png",1600,900);growth.SetActive(false);
   // A second export proves the articulated attack pose is materially different.
   for(int i=0;i<3;i++){var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Animations/"+new[]{"Hero","Goblin","Ogre"}[i]+"/Attack.anim");clip.SampleAnimation(director.characters[i].gameObject,.32f);}
   RenderPreview(cam,Out+"/Preview_Attack.png",1600,900);
   EditorSceneManager.OpenScene(Root+"/Scenes/AssetShowcase.unity");
  }
  static void RenderPreview(Camera cam,string path,int width,int height)
  {
   var rt=new RenderTexture(width,height,24);rt.Create();cam.targetTexture=rt;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
   var tex=new Texture2D(width,height,TextureFormat.RGBA32,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
   cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
  }
  static void Validate(List<GameObject> chars,GameObject hud)
  {
   var report=new List<string>{"Unity "+Application.unityVersion,"Asset validation"};
   foreach(var prefab in chars)
   {
    if(prefab.GetComponentsInChildren<SpriteRenderer>().Length<9)throw new Exception("Missing rig parts");
    if(prefab.GetComponentsInChildren<SpriteRenderer>().Any(x=>!x.sprite||!x.sharedMaterial))throw new Exception("Missing character artwork");
    var ctrl=prefab.GetComponent<Animator>().runtimeAnimatorController;if(ctrl.animationClips.Length!=5)throw new Exception("Expected 5 animations");
    var rig=prefab.GetComponent<CharacterRig>();if(!rig.effectSocket||!rig.slashPrefab||!rig.hitPrefab||!rig.deathPrefab)throw new Exception("Missing rig effects");
    foreach(var clip in ctrl.animationClips)if(AnimationUtility.GetCurveBindings(clip).Length<20)throw new Exception("Incomplete pose: "+clip.name);
    var clone=UnityEngine.Object.Instantiate(prefab);var arm=clone.transform.Find("Root/Torso/Arm");float before=arm.localEulerAngles.z;ctrl.animationClips.First(c=>c.name == "Attack" || c.name.EndsWith("_Attack")).SampleAnimation(clone,.16f);if(Mathf.Abs(Mathf.DeltaAngle(before,arm.localEulerAngles.z))<20)throw new Exception("Attack does not articulate arm");UnityEngine.Object.DestroyImmediate(clone);
    report.Add("PASS "+prefab.name+": sprites, bone articulation, 5 clips, effect references");
   }
   foreach(var effect in effects)
   {var clone=UnityEngine.Object.Instantiate(effect);int count=0;foreach(var ps in clone.GetComponentsInChildren<ParticleSystem>()){ps.Simulate(.1f,false,true);count+=ps.particleCount;if(!ps.GetComponent<ParticleSystemRenderer>().sharedMaterial)throw new Exception("Missing particle material");}if(count==0)throw new Exception("Empty effect "+effect.name);UnityEngine.Object.DestroyImmediate(clone);report.Add("PASS "+effect.name+": emits "+count+" particles");}
   if(!hud.GetComponent<CombatHUD>().attackButton)throw new Exception("Missing HUD attack button");
   foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{Root+"/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath))
   {var p=AssetDatabase.LoadAssetAtPath<GameObject>(path);foreach(var t in p.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script "+path);}
   var env=worldMap.GetComponentsInChildren<SpriteRenderer>();
   if(env.Length<100||env.Any(x=>!x.sprite))throw new Exception("World map missing ground or props");
   report.Add("PASS WorldMap: ground + "+(env.Length-1)+" props, "+MapW+"x"+MapH+" units");
   report.Add("PASS all prefabs: no missing MonoBehaviour scripts");report.Add("PASS source atlases: actual alpha transparency");
   File.WriteAllLines(Out+"/Documentation/Validation.txt",report);
  }
 }
}
