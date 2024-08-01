namespace Jaket.World;

using UnityEngine.UI;

using Jaket.Net;

/// <summary> Class that manages voting for the skip of a cutscene or an option at 2-S. </summary>
public class Votes
{
    /// <summary> Loads the vote system. </summary>
    public static void Load() => Events.OnLoaded += () =>
    {
        if (LobbyController.Online && Tools.Scene == "Level 2-S") Init2S();
    };

    /// <summary> Replaces Mirage with Virage, patches buttons and other minor stuff. </summary>
    public static void Init2S()
    {
        #region Mirage 2 Virage

        // sprite
        var fallen = Tools.ObjFind("Canvas/PowerUpVignette/Panel/Aspect Ratio Mask/Fallen");
        for (int i = 0; i < 4; i++)
            fallen.transform.GetChild(i).GetComponent<Image>().sprite = null;

        Tools.ResFind<SpritePoses>(sp => Tools.IsReal(sp) && sp.copyChangeTo.Length > 0, sp => sp.poses = null);

        // name
        var dialog = Tools.ObjFind("Canvas/PowerUpVignette/Panel/Aspect Ratio Mask/Middle/Dialogue Box (A4)/Text").GetComponent<Text>();
        dialog.text = dialog.text.Replace("Mirage", "Virage");

        dialog.font.RequestCharactersInTexture("3", 22);
        dialog.font.GetCharacterInfo('3', out var info, 22);
        var virage = $"VIRAG <quad size=18 x={info.uvBottomLeft.x:0.0000} y={info.uvBottomLeft.y:0.0000} width={info.uvTopRight.x - info.uvBottomLeft.x:0.0000} height={info.uvTopRight.y - info.uvBottomLeft.y:0.0000}> :".Replace(',', '.');

        Tools.ResFind<IntermissionController>(Tools.IsReal, ic => ic.preText = ic.preText.Replace("MIRAGE:", virage));

        #endregion
    }
}
