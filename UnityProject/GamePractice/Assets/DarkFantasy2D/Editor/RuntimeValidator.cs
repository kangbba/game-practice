using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace DarkFantasy2D.Editor
{
 [InitializeOnLoad]
 public static class RuntimeValidator
 {
  static int step, strikes; static double next, deadline; static AssetShowcase demo;
  static RuntimeValidator() { if(SessionState.GetBool("DF2D.RuntimeCheck",false)) EditorApplication.update+=Tick; }
  public static void Begin()
  {
   EditorSceneManager.OpenScene("Assets/DarkFantasy2D/Scenes/AssetShowcase.unity");
   SessionState.SetBool("DF2D.RuntimeCheck",true);EditorApplication.EnterPlaymode();
  }
  static void Require(bool condition,string message) {if(!condition)throw new Exception(message);}
  static void Tick()
  {
   if(!EditorApplication.isPlaying)return;
   double now=Time.timeAsDouble;if(deadline==0)deadline=EditorApplication.timeSinceStartup+120;
   try
   {
    if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Runtime validation timeout");if(now<next)return;
    if(step==0)
    {
     demo=UnityEngine.Object.FindFirstObjectByType<AssetShowcase>();if(!demo || Time.timeSinceLevelLoad < .15f)return;
     Require(demo.characters.All(c=>c.Health==c.maxHealth),"Initial health");
     foreach(var c in demo.characters)c.onStrike.AddListener(()=>strikes++);
     demo.animationButtons[1].onClick.Invoke();next=now+.45;step++;return;
    }
    if(step==1){Require(demo.characters.All(c=>c.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Walk")),"Walk transition");demo.animationButtons[2].onClick.Invoke();next=now+.48;step++;return;}
    if(step==2){Require(strikes==3,"Expected 3 attack contact events; received "+strikes);Require(UnityEngine.Object.FindObjectsByType<EffectLifetime>(FindObjectsSortMode.None).Length>=3,"Attack VFX spawn");demo.animationButtons[3].onClick.Invoke();next=now+.45;step++;return;}
    if(step==3){Require(demo.characters.All(c=>c.Health==c.maxHealth-15),"Hit health update");demo.animationButtons[4].onClick.Invoke();next=now+.9;step++;return;}
    if(step==4){Require(demo.characters.All(c=>c.IsDead),"Death health");Require(demo.characters.All(c=>c.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Death")),"Death state");demo.animationButtons[5].onClick.Invoke();next=now+.3;step++;return;}
    if(step==5){Require(demo.characters.All(c=>!c.IsDead&&c.Health==c.maxHealth),"Revive");foreach(var b in demo.effectButtons)b.onClick.Invoke();next=now+3.2;step++;return;}
    if(step==6){demo.growthButton.onClick.Invoke();next=now+.2;step++;return;}
    if(step==7){var gp=demo.growthPanel.GetComponent<GrowthPanel>();Require(demo.growthPanel.activeSelf,"Growth panel opens");int coins=gp.coins;int level=gp.attackLevel;gp.attackTrain.onClick.Invoke();Require(gp.coins==coins-5&&gp.attackLevel==level+1,"Training state update");var es=UnityEngine.EventSystems.EventSystem.current;var data=new UnityEngine.EventSystems.PointerEventData(es);data.position=RectTransformUtility.WorldToScreenPoint(gp.GetComponent<Canvas>().worldCamera,gp.attackTrain.transform.position);var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();es.RaycastAll(data,hits);Require(hits.Count>0&&hits[0].gameObject==gp.attackTrain.gameObject,"Growth button raycast priority");gp.close.onClick.Invoke();Require(!demo.growthPanel.activeSelf,"Growth closes");Require(UnityEngine.Object.FindObjectsByType<EffectLifetime>(FindObjectsSortMode.None).Length==0,"Particle prefab cleanup");File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("DARKFANTASY_EXPORT_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"DarkFantasy2D_AssetSet"),"Documentation/RuntimeValidation.txt"),"PASS Unity "+Application.unityVersion+" / "+(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline ? "URP" : "Built-in")+"\nPlay Mode: initial health, walk transitions, 3 attack contact events, attack VFX spawning, damage, death state, revive, all 8 effect buttons, particle cleanup, growth open/train/close, currency, modal button raycast priority.\n");SessionState.SetBool("DF2D.RuntimeCheck",false);Debug.Log("DARK_FANTASY_RUNTIME_SUCCESS");AssetDatabase.ExportPackage("Assets/DarkFantasy2D",Path.Combine(Environment.GetEnvironmentVariable("DARKFANTASY_EXPORT_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"DarkFantasy2D_AssetSet"),"DarkFantasy2D.unitypackage"),ExportPackageOptions.Recurse);EditorApplication.Exit(0);}
   }
   catch(Exception e){SessionState.SetBool("DF2D.RuntimeCheck",false);Debug.LogException(e);EditorApplication.Exit(1);}
  }
 }
}
