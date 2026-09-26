namespace Jaket.UI.Fragments;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

using Jaket.Assets;
using Jaket.Input;
using Jaket.UI.Lib;

using static Jaket.UI.Lib.Pal;

/// <summary> Fragment that is displayed when the player is riding a skateboard. </summary>
public class Skateboard : Fragment
{
    /// <summary> Bars that display the current stamina level. </summary>
    private UICircle[] bars = new UICircle[3];
    /// <summary> Text with speedometer and info about dashes. </summary>
    private Text speedometer;
    /// <summary> Gradient along which the text color changes. </summary>
    private UnityEngine.Gradient gradient = new();

    /// <summary> Speed at which the skateboard moves. </summary>
    private float speed;
    /// <summary> Boost charge used to dash forward. </summary>
    private float boost
    {
        get => nm.boostCharge;
        set => nm.boostCharge = value;
    }
    /// <summary> When the maximum speed is exceeded, deceleration is activated. </summary>
    private bool decelerates;
    /// <summary> Falling particles are used to show deceleration. </summary>
    private GameObject particles;

    public Skateboard(Transform root) : base(root, "Skateboard", false)
    {
        for (int i = 0; i < 3; i++)
            bars[i] = Builder.Circle(Rect("StaminaBar", new(512f, 512f)), .1f, -56 + 38 * i, 8f, color: clear);

        var backgrd = Builder.Image(Rect("Speedometer", new(-512f, -128f, 290f, 74f)), Tex.Back, semi);
        speedometer = Builder.Text(Builder.Rect("Text", backgrd.transform, new()), "", 24, white, TextAnchor.MiddleLeft);

        gradient.SetKeys(
        [
            new(green,  20f / 80f),
            new(orange, 50f / 80f),
            new(red,    80f / 80f),
        ], []);
    }

    public override void Toggle()
    {
        Content.gameObject.SetActive(Shown = Emotes.Current == 0x0B);
        speed = 0f;
        Dest(particles);
    }

    #region vehicle

    public void UpdateVehicle()
    {
        for (int i = 0; i < 3; i++)
        {
            bars[i].ProgressColor = boost < 100f ? cb.staminaEmptyColor : boost < 100f * (i + 1) ? cb.staminaChargingColor : cb.staminaColor;
            bars[i].SetProgress((boost - 100f * i) / 100f);
        }
        speedometer.text = Bundle.Format("skateboard", ((int)speed).ToString(), Int2Hex(gradient.Evaluate(speed / 80f)));

        speed = Mathf.MoveTowards(speed, 20f, (decelerates ? 28f : 14f) * Time.deltaTime);
        boost = Mathf.MoveTowards(boost, 300f, 70f * Time.deltaTime);

        if (speed >= 80f && !decelerates)
        {
            decelerates = true;
            particles = Inst(nm.fallParticle, nmtr);
        }
        if (speed <= 40f && decelerates)
        {
            decelerates = false;
            Dest(particles);
        }

        nm.rb.velocity = (nmtr.forward * speed) with { y = nm.rb.velocity.y };

        // prevent the front and rear wheels from falling underground
        static void Check(Vector3 pos)
        {
            if (Physics.Raycast(pos, Vector3.down, out var hit, 1.5f, EnvMask) && hit.distance > .8f) nmtr.position = nmtr.position with { y = hit.point.y + 1.5f };
        }
        Check(nmtr.position + nmtr.forward * 1.2f);
        Check(nmtr.position - nmtr.forward * 1.2f);
    }

    public void UpdateInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (boost >= 100f || (AssistController.Instance.majorEnabled && AssistController.Instance.infiniteStamina))
            {
                speed += 20f;
                boost -= 100f;

                // major assists make it possible to dash endlessly, that's why boost charge must be clamped
                if (boost < 0f) boost = 0f;

                Inst(nm.dodgeParticle, nmtr.position, nmtr.rotation);
                AudioSource.PlayClipAtPoint(nm.dodgeSound, nmtr.position);
            }
            else Inst(nm.staminaFailSound);
        }
        nmtr.Rotate(Vector3.up * InputManager.Instance.InputSource.Move.ReadValue<Vector2>().x * 120f * Time.deltaTime);
    }

    #endregion
}
