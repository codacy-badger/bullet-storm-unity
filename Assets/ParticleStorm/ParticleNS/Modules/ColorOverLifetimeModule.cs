﻿#pragma warning disable 0649

using System;
using UnityEngine;

namespace ParticleStorm.ParticleNS.Modules
{
	[Serializable]
	internal struct ColorOverLifetimeModule : IParticleModule
	{
		[Tooltip("Enable color change according to lifetime.")]
		public bool enabled;
		public ParticleSystem.MinMaxGradient color;

		public void ApplicateOn(ParticleSystemController psc)
		{
			var module = psc.ParticleSystem.colorOverLifetime;
			module.enabled = enabled;
			module.color = color;
		}
	}
}