namespace Jaket.UI;

using UnityEngine;
using UnityEngine.UI;

using System.Collections.Generic;

/// <summary> Controls the sprays settings menu. </summary>
public class SpraySettings : CanvasSingleton<SpraySettings>
{
    /// <summary> Reference to preference manager. </summary>
    private static PrefsManager prefs => PrefsManager.Instance;

    #region Spray settings
    // <summary> Toggles sprays visibility on client side. </summary>
    public bool DisableSprays;
    /// <summary> Selected spray name. </summary>
    public string SelectedSpray;
    #endregion

    public Text SprayInfoText;
    public Transform SprayTable;
    public List<Button> SprayButtons = new();
    public Image SprayImage;

    public void UpdateSettings()
    {
        DisableSprays = prefs.GetBool("jaket.disable-sprays", false);
        SelectedSpray = prefs.GetString("jaket.selected-spray", "");
    }

    private void Start()
    {
        UpdateSettings();

        UI.Shadow("Shadow", transform);
        UI.TableAT("Sprays", transform, 0f, 352f, 826f, table =>
        {
            UI.Text("--SPRAYS--", table, 0f, 381f);

            SprayTable = table;

            UI.Button("Show folder", table, 0f, -373f, clicked: () => Application.OpenURL($"file://{SprayManager.SpraysPath.Replace("\\", "/")}"));
            UI.Button("Update", table, 0f, -317f, clicked: () =>
            {
                SprayManager.LoadFileSprays();
                Rebuild();
            });
            SprayImage = UI.ImageFromTexture("Spray preview", table, 0f, -150f, Texture2D.whiteTexture, 256f, 256f);
        });
        UI.TableAT("Settings", transform, 842f, 352f, 200f, table =>
        {
            UI.Text("--SETTINGS--", table, 0f, 68f);
            UI.Toggle("DISABLE SPRAYS", table, 0f, 20f, size: 16, clicked: force =>
            {
                prefs.SetBool("jaket.disable-sprays", DisableSprays = force);
            }).isOn = DisableSprays;
        });

        WidescreenFix.MoveDown(transform);
        Rebuild();
    }

    public void ChangeSpray(string name)
    {
        SprayManager.ChangeFileSpray(name);
        prefs.SetString("jaket.selected-spray", name);
        SelectedSpray = name;
        Rebuild();
    }

    // <summary> Toggles visibility of settings. </summary>
    public void Toggle(bool toggle = false)
    {
        // if another menu is open, then nothing needs to be done
        if (UI.AnyJaket() && !Shown) return;

        gameObject.SetActive(Shown = toggle);
    }

    public string GetSprayCurrentString(string spray) => $"<size=20><color=white>Current spray is:\n{spray}</color></size>";
    public string GetSprayInfoString(string spray) => $"{GetSprayCurrentString(spray)}\nYour spray applies when scene is changed or player is joined\n<size=16>Max size is {SprayFile.ImageMaxSize / 1024f:0.##}kb</size>";

    public void Rebuild()
    {
        if (!SprayManager.ChangeFileSpray(SelectedSpray)) SelectedSpray = "";

        // destroy all buttons and clear the list
        SprayButtons.ForEach(b => Destroy(b.gameObject));
        SprayButtons.Clear();
        if (SprayInfoText != null) Destroy(SprayInfoText.gameObject);

        for (int i = 0; i < 5; i++) // limit to 5 sprays shown at once
        {
            if (i < SprayManager.FileSprays.Count)
            {
                var spray = SprayManager.FileSprays[i];
                SprayButtons.Add(UI.IconTextureButton(spray.GetShortName(), spray.Texture, SprayTable, 0f, 325f - i * 54f, clicked: () =>
                {
                    if (spray.CheckSize()) // check if the spray is too big 
                    {
                        UI.SendMsg($"<color=red>Spray is too large. Please, choose another one.</color>\nMax size is {SprayFile.ImageMaxSize / 1024f:0.##}kb.");
                        return;
                    }
                    ChangeSpray(spray.Name);
                }, color: spray.CheckSize() ? Color.red : Color.white)); // set color based if spray is too big
            }
            else
            {
                var t = new Texture2D(1, 1);
                t.SetPixel(0, 0, Color.gray);
                t.Apply();

                var btn = UI.IconTextureButton("", t, SprayTable, 0f, 325f - i * 54f, color: Color.gray);
                btn.interactable = false;
                SprayButtons.Add(btn);
            }
        };

        static string GetNameString() => SprayManager.CurrentSpray != null ? SprayManager.CurrentSpray.GetShortName(18) : "<color=gray>None</color>";

        if (SprayManager.FileSprays.Count == 0)
            SprayInfoText = UI.Text($"No sprays found. Add one to the folder.\n{GetSprayCurrentString(GetNameString())}", SprayTable, 0f, 40f, height: 72f, color: Color.gray, size: 17);
        else
            SprayInfoText = UI.Text(GetSprayInfoString(GetNameString()), // show current spray info
                    SprayTable, 0f, 32f, height: 88f, color: Color.gray, size: 17);

        if (SprayManager.CurrentSpray != null)
        {
            var texture = SprayManager.CurrentSpray.Texture;
            SprayImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}