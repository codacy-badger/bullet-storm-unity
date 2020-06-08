﻿using ParticleStorm.Script;
using ParticleStorm.Util;
using System.Collections.Generic;
using UnityEngine;

namespace ParticleStorm.Core
{
	/// <summary>
	/// Contains a <see cref="UnityEngine.ParticleSystem"/> and can emit particle from it.<para/>
	/// There two kinds of <see cref="ParticleSystemController"/>s,
	/// one called origin, the other called copy.<para/>
	/// Origin is created in <see cref="Particle"/>, one kind of particle
	/// only has one origin. Origin is always at (0, 0, 0) and never rotate.<para/>
	/// Copy can be created in <see cref="StormExecuter"/>, copies are copied from
	/// origin, and the particle system can move with <see cref="StormGenerator"/>.
	/// </summary>
	public class ParticleSystemController
	{
		public bool IsOrigin { get; private set; }
		public GameObject GameObject { get; private set; }
		public ParticleSystem ParticleSystem { get; private set; }
		public UpdateEvent UpdateEvent { get; set; }
		public CollisionEvent CollisionEvent { get; set; }

		private ParticleStatusList particles;
		private List<ParticleCollisionEvent> collisionEvents;

		/// <summary>
		/// Create an origin <see cref="ParticleSystemController"/>.
		/// The <see cref="UnityEngine.ParticleSystem"/> will be located at origin point.
		/// </summary>
		public ParticleSystemController()
		{
			IsOrigin = true;
			GameObject = new GameObject();
			Initialize();
		}

		/// <summary>
		/// Create an origin <see cref="ParticleSystemController"/>.
		/// The <see cref="UnityEngine.ParticleSystem"/> will be located at origin point.
		/// </summary>
		/// <param name="name">Name of the particle system game objecct.</param>
		public ParticleSystemController(string name)
		{
			IsOrigin = true;
			GameObject = new GameObject(name);
			Initialize();
		}

		/// <summary>
		/// Create an origin <see cref="ParticleSystemController"/>.
		/// The <see cref="UnityEngine.ParticleSystem"/> will be located at origin point.
		/// </summary>
		/// <param name="prefeb">The particle prefeb</param>
		public ParticleSystemController(ParticlePrefeb prefeb)
		{
			IsOrigin = true;
			GameObject = new GameObject(prefeb.name);
			Initialize();
			prefeb.ApplicateOn(this);
		}

		/// <summary>
		/// Create an origin <see cref="ParticleSystemController"/>.
		/// The <see cref="UnityEngine.ParticleSystem"/> will be located at origin point.
		/// </summary>
		/// <param name="name">Name of the particle system game objecct.</param>
		/// <param name="prefeb">The particle prefeb</param>
		public ParticleSystemController(string name, ParticlePrefeb prefeb)
		{
			IsOrigin = true;
			GameObject = new GameObject(name);
			Initialize();
			prefeb.ApplicateOn(this);
		}

		/// <summary>
		/// Copy an origin.
		/// </summary>
		/// <param name="origin">The origin particle system controller</param>
		public ParticleSystemController(ParticleSystemController origin, Transform parent)
		{
			IsOrigin = false;
			GameObject = GameObject.Instantiate<GameObject>(origin.GameObject, parent, false);
			Initialize();
			UpdateEvent = origin.UpdateEvent;
			CollisionEvent = origin.CollisionEvent;
		}

		~ParticleSystemController()
		{
			GameObject.Destroy(GameObject);
		}

		public void Emit(EmitParams emitParams)
		{
			ParticleSystem.Emit(emitParams.Full, 1);
		}

		/// <summary>
		/// Should be called on update.
		/// </summary>
		public void Update()
		{
			if (UpdateEvent != null && UpdateEvent.OnParticleUpdate != null)
			{
				particles.Update(UpdateEvent.OnParticleUpdate, UpdateEvent.ParallelOnUpdate);
			}
		}

		/// <summary>
		/// Should be called on fixed update.
		/// </summary>
		public void FixedUpdate()
		{
			if (UpdateEvent != null && UpdateEvent.OnParticleFixedUpdate != null)
			{
				particles.Update(UpdateEvent.OnParticleFixedUpdate, UpdateEvent.ParallelOnFixedUpdate);
			}
		}

		/// <summary>
		/// Should be called on late update.
		/// </summary>
		public void LateUpdate()
		{
			if (UpdateEvent != null && UpdateEvent.OnParticleLateUpdate != null)
			{
				particles.Update(UpdateEvent.OnParticleLateUpdate, UpdateEvent.ParallelOnLateUpdate);
			}
		}

		/// <summary>
		/// Should be called on particle collidion.
		/// </summary>
		/// <param name="other">The collided game object</param>
		public void OnParticleCollision(GameObject other)
		{
			if (CollisionEvent != null && CollisionEvent.OnGameObjectCollision != null)
			{
				ParticleSystem.GetCollisionEvents(other, collisionEvents);
				CollisionEvent.OnGameObjectCollision(other, collisionEvents);
			}
		}

		private void Initialize()
		{
			if (IsOrigin)
			{
				// Create particle system
				ParticleSystem = GameObject.AddComponent<ParticleSystem>();

				// Add updater
				var updater = GameObject.AddComponent<ParticleSystemControllerUpdater>();
				updater.UpdateFor(this);

				// Enable GPU
				var psr = GameObject.GetComponent<ParticleSystemRenderer>();
				psr.enableGPUInstancing = true;

				// Lock transform
				GameObject.transform.position = Vector3.zero;
				GameObject.transform.rotation = Quaternion.identity;
				GameObject.isStatic = true;
			}
			else
			{
				ParticleSystem = GameObject.GetComponent<ParticleSystem>();
				GameObject.GetComponent<ParticleSystemControllerUpdater>().UpdateFor(this);
				GameObject.isStatic = false;
			}

			// Disable auto generate
			ParticleSystem.Stop();

			// Init lists
			particles = new ParticleStatusList(ParticleSystem);
			collisionEvents = new List<ParticleCollisionEvent>();
		}
	}
}
