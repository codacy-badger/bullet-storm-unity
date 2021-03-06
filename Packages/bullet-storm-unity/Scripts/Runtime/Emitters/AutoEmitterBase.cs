﻿using System;
using System.Collections;
using System.Collections.Generic;
using CANStudio.BulletStorm.BulletSystem;
using CANStudio.BulletStorm.Emission;
using CANStudio.BulletStorm.Storm;
using CANStudio.BulletStorm.Util;
using NaughtyAttributes;
using UnityEngine;

namespace CANStudio.BulletStorm.Emitters
{
    /// <summary>
    /// Base class of all auto emitters.
    /// <para/>
    /// Auto emitters can rotate automatically when emitting bullets. But every auto emitter can only start
    /// one emission at same time.
    /// <para/>
    /// To write an auto emitter, you should extend this class, and implement <see cref="StartEmitCoroutine"/>
    /// to do your emission behaviors.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class AutoEmitterBase : Emitter
    {
        [Tooltip("Begin to emit bullets on start.")]
        public bool emitOnStart;
        [Tooltip("Auto destroy the emitter when emission finished.")]
        public bool destroyOnFinish;
        
        [BoxGroup("Auto aim"), Label("Enable")]
        [Tooltip("Auto rotates the emitter to aim at a target.")]
        public bool enableAutoAim;
        [BoxGroup("Auto aim"), Label("Detail"), EnableIf("enableAutoAim")]
        public AutoAimModule autoAim = new AutoAimModule
        {
            followRateMultiplier = 1
        };

        [BoxGroup("Aim offset"), Label("Enable")]
        [Tooltip("Enables the emitter to emit towards customized direction, otherwise it will always emit forward.")]
        public bool enableAimOffset;
        [BoxGroup("Aim offset"), Label("Detail"), EnableIf("enableAimOffset")]
        public AimOffsetModule aimOffset = new AimOffsetModule
        {
            curveTimeScale = 1
        };
        
        // the emission coroutine
        private ControllableCoroutine coroutine;
        // deal with aim offset module
        private Transform subEmitter;
        
        /// <summary>
        /// Transform used to emit bullets.
        /// </summary>
        public Transform Emitter
        {
            get
            {
                if (!enableAimOffset) return transform;
                if (!subEmitter) InitAimOffset();
                return subEmitter;
            }
        }

        /// <summary>
        /// Is the emitter doing an emission?
        /// </summary>
        public bool IsEmitting => !(coroutine is null) && coroutine.Status == CoroutineStatus.Running;
        
        /// <summary>
        /// Call this function to start the emitter.
        /// You can start the emitter only when it is not <see cref="IsEmitting"/>.
        /// After started, the emitter will behave as configured in the inspector.
        /// </summary>
        [Button("Start Emission", EButtonEnableMode.Playmode)]
        public void StartEmission()
        {
            // init auto aim
            if (enableAutoAim && autoAim.aimOnEmissionStart && autoAim.target.Check())
            {
                transform.LookAt(autoAim.target);
            }

            InitAimOffset();
            
            // start coroutine
            if (coroutine is null || coroutine.Status == CoroutineStatus.Finished)
            {
                coroutine = new ControllableCoroutine(StartEmitCoroutine(), () =>
                {
                    if (destroyOnFinish) Destroy(this);
                });
                coroutine.Start();
            }
            else
            {
                BulletStormLogger.LogWarning($"Emitter {this} is emitting now.");
            }
        }

        // override emit functions to set transform to Emitter.
        public override void Emit(StormInfo storm) => base.Emit(storm, Emitter);
        public override void Emit(IEnumerable<BulletEmitParam> shape, IBulletSystem bullet) => base.Emit(shape, bullet, Emitter);
        public override void Emit(BulletEmitParam emitParam, IBulletSystem bullet) => base.Emit(emitParam, bullet, Emitter);

        /// <summary>
        /// Begins after <see cref="StartEmission"/> is called.
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerator StartEmitCoroutine();

        protected virtual void Start()
        {
            if (emitOnStart) StartEmission();
        }

        protected virtual void Update()
        {
            if (!IsEmitting) return;
            
            // auto aim
            if (enableAutoAim && autoAim.target)
            {
                var t = transform;
                var expected = autoAim.target.AsTransform.position - t.position;
                var forward = t.forward;
                forward = Vector3.RotateTowards(forward, expected,
                    autoAim.GetFollowRate(Vector3.Angle(forward, expected)), 0);
                transform.forward = forward;
            }

            // aim offset
            if (enableAimOffset)
            {
                if (!subEmitter) InitAimOffset();
                aimOffset.Tick(Time.deltaTime);
                subEmitter.localEulerAngles = aimOffset.TotalOffset;
            }
        }

        private void InitAimOffset()
        {
            aimOffset.Reset();
            if (!(subEmitter is null)) return;
            subEmitter = new GameObject().transform;
            subEmitter.name = "Sub emitter";
            subEmitter.SetParent(transform);
            subEmitter.localPosition = Vector3.zero;
        }

        [Serializable]
        public struct AutoAimModule
        {
            [Tooltip("The game object should the emitter aim at.")]
            public Target target;
            [Tooltip("When start emitting, aim at the target.")]
            public bool aimOnEmissionStart;
            
            [Header("Follow rate")]
            [Tooltip("Use a curve to describe follow rate.")]
            public bool useCurve;

            [Tooltip("Max rotation angle per second during emission to follow target."), HideIf("useCurve"),
             AllowNesting, Label("Follow rate"), MinValue(0)]
            public float followRateConst;
            
            [Tooltip("X-axis is the angle between target and current aim direction, Y-axis is rotation rate.")]
            [CurveRange(0, 0, 180, 2), ShowIf("useCurve"), AllowNesting, Label("Follow rate")]
            public AnimationCurve followRateCurve;

            [Tooltip("Multiplier for follow rate curve."), ShowIf("useCurve"), AllowNesting, Label("Multiplier"),
             MinValue(0)]
            public float followRateMultiplier;

            public float GetFollowRate(float angleDiff) =>
                useCurve
                    ? followRateCurve.Evaluate(angleDiff) * followRateMultiplier
                    : followRateConst;
        }

        [Serializable]
        public struct AimOffsetModule
        {
            [Tooltip("XYZ rotation offset when an emission starts in euler angles.")]
            public Vector3 offsetOnStart;
            
            [Tooltip("When using curve, the time in seconds that curve x-axis 0~1 represents."),
             MinValue(1), ShowIf("ShowCurveTimeScale"), AllowNesting]
            public float curveTimeScale;
            
            [Header("X-axis")]
            [Tooltip("Offset mode on x-axis, this will cause emitter aim up (negative) and down (positive).")]
            public OffsetMode xOffsetMode;
            [Tooltip("Offset on x-axis.")]
            public ParticleSystem.MinMaxCurve xOffset;

            [Header("Y-axis")]
            [Tooltip("Offset mode on y-axis, this will cause emitter aim left (negative) and right (positive).")]
            public OffsetMode yOffsetMode;
            [Tooltip("Offset on y-axis.")]
            public ParticleSystem.MinMaxCurve yOffset;

            [Header("Z-axis")]
            [Tooltip("Offset mode on z-axis, this will cause emitter aim clockwise (negative) and counterclockwise (positive).")]
            public OffsetMode zOffsetMode;
            [Tooltip("Offset on z-axis.")]
            public ParticleSystem.MinMaxCurve zOffset;

            private float time;
            public Vector3 TotalOffset { get; private set; }

            #region reflection use only

            // ReSharper disable once UnusedMember.Local
            private bool ShowCurveTimeScale => xOffset.mode == ParticleSystemCurveMode.Curve ||
                                               xOffset.mode == ParticleSystemCurveMode.TwoCurves ||
                                               yOffset.mode == ParticleSystemCurveMode.Curve ||
                                               yOffset.mode == ParticleSystemCurveMode.TwoCurves ||
                                               zOffset.mode == ParticleSystemCurveMode.Curve ||
                                               zOffset.mode == ParticleSystemCurveMode.TwoCurves;

            #endregion

            /// <summary>
            /// Call this on emission starts.
            /// </summary>
            internal void Reset()
            {
                time = 0;
                TotalOffset = offsetOnStart;
            }

            /// <summary>
            /// Tick the module to refresh <see cref="TotalOffset"/>.
            /// </summary>
            /// <param name="deltaTime">Delta time from last tick.</param>
            internal void Tick(float deltaTime)
            {
                time += deltaTime;
                TotalOffset = new Vector3(
                    GetOffset(TotalOffset.x, xOffsetMode, deltaTime, xOffset),
                    GetOffset(TotalOffset.y, yOffsetMode, deltaTime, yOffset),
                    GetOffset(TotalOffset.z, zOffsetMode, deltaTime, zOffset));
            }

            private float GetOffset(float oldValue, OffsetMode mode, float deltaTime, ParticleSystem.MinMaxCurve curve)
            {
                var value = curve.Evaluate(time / curveTimeScale);
                switch (mode)
                {
                    case OffsetMode.ExactAngle:
                        return value;
                    case OffsetMode.AngularVelocity:
                        return oldValue + value * deltaTime;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                }
            }
            
            public enum OffsetMode
            {
                [Tooltip("Use exactly this angle value.")]
                ExactAngle,
                [Tooltip("Rotate angle per second from emission start.")]
                AngularVelocity,
            }
        }
    }
}