namespace Jaket.UI;

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using BepInEx;

using Jaket.Sprays;

/// <summary> Controls the sprays settings menu. </summary>
public class SpraySettings : CanvasSingleton<SpraySettings>
{
    private static PrefsManager prefs => PrefsManager.Instance;

    // <summary> Toggles sprays visibility on client side. </summary>
    public bool DisableSprays;
    /// <summary> Selected spray name. </summary>
    public string SelectedSpray;

    public Text SprayInfoText;
    public Transform SprayTable;
    public List<Button> SprayButtons = new();
    public Image SprayImage;

    /// <summary> Updates settings from preferences. </summary>
    public void UpdateSettings()
    {
        DisableSprays = prefs.GetBool("jaket.disable-sprays", false);
        SelectedSpray = prefs.GetString("jaket.selected-spray", "");
    }

    private void Start()
    {
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
        ChangeSpray(SelectedSpray);
        Rebuild();
    }

    // <summary> Changes the selected spray and updates the UI. </summary>
    public void ChangeSpray(string name)
    {
        if (!name.IsNullOrWhiteSpace() && SprayManager.SetSpray(name) != null)
        {
            SelectedSpray = name;
            prefs.SetString("jaket.selected-spray", name);
        }
    }

    // <summary> Toggles visibility of settings. </summary>
    public void Toggle(bool toggle = false)
    {
        gameObject.SetActive(Shown = toggle);
    }

    public string GetSprayCurrentString(string spray) => $"<size=20><color=white>Current spray is:\n{spray}</color></size>";
    public string GetSprayInfoString(string spray) => 
        $"{GetSprayCurrentString(spray)}\nYour spray applies when scene is changed or player is joined\n<size=16>Max size is {GetSizeString(SprayFile.IMAGE_MAX_SIZE)}</size>";

    // <summary> Gets a string with the given size in bytes in KB or MB in format: "123KB" or "123MB". </summary>
    public string GetSizeString(double size)
    {
        if (size < 1024 * 1024) return $"{size / 1024:0.##}KB";
        else return $"{size / 1024 / 1024:0.##}MB";
    }

    public void Rebuild()
    {
        // destroy all buttons and clear the list
        SprayButtons.ForEach(b => Destroy(b.gameObject));
        SprayButtons.Clear();
        if (SprayInfoText != null) Destroy(SprayInfoText.gameObject);

        for (int i = 0; i < 5; i++) // limit to 5 sprays shown at once
        {
            if (i < SprayManager.FileSprays.Count)
            {
                var spray = SprayManager.FileSprays.Values.ElementAt(i);
                SprayButtons.Add(UI.IconTextureButton(spray.GetShortName(), spray.Texture, SprayTable, 0f, 325f - i * 54f, clicked: () =>
                {
                    if (spray.CheckSize()) // check if the spray is too big 
                    {
                        UI.SendMsg
                        (
                            $"<color=red>Spray is too large</color> ({GetSizeString(spray.ImageData.Length)})" + 
                            "\n<color=red>Please, choose another one</color>" + 
                            $"\nMax size is {GetSizeString(SprayFile.IMAGE_MAX_SIZE)}"
                        );
                        return;
                    }
                    ChangeSpray(spray.Name);
                    Rebuild();
                }, color: spray.CheckSize() ? Color.red : Color.white)); // set color based if spray is too big
            }
            else
            {
                // create empty buttons if there are no sprays
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
            SprayInfoText = UI.Text($"No sprays found. Add one to the folder.\n{GetSprayCurrentString(GetNameString())}",
                SprayTable, 0f, 40f, height: 72f, color: Color.gray, size: 17);
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