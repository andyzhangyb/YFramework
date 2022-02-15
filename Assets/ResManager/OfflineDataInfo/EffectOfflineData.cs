using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectOfflineData : OfflineData
{
    public ParticleSystem[] Particles;
    public TrailRenderer[] TrailRenderers;

    public override void ResetData()
    {
        base.ResetData();
        for (int i = 0; i < Particles.Length; i++)
        {
            Particles[i].Clear(true);
            Particles[i].Play();
        }
        for (int i = 0; i < TrailRenderers.Length; i++)
        {
            TrailRenderers[i].Clear();
        }
    }

    public override void BindData()
    {
        base.BindData();
        Particles = gameObject.GetComponentsInChildren<ParticleSystem>(true);
        TrailRenderers = gameObject.GetComponentsInChildren<TrailRenderer>(true);
    }

}
