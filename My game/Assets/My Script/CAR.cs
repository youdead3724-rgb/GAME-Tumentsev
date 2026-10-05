// Импортируем пространства имен, необходимые для работы скрипта.
using UnityEngine;
using System.Collections.Generic;

// Этот атрибут гарантирует, что на объекте всегда будет компонент Rigidbody.
[RequireComponent(typeof(Rigidbody))]
public class UltimateCarConstructor : MonoBehaviour
{
    // --- Вспомогательные классы и перечисления перенесены ВНУТРЬ, чтобы избежать ошибок Unity ---

    /// <summary> Перечисление для выбора типа привода. </summary>
    public enum DriveType { Задний, Передний, Полный }

    /// <summary> Структура для детальной настройки трения колес. </summary>
    [System.Serializable]
    public class WheelFrictionSettings
    {
        [Tooltip("Насколько сильно колесо 'цепляется' за дорогу при разгоне и торможении. Высокое значение = хорошее сцепление.")]
        public float forwardStiffness = 1.5f;
        [Tooltip("Насколько сильно колесо 'сопротивляется' боковому скольжению (дрифту). Высокое значение = сложно уйти в занос.")]
        public float sidewaysStiffness = 2.0f;
    }

    /// <summary> Контейнер для всех настроек турбо-ускорения. </summary>
    [System.Serializable]
    public class TurboSettings
    {
        [Tooltip("Клавиша для активации турбо-ускорения.")]
        public KeyCode activationKey = KeyCode.LeftShift;
        [Tooltip("Дополнительная сила, которую дает турбо. Добавляется к основной мощности двигателя.")]
        public float boostForce = 2000f;
        [Tooltip("Как долго турбо может работать без перерыва (в секундах).")]
        public float boostDuration = 3.0f;
        [Tooltip("Время в секундах, которое турбо должно остыть после полного истощения.")]
        public float boostCooldown = 5.0f;
        [Tooltip("С какой скоростью турбо восстанавливается, когда не используется.")]
        public float boostRechargeRate = 0.5f;
    }


    #region 🚗 Основные Компоненты

    [Header("--- 🚗 Назначение Колёс и их Моделей ---")]
    [Tooltip("Физический коллайдер левого переднего колеса.")] public WheelCollider frontLeftCollider;
    [Tooltip("Визуальная 3D-модель левого переднего колеса.")] public Transform frontLeftMesh;
    [Tooltip("Физический коллайдер правого переднего колеса.")] public WheelCollider frontRightCollider;
    [Tooltip("Визуальная 3D-модель правого переднего колеса.")] public Transform frontRightMesh;
    [Tooltip("Физический коллайдер левого заднего колеса.")] public WheelCollider rearLeftCollider;
    [Tooltip("Визуальная 3D-модель левого заднего колеса.")] public Transform rearLeftMesh;
    [Tooltip("Физический коллайдер правого заднего колеса.")] public WheelCollider rearRightCollider;
    [Tooltip("Визуальная 3D-модель правого заднего колеса.")] public Transform rearRightMesh;

    #endregion

    #region ⚙️ Тип Привода и Двигатель

    [Header("--- ⚙️ Тип Привода и Двигатель ---")]
    [Tooltip("Выберите тип привода:\n- Задний (RWD): Классика для спорткаров, хорошо дрифтует.\n- Передний (FWD): Стабильный, как у большинства городских авто.\n- Полный (AWD): Максимальное сцепление, идеален для ралли.")]
    public DriveType driveType = DriveType.Задний;

    [Tooltip("Распределение крутящего момента для полного привода (AWD).\n0 = 100% на задние колеса.\n1 = 100% на передние колеса.\n0.5 = 50/50.")]
    [Range(0f, 1f)] public float awdTorqueSplit = 0.5f;

    [Tooltip("Основная мощность двигателя. Чем выше значение, тем быстрее машина будет разгоняться.")]
    public float motorForce = 5000f;

    [Tooltip("Максимальная скорость, до которой машина может разогнаться (в км/ч).")]
    public float maxSpeed = 250f;

