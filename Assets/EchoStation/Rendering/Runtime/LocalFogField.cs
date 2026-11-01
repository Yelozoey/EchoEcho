using System;
using UnityEngine;
namespace EchoStation.Rendering
{
 [DisallowMultipleComponent]
 public sealed class LocalFogField:MonoBehaviour
 {
  private float recovery,labScale=1,ventScale=1;
  public Bounds LabBounds=>new Bounds(new Vector3(0,-2,30),new Vector3(12,4,16));
  public Bounds VentBounds=>new Bounds(new Vector3(0,-2,48),new Vector3(5,4,20));
  public float LabDensity=>Mathf.Lerp(.025f,.008f,Mathf.SmoothStep(0,1,recovery))*labScale;
  public float VentDensity=>Mathf.Lerp(.17f,.015f,Mathf.SmoothStep(0,1,recovery))*ventScale;
  public float LightStrength {get;set;}=1;
  public float Recovery=>recovery;
  public int LastWidth {get;internal set;}public int LastHeight {get;internal set;}public int LastRecordedFrame {get;internal set;}=-1;
  public void SetRecovery(float fraction){if(!float.IsFinite(fraction)||fraction<0||fraction>1)throw new ArgumentOutOfRangeException(nameof(fraction));recovery=fraction;}
  public void SetDensityScale(float lab,float vent){if(!float.IsFinite(lab)||!float.IsFinite(vent)||lab<0||vent<0||lab>4||vent>4)throw new ArgumentOutOfRangeException();labScale=lab;ventScale=vent;}
  public bool ShouldRender(Vector3 camera)=>isActiveAndEnabled&&camera.z>=4&&camera.z<90&&Mathf.Abs(camera.x)<30&&camera.y<5;
 }
}
