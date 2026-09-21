using UnityEngine;
using UnityEditor.Animations;
using System;

public class InterviewStep
{
    private int duration;
    private string animatorControllerPath;
    private string audioClipPath;
    private string audioFolder;

    private Interview interview;

    // Constructor para pasos de espera/escucha (sin audio).
    public InterviewStep(int duration, string animatorControllerPath, Interview owner)
    {
        this.duration = duration;
        this.animatorControllerPath = animatorControllerPath;
        this.audioClipPath = "";
        this.audioFolder = "";
        interview = owner;
    }

    // Constructor original. Se conserva por compatibilidad y usa NewAudios.
    public InterviewStep(string animatorControllerPath, string audioClipPath, Interview owner)
        : this(animatorControllerPath, audioClipPath, "NewAudios", owner)
    {
    }

    // Nuevo constructor: permite elegir si el audio viene de OldAudios o NewAudios.
    public InterviewStep(string animatorControllerPath, string audioClipPath, string audioFolder, Interview owner)
    {
        this.animatorControllerPath = animatorControllerPath;
        this.audioClipPath = audioClipPath;
        this.audioFolder = audioFolder;
        interview = owner;

        AudioClip audioClip = getAudioClip();

        if (audioClip != null)
        {
            duration = (int)Math.Ceiling(audioClip.length);
        }
        else
        {
            duration = 0;
            Debug.LogError("No se encontro el audio: " + audioClipPath + " en " + audioFolder);
        }
    }

    public int getDuration()
    {
        return duration;
    }

    public AudioClip getAudioClip()
    {
        // Los pasos de espera no tienen audio.
        if (string.IsNullOrEmpty(audioClipPath))
        {
            return null;
        }

        string path = interview.getBasePath() + "/Audio/" + audioFolder + "/" + audioClipPath;
        Debug.Log("loading from path: " + path);
        return Resources.Load<AudioClip>(path);
    }

    public AnimatorController getAnimatorController()
    {
        string path = interview.getBasePath() + "/Animations/" + animatorControllerPath;
        Debug.Log("loading from path: " + path);
        return Resources.Load<AnimatorController>(path);
    }
}
