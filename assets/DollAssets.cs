namespace Jaket.Assets;

using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

using Jaket.Content;
using Jaket.Net.EntityTypes;

/// <summary> Class that works with the assets bundle for the player doll. </summary>
public class DollAssets
{
    /// <summary> Bundle containing assets for player doll. </summary>
    public static AssetBundle Bundle;

    /// <summary> Player doll and its preview prefabs. </summary>
    public static GameObject Doll, Preview;

    /// <summary> Player doll icon. </summary>
    public static Sprite Icon;

    /// <summary> Shader used by the game for materials. </summary>
    public static Shader Shader;

    /// <summary> Wing textures used to differentiate teams. </summary>
    public static Texture[] WingTextures;

    /// <summary> Loads assets bundle and other necessary stuff. </summary>
    public static void Load()
    {
        Bundle = LoadBundle();

        // cache the shader and the wing textures for future use
        var V2 = AssetHelper.LoadPrefab("cb3828ada2cbefe479fed3b51739edf6").GetComponent<V2>();

        Shader = V2.smr.material.shader;
        WingTextures = new Texture[5];

        // loading wing textures from the bundle
        for (int i = 0; i < 4; i++)
        {
            var index = i; // C# sucks
            LoadAsync<Texture>("V3-wings-" + ((Team)i).ToString(), tex => WingTextures[index] = tex);
        }
        WingTextures[4] = V2.wingTextures[1];

        // create prefabs of the player doll and its preview
        LoadAsync<GameObject>("Player Doll.prefab", prefab =>
        {
            Object.DontDestroyOnLoad(prefab);
            FixMaterials(prefab);

            Doll = prefab;
        });

        // I guess async will improve performance a little bit
        LoadAsync<Sprite>("V3-icon", sprite => Icon = sprite);
    }

    /// <summary> Finds and loads an assets bundle. </summary>
    public static AssetBundle LoadBundle()
    {
        string assembly = Assembly.GetExecutingAssembly().Location;                                  // location of Jaket.dll
        string directory = assembly.Substring(0, assembly.LastIndexOf(Path.DirectorySeparatorChar)); // mod folder
        string bundle = Path.Combine(directory, "jaket-player-doll.bundle");                         // location of bundle

        return AssetBundle.LoadFromFile(bundle);
    }

    /// <summary> Finds and asynchronously loads an asset. </summary>
    public static void LoadAsync<T>(string name, UnityAction<T> cons) where T : Object
    {
        var task = Bundle.LoadAssetAsync<T>(name);
        task.completed += _ => cons(task.asset as T);
    }

    /// <summary> Changes the colors of materials and their shaders to match the style of the game.. </summary>
    public static void FixMaterials(GameObject obj)
    {
        foreach (var mat in obj.GetComponentInChildren<SkinnedMeshRenderer>().materials)
        {
            mat.color = Color.white;
            mat.shader = Shader;
        }
    }

    /// <summary> Tags after loading from a bundle changes due to a mismatch in the tags list, this method returns everything to its place. </summary>
    public static string MapTag(string tag) => tag switch
    {
        "RoomManager" => "Body",
        "Body" => "Limb",
        "Forward" => "Head",
        _ => tag
    };


    /// <summary> Creates a new player doll from the prefab. </summary>
    public static RemotePlayer CreateDoll()
    {
        // create a doll from the prefab obtained from the bundle
        var obj = Object.Instantiate(Doll, Vector3.zero, Quaternion.identity);

        // it is necessary that the client doesn't consider the enemyId as a local object
        obj.name = "Net";

        // add components
        var enemyId = obj.AddComponent<EnemyIdentifier>();
        var machine = obj.AddComponent<Machine>();

        enemyId.enemyClass = EnemyClass.Machine;
        enemyId.enemyType = EnemyType.V2;
        enemyId.weaknesses = new string[0];
        enemyId.burners = new();
        machine.destroyOnDeath = new GameObject[0];
        machine.hurtSounds = new AudioClip[0];

        // add enemy identifier to all doll parts so that bullets can hit it
        foreach (var rigidbody in obj.transform.GetChild(0).GetChild(0).GetComponentsInChildren<Rigidbody>())
        {
            rigidbody.gameObject.AddComponent<EnemyIdentifierIdentifier>();
            rigidbody.gameObject.tag = MapTag(rigidbody.gameObject.tag);
        }

        // add a script to further control the doll
        return obj.AddComponent<RemotePlayer>();
    }
}