    [Tooltip("Кривая ускорения. Позволяет настроить, как мощность меняется в зависимости от скорости.")]
    public AnimationCurve accelerationCurve = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(0.8f, 0.8f), new Keyframe(1, 0.3f));

    #endregion

    #region 🛑 Тормозная Система

    [Header("--- 🛑 Тормозная Система ---")]
    [Tooltip("Общая сила торможения при нажатии на клавишу S.")]
    public float brakeForce = 12000f;

    [Tooltip("Сила 'торможения двигателем'. Сопротивление, которое возникает, когда вы отпускаете газ. Увеличьте, если машина катится слишком долго по инерции.")]
    public float engineBrakeForce = 1000f;

    [Tooltip("Баланс тормозов (0 = только задние, 1 = только передние).\nСовет: для дрифта попробуйте 0.3-0.4, чтобы задние колеса блокировались раньше.")]
    [Range(0f, 1f)] public float brakeBias = 0.7f;

    [Tooltip("Сила ручного тормоза для резкой блокировки задних колес.")]
    public float handbrakeForce = 25000f;

    #endregion

    #region 🕹️ Управляемость и Руль

    [Header("--- 🕹️ Управляемость и Руль ---")]
    [Tooltip("Зависимость угла поворота от скорости. Ключевая настройка! Позволяет сделать руль острым на парковке и плавным на шоссе.")]
    public AnimationCurve steeringCurve = new AnimationCurve(new Keyframe(0, 40f), new Keyframe(100, 15f), new Keyframe(250, 8f));

    [Tooltip("Насколько быстро колеса поворачиваются в нужную сторону. Высокое значение = резкий, спортивный руль. Низкое = плавный, 'тяжелый' руль, как у грузовика.")]
    public float steerResponsiveness = 8f;

    [Tooltip("Эффект Аккермана (0 = выключен, 1 = реалистично).\nВ реальности внутреннее колесо в повороте поворачивается на больший угол. Небольшое значение (0.1-0.2) добавит реализма.")]
    [Range(0f, 1f)] public float ackermannFactor = 0.1f;

    #endregion

    #region 🌍 Физика и Стабильность

    [Header("--- 🌍 Физика, Стабильность и Сцепление ---")]
    [Tooltip("Смещение центра масс автомобиля. Очень важно! Сместите его немного вниз по оси Y (например, -0.5), чтобы машина стала НАМНОГО устойчивее.")]
    public Vector3 centerOfMassOffset = new Vector3(0, -0.5f, 0);

    [Space(10)]

    [Tooltip("Сила, прижимающая машину к земле на скорости (антикрыло). Увеличивает сцепление в поворотах. Если машина нестабильна на скорости - увеличьте это значение.")]
    public float downforceFactor = 50f;

    [Tooltip("Сопротивление качению. Имитирует трение колес о дорогу. Отвечает за базовое замедление.")]
    public float rollingResistance = 150f;

    [Tooltip("Сопротивление воздуха. Начинает сильно влиять на высоких скоростях. Главный параметр, который мешает машине разгоняться до бесконечности.")]
    public float airDragCoefficient = 2.0f;

    [Space(10)]

    [Header("--- ↔️ Настройки Сцепления Колес (для ПРО) ---")]
    [Tooltip("Настройки трения для передних колес.")] public WheelFrictionSettings frontWheelsFriction;
    [Tooltip("Настройки трения для задних колес.")] public WheelFrictionSettings rearWheelsFriction;

    #endregion

    #region 🔊 Звуковая Система

    [Header("--- 🔊 Звуковая Система ---")]
    [Tooltip("Источник звука для двигателя. Перетащите сюда объект с AudioSource.")]
    public AudioSource engineSoundSource;
    [Tooltip("Минимальная высота тона (pitch) звука двигателя на холостом ходу.")]
    public float minEnginePitch = 0.8f;
    [Tooltip("Максимальная высота тона (pitch) звука двигателя на максимальных оборотах.")]
    public float maxEnginePitch = 2.5f;

    [Space(5)]

    [Tooltip("Источник звука для визга покрышек. Перетащите сюда объект с AudioSource.")]
    public AudioSource skidSoundSource;
    [Tooltip("Порог бокового скольжения колеса, при котором начинает появляться визг покрышек. Попробуйте значения 0.3-0.5.")]
    public float slipThreshold = 0.4f;

    #endregion

    #region 💥 Эффекты и Турбо

    [Header("--- 💥 Эффекты и Турбо ---")]
    [Tooltip("Системы частиц для дыма из-под задних колес. Можно добавить сколько угодно.")]
    public List<ParticleSystem> tireSmokeEffects = new List<ParticleSystem>();
    [Tooltip("Настройки для системы турбо-ускорения.")]
    public TurboSettings turbo;

    #endregion

    // --- Приватные переменные для внутренних расчетов ---
    private Rigidbody rb;
    private float throttleInput, steerInput;
    private bool handbrakeInput, turboInput;
    private float currentSteerAngle;
    private float currentBoostAmount;
    private float boostCooldownTimer;
    private WheelCollider[] allWheels;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass += centerOfMassOffset;
        allWheels = new WheelCollider[] { frontLeftCollider, frontRightCollider, rearLeftCollider, rearRightCollider };

        InitializeSounds();
        currentBoostAmount = turbo.boostDuration;
    }

    void Update()
    {
        GatherInput();
        HandleSounds();
        HandleEffects();
    }

    void FixedUpdate()
    {
        float currentSpeed = rb.linearVelocity.magnitude * 3.6f;

        HandleMotorAndBrakes(currentSpeed);
        HandleSteering(currentSpeed);
        ApplyPassiveForces();
        AnimateWheels();
    }

    void GatherInput()
    {
        throttleInput = Input.GetAxis("Vertical");
        steerInput = Input.GetAxis("Horizontal");
        handbrakeInput = Input.GetKey(KeyCode.Space);
        turboInput = Input.GetKey(turbo.activationKey);
    }

    /// <summary> Основной метод, управляющий движением, тормозами и турбо. </summary>
    void HandleMotorAndBrakes(float speed)
    {
        // --- Расчет мощности двигателя и турбо ---
        float activeMotorTorque = motorForce * throttleInput * accelerationCurve.Evaluate(speed / maxSpeed);

        HandleTurbo(); // Проверяем и применяем турбо
        if (turboInput && currentBoostAmount > 0)
        {
            activeMotorTorque += turbo.boostForce;
        }

        // --- Расчет тормозного усилия ---
        float finalBrakeTorque = 0f;
        if (Mathf.Abs(throttleInput) < 0.1f && speed > 1f && !handbrakeInput)
        {
            finalBrakeTorque = engineBrakeForce;
        }
        else if (throttleInput < -0.1f && Vector3.Dot(transform.forward, rb.linearVelocity) > 0.5f)
        {
            finalBrakeTorque = brakeForce;
            activeMotorTorque = 0;
        }

        // --- Применение сил ---
        float frontBrake = finalBrakeTorque * brakeBias, rearBrake = finalBrakeTorque * (1 - brakeBias);
        if (handbrakeInput) { frontBrake = 0; rearBrake = handbrakeForce; activeMotorTorque = 0; }

        frontLeftCollider.brakeTorque = frontRightCollider.brakeTorque = frontBrake;
        rearLeftCollider.brakeTorque = rearRightCollider.brakeTorque = rearBrake;

        DistributeTorque(activeMotorTorque);
    }

    /// <summary> Распределяет мощность по колесам в зависимости от типа привода. </summary>
    void DistributeTorque(float torque)
    {
        float frontT = 0, rearT = 0;
        switch (driveType)
        {
            case DriveType.Задний: rearT = torque; break;
            case DriveType.Передний: frontT = torque; break;
            case DriveType.Полный: frontT = torque * awdTorqueSplit; rearT = torque * (1 - awdTorqueSplit); break;
        }
        frontLeftCollider.motorTorque = frontRightCollider.motorTorque = frontT;
        rearLeftCollider.motorTorque = rearRightCollider.motorTorque = rearT;
    }

    /// <summary> Управляет поворотом колес с учетом эффекта Аккермана. </summary>
    void HandleSteering(float speed)
    {
        float maxAngle = steeringCurve.Evaluate(speed);
        float targetAngle = steerInput * maxAngle;
        currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetAngle, Time.fixedDeltaTime * steerResponsiveness);

        if (steerInput > 0)
        { // Поворот направо
            frontLeftCollider.steerAngle = currentSteerAngle * (1 + ackermannFactor);
            frontRightCollider.steerAngle = currentSteerAngle * (1 - ackermannFactor);
        }
        else if (steerInput < 0)
        { // Поворот налево
            frontLeftCollider.steerAngle = currentSteerAngle * (1 - ackermannFactor);
            frontRightCollider.steerAngle = currentSteerAngle * (1 + ackermannFactor);
        }
        else
        {
            frontLeftCollider.steerAngle = frontRightCollider.steerAngle = currentSteerAngle;
        }
    }

    /// <summary> Управляет системой турбо-ускорения. </summary>
    void HandleTurbo()
    {
        if (boostCooldownTimer > 0)
        {
            boostCooldownTimer -= Time.deltaTime;
            return; // Если турбо на перезарядке, выходим
        }

        if (turboInput && throttleInput > 0)
        {
            currentBoostAmount -= Time.deltaTime;
            if (currentBoostAmount <= 0)
            {
                currentBoostAmount = 0;
                boostCooldownTimer = turbo.boostCooldown; // Активируем кулдаун
            }
        }
        else
        {
            if (currentBoostAmount < turbo.boostDuration)
            {
                currentBoostAmount += Time.deltaTime * turbo.boostRechargeRate;
            }
        }
        currentBoostAmount = Mathf.Clamp(currentBoostAmount, 0, turbo.boostDuration);
    }

    /// <summary> Управляет звуками двигателя и покрышек. </summary>
    void HandleSounds()
    {
        // --- Звук двигателя ---
        if (engineSoundSource != null)
        {
            float averageRPM = (Mathf.Abs(rearLeftCollider.rpm) + Mathf.Abs(rearRightCollider.rpm)) / 2;
            float rpmRatio = Mathf.Clamp01(averageRPM / 2000f); // 2000 RPM как условный максимум
            float targetPitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, rpmRatio);
            targetPitch += Mathf.Abs(throttleInput) * 0.15f; // Добавляем "рычания" при нажатии на газ
            engineSoundSource.pitch = Mathf.Lerp(engineSoundSource.pitch, targetPitch, Time.deltaTime * 5f);
        }

        // --- Звук визга покрышек ---
        if (skidSoundSource != null)
        {
            float maxSlip = GetMaxWheelSlip();
            bool isSlipping = maxSlip > slipThreshold || (handbrakeInput && rb.linearVelocity.magnitude > 10f);

            float targetVolume = isSlipping ? Mathf.Clamp01((maxSlip - slipThreshold) * 2f) : 0;
            skidSoundSource.volume = Mathf.Lerp(skidSoundSource.volume, targetVolume, Time.deltaTime * 10f);
        }
    }

    /// <summary> Управляет визуальными эффектами, например, дымом. </summary>
    void HandleEffects()
    {
        bool isSlipping = GetMaxWheelSlip() > slipThreshold || (handbrakeInput && rb.linearVelocity.magnitude > 5f);
        foreach (var smoke in tireSmokeEffects)
        {
            var emission = smoke.emission;
            emission.enabled = isSlipping;
        }
    }

    void ApplyPassiveForces()
    {
        rb.AddForce(-transform.up * downforceFactor * rb.linearVelocity.magnitude);
        if (rb.linearVelocity.magnitude < 0.5f) return;
        rb.AddForce(-rb.linearVelocity.normalized * rollingResistance);
        float speed_ms = rb.linearVelocity.magnitude;
        float airDrag = 0.5f * speed_ms * speed_ms * airDragCoefficient;
        rb.AddForce(-rb.linearVelocity.normalized * airDrag);
    }

    void AnimateWheels()
    {
        AnimateSingleWheel(frontLeftCollider, frontLeftMesh);
        AnimateSingleWheel(frontRightCollider, frontRightMesh);
        AnimateSingleWheel(rearLeftCollider, rearLeftMesh);
        AnimateSingleWheel(rearRightCollider, rearRightMesh);
    }

    void AnimateSingleWheel(WheelCollider col, Transform mesh)
    {
        if (col == null || mesh == null) return;
        Vector3 pos; Quaternion rot;
        col.GetWorldPose(out pos, out rot);
        mesh.position = pos;
        mesh.rotation = rot;
    }

    void ApplyFrictionSettings()
    {
        WheelFrictionCurve fF_f = frontLeftCollider.forwardFriction, sF_f = frontLeftCollider.sidewaysFriction;
        WheelFrictionCurve fF_r = rearLeftCollider.forwardFriction, sF_r = rearLeftCollider.sidewaysFriction;
        fF_f.stiffness = frontWheelsFriction.forwardStiffness; sF_f.stiffness = frontWheelsFriction.sidewaysStiffness;
        fF_r.stiffness = rearWheelsFriction.forwardStiffness; sF_r.stiffness = rearWheelsFriction.sidewaysStiffness;
        frontLeftCollider.forwardFriction = frontRightCollider.forwardFriction = fF_f;
        frontLeftCollider.sidewaysFriction = frontRightCollider.sidewaysFriction = sF_f;
        rearLeftCollider.forwardFriction = rearRightCollider.forwardFriction = fF_r;
        rearLeftCollider.sidewaysFriction = rearRightCollider.sidewaysFriction = sF_r;
    }

    void InitializeSounds()
    {
        if (engineSoundSource != null) { engineSoundSource.loop = true; engineSoundSource.Play(); }
        if (skidSoundSource != null) { skidSoundSource.loop = true; skidSoundSource.volume = 0; skidSoundSource.Play(); }
    }

    float GetMaxWheelSlip()
    {
        float max = 0;
        foreach (var wheel in allWheels)
        {
            if (wheel.GetGroundHit(out WheelHit hit))
                max = Mathf.Max(max, Mathf.Abs(hit.sidewaysSlip));
        }
        return max;
    }
}