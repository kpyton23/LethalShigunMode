using BepInEx;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

[BepInPlugin("com.jiho7407.LethalShigunMode", "LethalShigunMode", "1.0.0")]
public class ShiganMods : BaseUnityPlugin
{   
    public static ShiganMods instance;
    public AudioSource shiganAudio;
    public AudioClip attackSound;
    public AudioClip hitSound;

    void Awake()
    {   
        instance = this;
        var harmony = new Harmony("com.jiho7407.LethalShigunMode");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        Logger.LogInfo($"Plugin LethalShigunMode is loaded!");

        shiganAudio = gameObject.AddComponent<AudioSource>();
        DontDestroyOnLoad(shiganAudio.gameObject);

        string attackPath = Path.Combine(Paths.PluginPath, "LethalShigunMode", "attack.mp3");
        Logger.LogInfo($"attackPath: {attackPath}");
        attackSound = LoadAudioClip(attackPath);

        string hitPath = Path.Combine(Paths.PluginPath, "LethalShigunMode", "hit.mp3");
        Logger.LogInfo($"hitPath: {hitPath}");
        hitSound = LoadAudioClip(hitPath);
        
        if(attackSound == null || hitSound == null)
        {
            Logger.LogError("Failed to create audio clip");
        }
        else{
            Logger.LogInfo("Audio clip is successfully created");
        }
    }

    private AudioClip LoadAudioClip(string path)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG))
        {
            www.SendWebRequest();
            while (!www.isDone) { }
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Logger.LogError($"Failed to load audio clip: {www.error}");
                return null;
            }
            else
            {   
                Logger.LogInfo($"Audio clip is loaded: {path}");
                return DownloadHandlerAudioClip.GetContent(www);
            }
        }
    }

    public static void PlayAttackSound()
    {
        if (instance != null && instance.attackSound != null)
        {
            instance.shiganAudio.PlayOneShot(instance.attackSound);
            instance.Logger.LogInfo("Play attack sound");
        }
        else{
            instance.Logger.LogError("Failed to play attack sound");
            if(instance.attackSound == null)
            {
                instance.Logger.LogError("attackSound is null");
            }
            else{
                instance.Logger.LogError("instance is null");
            }
        }
    }

    public static void PlayHitSound()
    {
        if (instance != null && instance.hitSound != null)
        {
            instance.shiganAudio.PlayOneShot(instance.hitSound);
            instance.Logger.LogInfo("Play hit sound");
        }
        else{
            instance.Logger.LogError("Failed to play hit sound");
        }
    }
}


[HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), "StartPerformingEmoteClientRpc")]
public static class EmoteDamagePatch
{
    static void Postfix(GameNetcodeStuff.PlayerControllerB __instance)
    {
        // 감정 표현 번호 확인
        int emoteNumber = __instance.playerBodyAnimator.GetInteger("emoteNumber");
        if (emoteNumber == 2)
        { // 손가락질 -> 지건발동
            ShiganMods.PlayAttackSound();
            ApplyDamage(__instance);
        }
    }

    // 데미지 처리 로직
    static void ApplyDamage(GameNetcodeStuff.PlayerControllerB instance)
    {   
        bool isAttack = false;
        RaycastHit[] hits = Physics.SphereCastAll(instance.gameplayCamera.transform.position + instance.gameplayCamera.transform.right * -0.35f, 0.8f, instance.gameplayCamera.transform.forward, 2.0f, 11012424, QueryTriggerInteraction.Collide);
        foreach (RaycastHit hit in hits)
        {
            IHittable hittable = hit.transform.GetComponent<IHittable>();
            if (hittable != null && hit.transform != instance.transform) // 자기 자신은 제외
            {
                // 즉사급 데미지
                hittable.Hit(20, instance.gameplayCamera.transform.forward, instance, true, 1);
                isAttack = true;
            }
        }
        if (isAttack)
        {
            ShiganMods.PlayHitSound();
        }
    }
}