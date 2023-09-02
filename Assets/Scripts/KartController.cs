using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class KartController : MonoBehaviour
{
    [Header("Kart Settings")]
    public Rigidbody rgBody;
    public Transform kartModel;
    public Transform kartParent;
    public Transform kartPivot;

    public LayerMask ground;

    [Header("Movement Settings")]
    public float maxSpeed = 5f;
    public float minSpeed = -5f;
    public float acceleration = 1.0f;

    [Header("Steering Settings")]
    public float steeringAmount = 80;
    public float steerRate = 4f;
    public float airborneSteerRate = 10f;
    public float angularAcceleration = 50f;
    public float tiltAcceleration = 5f;

    [Header("Drift Settings")]
    public float maxDriftAngle = 30f;
    public float tiltAngleMultiplier = 3f;
    public float driftRate = 8f;
    public float driftControlMultiplier = 1.5f;

    [Header("Jump Settings")]
    public float jumpForce = 10f;

    [Header("Boost Settings")]
    public float boostSpeed_1 = 20f;
    public float boostSpeed_2 = 35f;
    public float boostSpeed_3 = 45f;

    public float boostAcceleration = 10f;
    public float boostTime = 1f;

    public int firstBoostThreshold = 25;
    public int secondBoostThreshold = 45;
    public int thirdBoostThreshold = 60;

    [Header("Model Parts")]
    public Transform frontWheels;
    public Transform backWheels;
    public Transform steeringWheel;

    [Header("VFX")]
    public Transform tireSmoke;
    public Transform rightWheelDriftVFX;
    public Transform leftWheelDriftVFX;
    public Transform boostVFX;

    public ParticleSystem speedLines;

    public Color[] driftColors;

    List<ParticleSystem> wheelParticleSystems = new List<ParticleSystem>();
    List<ParticleSystem> boostParticleSystems = new List<ParticleSystem>();
    List<ParticleSystem> tireSmokeParticleSystems = new List<ParticleSystem>();

    float speed;

    float rotate;
    float currentRotate;

    float boostTimer = 0;
    float driftPower = 0;
    bool isInBoost = false;
    bool canBoost;

    float horizontalInput;
    float verticalInput;

    bool isDrifting;
    int driftDirection;

    bool jumpImpulse;
    bool isAirborne;

    Vector3 velocity;
    float velocityThreshold = 5f;

    readonly float rayGroundCheckDistance = 1.1f;
    readonly float rayNormalCheckDistance = 1.5f;

    public Volume postProcessingVolume;
    public Cinemachine.CinemachineVirtualCamera virtualCamera;
    public float cameraEffectsRate = 2;

    UnityEngine.Rendering.Universal.ChromaticAberration chromaticAberration;
    float chromaticAberrationIntensity = 0;

    private void Start()
    {
        if (!postProcessingVolume.profile.TryGet(out chromaticAberration)) 
        { 
            throw new NullReferenceException(nameof(chromaticAberration)); 
        }

        foreach (ParticleSystem ps in rightWheelDriftVFX.GetComponentsInChildren<ParticleSystem>())
        {
            wheelParticleSystems.Add(ps);
        }

        foreach (ParticleSystem ps in leftWheelDriftVFX.GetComponentsInChildren<ParticleSystem>())
        {
            wheelParticleSystems.Add(ps);
        }

        foreach (ParticleSystem ps in boostVFX.GetComponentsInChildren<ParticleSystem>())
        {
            boostParticleSystems.Add(ps);
        }

        foreach (ParticleSystem ps in tireSmoke.GetComponentsInChildren<ParticleSystem>())
        {
            tireSmokeParticleSystems.Add(ps);
        }

        gamepadConnected = Gamepad.all.Count > 0;

        InputSystem.onDeviceChange += OnDeviceChanged;
    }

    bool gamepadConnected;
    bool isAccelerating, isBraking;

    public void OnDeviceChanged(InputDevice device, InputDeviceChange change)
    {
        if (Gamepad.all.Count <= 0) 
            gamepadConnected = false;
        else 
            gamepadConnected = true;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        horizontalInput = context.ReadValue<Vector2>().x;

        if(!gamepadConnected)
            verticalInput = context.ReadValue<Vector2>().y;
    }
  
    public void OnAccelerate(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isAccelerating = true;
        }
        else if (context.canceled)
        {
            isAccelerating = false;
        }
    }

    public void OnBrake(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isBraking = true;
        }
        else if (context.canceled)
        {
            isBraking= false;
        }
    }

    public void OnDrift(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isDrifting = !isAirborne;
            driftDirection = (int)Mathf.Sign(horizontalInput);
        }
        else if (context.canceled)
        {
            isDrifting = false;
            canBoost = true;
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpImpulse = true;
        }
        else if (context.canceled)
        {
            jumpImpulse = false;
        }
    }

    void Update()
    {
        if (isDrifting && velocity.magnitude > velocityThreshold && (horizontalInput != 0 || isInDrift))
        {
            float control = (driftDirection == 1) ? Remap(horizontalInput, -1, 1, 0, driftControlMultiplier) : Remap(horizontalInput, -1, 1, -driftControlMultiplier, 0);
            float driftAngle = maxDriftAngle * driftDirection;
            float tiltAngle = tiltAngleMultiplier * control;

            kartParent.localRotation = Quaternion.Euler(0, Mathf.LerpAngle(kartParent.localEulerAngles.y, driftAngle, .2f), 
                Mathf.LerpAngle(kartParent.localEulerAngles.z, tiltAngle, .2f));

            foreach(ParticleSystem ps in wheelParticleSystems)
            {
                ParticleSystem.MainModule settings = ps.main;
                settings.startColor = Color.clear;

                if (driftPower > firstBoostThreshold && driftPower < secondBoostThreshold)
                {
                    settings.startColor = driftColors[0];
                }
                else if (driftPower >= secondBoostThreshold && driftPower < thirdBoostThreshold)
                {
                    settings.startColor = driftColors[1];
                }
                else if (driftPower >= thirdBoostThreshold)
                {
                    settings.startColor = driftColors[2];
                }

                if(!ps.isPlaying)
                    ps.Play();
            }

            foreach (ParticleSystem ps in tireSmokeParticleSystems)
            {
                if (!ps.isPlaying)
                    ps.Play();
            }
        }
        else
        {
            foreach (ParticleSystem ps in wheelParticleSystems)
            {
                ParticleSystem.MainModule settings = ps.main;
                settings.startColor = Color.clear;
                ps.Stop();
            }

            foreach (ParticleSystem ps in tireSmokeParticleSystems)
            {
                ps.Stop();
            }

            if (horizontalInput != 0)
            {
                kartParent.localRotation = Quaternion.Euler(0, Mathf.LerpAngle(kartParent.localEulerAngles.y, 10f*Mathf.Sign(horizontalInput), 0.05f), 0);
            }
            else
            {
                kartParent.localRotation = Quaternion.Euler(0, Mathf.LerpAngle(kartParent.localEulerAngles.y, 0, 0.05f), 0);
            }
        }

        if (isInBoost)
        {
            virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(virtualCamera.m_Lens.FieldOfView, 90, cameraEffectsRate);
            chromaticAberrationIntensity = Mathf.Lerp(chromaticAberrationIntensity, 1, cameraEffectsRate);
            chromaticAberration.intensity.Override(chromaticAberrationIntensity);

            speedLines.Play();

            foreach (ParticleSystem ps in boostParticleSystems)
            {
                if (!ps.isPlaying) ps.Play();
            }
        }
        else
        {
            virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(virtualCamera.m_Lens.FieldOfView, 75, cameraEffectsRate);
            chromaticAberrationIntensity = Mathf.Lerp(chromaticAberrationIntensity, 0, cameraEffectsRate);
            chromaticAberration.intensity.Override(chromaticAberrationIntensity);

            speedLines.Stop();

            foreach (ParticleSystem ps in boostParticleSystems)
            {
                ps.Stop();
            }
        }

        frontWheels.localEulerAngles = Vector3.Lerp(frontWheels.localEulerAngles, new Vector3(0, (horizontalInput * 20) + 90, frontWheels.localEulerAngles.z), 0.05f);
        frontWheels.Rotate(Vector3.forward, rgBody.velocity.magnitude / 2);
        backWheels.Rotate(Vector3.forward, rgBody.velocity.magnitude / 2);
    }

    bool isInDrift = false;
    private void FixedUpdate()
    {
        if (!gamepadConnected)
        {
            isAccelerating = verticalInput > 0;
            isBraking = verticalInput < 0;
        }

        if (isAccelerating && !isBraking)
        {
            speed = Mathf.Lerp(speed, maxSpeed, 1 / acceleration);
        }
        else if (!isAccelerating && isBraking)
        {
            speed = Mathf.Lerp(speed, minSpeed, 1 / acceleration);
        }
        else
        {
            speed = Mathf.Lerp(speed, 0, 1 / acceleration);
        }

        if(horizontalInput != 0)
        {
            int dir = horizontalInput > 0 ? 1 : -1;
            float amount = Mathf.Abs(horizontalInput);
            Steer(dir, amount);
        }

        if (!isInBoost && canBoost)
        {
            if (driftPower > firstBoostThreshold && driftPower <= secondBoostThreshold)
            {
                StartCoroutine(Boost(boostSpeed_1));
            }
            else if (driftPower > secondBoostThreshold && driftPower <= thirdBoostThreshold)
            {
                StartCoroutine(Boost(boostSpeed_2));
            }
            else if (driftPower > thirdBoostThreshold)
            {
                StartCoroutine(Boost(boostSpeed_3));
            }
        }

        if (isDrifting &&!isAirborne && velocity.magnitude > velocityThreshold && ( horizontalInput != 0 || isInDrift))
        {
            isInDrift = true;
            float control = (driftDirection == 1) ? Remap(horizontalInput, -1, 1, 0, driftControlMultiplier) : Remap(horizontalInput, -1, 1, driftControlMultiplier, 0);
            float powerControl = (driftDirection == 1) ? Remap(horizontalInput, -1, 1, .6f, 1) : Remap(horizontalInput, -1, 1, 1, .6f);

            driftPower += powerControl;

            Steer(driftDirection, control);
            currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * driftRate);
        }
        else if(!isDrifting && !isAirborne)
        {
            isInDrift = false;

            driftPower = 0;
            currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * steerRate);
        }
        else
        {
            isInDrift = false;

            driftPower = 0;
            currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * airborneSteerRate);
        }

        rotate = 0f;
        canBoost = false;

        Physics.Raycast(rgBody.position, Vector3.down, out RaycastHit groundCheckHit, rayGroundCheckDistance, ground);
        Physics.Raycast(rgBody.position, Vector3.down, out RaycastHit groundNormalCheckHit, rayNormalCheckDistance, ground);

        velocity = kartPivot.forward * speed;

        if (groundCheckHit.collider)
        {
            isAirborne = false;
            rgBody.velocity = new Vector3(velocity.x, rgBody.velocity.y, velocity.z);
        }
        else
        {
            rgBody.velocity = new Vector3(velocity.x, rgBody.velocity.y, velocity.z);
        }

        if (jumpImpulse && !isAirborne)
        {
            jumpImpulse = false;
            isAirborne = true;
            rgBody.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
        }

        //Rotation
        Vector3 newRotation = new(0, kartPivot.eulerAngles.y + currentRotate, 0);
        kartPivot.eulerAngles = Vector3.Lerp(kartPivot.eulerAngles, newRotation, 1 / angularAcceleration);

        //Normal
        if (groundNormalCheckHit.collider)
        {
            Vector3 newUp = kartModel.up;
            newUp = Vector3.Lerp(newUp, groundNormalCheckHit.normal, 1 / tiltAcceleration);

            Quaternion rotation = InverseLookRotation(kartParent.forward, newUp);

            if (groundNormalCheckHit.normal == Vector3.up)
            {
                rotation.eulerAngles = new Vector3(rotation.eulerAngles.x, 0, rotation.eulerAngles.z);
                kartModel.localRotation = rotation;
            }
            else
            {
                kartModel.rotation = rotation;
            }
        }

        //Position
        kartPivot.position = new Vector3(rgBody.position.x, rgBody.position.y - 1, rgBody.position.z);
    }

    public IEnumerator Boost(float boostSpeed)
    {
        boostTimer = 0;

        while (boostTimer <= boostTime)
        {
            isInBoost = true;
            speed = Mathf.Lerp(speed, boostSpeed, Time.deltaTime * boostAcceleration);
            boostTimer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        boostTimer = 0;
        isInBoost = false;
    }

    Quaternion InverseLookRotation(Vector3 approximateForward, Vector3 exactUp)
    {
        Quaternion rotateZToUp = Quaternion.LookRotation(exactUp, -approximateForward);
        Quaternion rotateYToZ = Quaternion.Euler(90f, 0f, 0f);

        return rotateZToUp * rotateYToZ;
    }

    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public void Steer(int direction, float amount)
    {
        rotate = (steeringAmount * direction) * amount;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(rgBody.position, rgBody.position + Vector3.down * rayGroundCheckDistance);
        //Gizmos.color = Color.red;
        //Gizmos.DrawLine(rgBody.position, rgBody.position + Vector3.down * rayNormalCheckDistance);
    }
}
