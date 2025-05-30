using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(ScreenDamage))]

public class Custom_SD_Inspector : Editor
{
    SerializedProperty maxHealth,
    bloodyFrame,
    criticalHealth,
    useBlurEffect,
    blurImage,
    blurDuration,
    blurFadeSpeed,
    healingSpeed,
    autoHeal,
    autoHealTime,
    pulseSound,
    fadeAudios,
    audiosToFade,
    audiosFadeVolume,
    deathEvent;


    void OnEnable() 
    {
        maxHealth = serializedObject.FindProperty("maxHealth");
        bloodyFrame = serializedObject.FindProperty("bloodyFrame");
        criticalHealth = serializedObject.FindProperty("criticalHealth");
        useBlurEffect = serializedObject.FindProperty("useBlurEffect");
        blurImage = serializedObject.FindProperty("blurImage");
        blurDuration = serializedObject.FindProperty("blurDuration");
        blurFadeSpeed = serializedObject.FindProperty("blurFadeSpeed");
        healingSpeed = serializedObject.FindProperty("healingSpeed");
        autoHeal = serializedObject.FindProperty("autoHeal");
        autoHealTime = serializedObject.FindProperty("autoHealTime");
        pulseSound = serializedObject.FindProperty("pulseSound");
        fadeAudios = serializedObject.FindProperty("fadeAudios");
        audiosToFade = serializedObject.FindProperty("audiosToFade");
        audiosFadeVolume = serializedObject.FindProperty("audiosFadeVolume");
        deathEvent = serializedObject.FindProperty("deathEvent");
    }


    public override void OnInspectorGUI() 
    {
        ScreenDamage script = (ScreenDamage)target;

        var button = GUILayout.Button("Click for more tools");
        if (button) Application.OpenURL("https://assetstore.unity.com/publishers/39163");
        EditorGUILayout.Space(5);


        EditorGUILayout.LabelField("Health", EditorStyles.boldLabel);
        // Bloody Frame
        EditorGUILayout.PropertyField(bloodyFrame);
        // Max Health
        EditorGUILayout.PropertyField(maxHealth);
        // Critical Health
        EditorGUILayout.PropertyField(criticalHealth);
        

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Blur Effect", EditorStyles.boldLabel);
        // Use Blur Rffect
        EditorGUILayout.PropertyField(useBlurEffect);
        EditorGUI.BeginDisabledGroup (script.useBlurEffect == false);
            // Blur Image
            EditorGUILayout.PropertyField(blurImage);
            // Blur Duration
            EditorGUILayout.PropertyField(blurDuration);
            // Blur Fade Speed
            EditorGUILayout.PropertyField(blurFadeSpeed);
        EditorGUI.EndDisabledGroup();

        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Audios", EditorStyles.boldLabel);
        // Pulse Sound
        EditorGUILayout.PropertyField(pulseSound);

        // Fade World Audios
        EditorGUILayout.PropertyField(fadeAudios);
        
        // disable property when fade world audios is disabled
        EditorGUI.BeginDisabledGroup (script.fadeAudios == false);
            EditorGUILayout.PropertyField(audiosToFade);
            EditorGUILayout.PropertyField(audiosFadeVolume);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Healing", EditorStyles.boldLabel);
        //Auto Heal
        EditorGUILayout.PropertyField(autoHeal);

        // Auto Heal -- If auto heal is false.. disable othe fields
        EditorGUI.BeginDisabledGroup (script.autoHeal == false);
            // Healing Multiplier
            EditorGUILayout.PropertyField(healingSpeed);

            // Auto Heal Time
            EditorGUILayout.PropertyField(autoHealTime);
        EditorGUI.EndDisabledGroup ();


        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Events to call on death", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(deathEvent);

        
        serializedObject.ApplyModifiedProperties();
    }
}
