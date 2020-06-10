﻿using ParticleStorm.ParticleNS;
using ParticleStorm.ParticleNS.Script;
using ParticleStorm.StormNS;
using ParticleStorm.StormNS.Behavior;
using UnityEngine;

public class CollisionDemo : MonoBehaviour
{
	public ParticlePrefeb prefeb;
	public Collider box;
	public float maxAngle;
	private Vector3 boxPosition;

	private void TurnToBox(ParticleStatus particle)
	{
		particle.Velocity = Vector3.RotateTowards(
			particle.Velocity,
			boxPosition - particle.Position,
			maxAngle * particle.DeltaTime * Mathf.Deg2Rad,
			0
		);
	}

	private void Start()
	{
		new UpdateEvent("TurnToBox", TurnToBox);
		new CollisionEvent("Collision",
			(go, collisions) =>
			{
				for (int i = 0; i < collisions.Count; i++)
				{
					Debug.Log(go.name + " collided");
				}
			}
		);
		var generator = GetComponent<StormGenerator>();
		var storm = new Storm();
		var particle = new Particle(prefeb);
		storm.AddBehavior(new EmissionBehavior(EmitList.Cone(100, 2, 90, 0.5f), particle, 0, 1));
		generator.Generate(storm);
	}

	private void Update()
	{
		boxPosition = box.transform.position;
	}
}