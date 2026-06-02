using UnityEngine;
using UnityEditor.Animations;
using System;

public class InterviewStep
{
    private int duration;
    private string animatorControllerPath;
    private string audioClipPath;

    private Interview interview;

    public InterviewStep(int duration, string animatorControllerPath, Interview owner){
        this.duration = duration;
        this.animatorControllerPath = animatorControllerPath;
        this.audioClipPath = "";
        interview = owner;
    }

    public InterviewStep(string animatorControllerPath, string audioClipPath, Interview owner) {
        this.animatorControllerPath = animatorControllerPath;
        this.audioClipPath = audioClipPath;
        interview = owner;
        AudioClip audioClip = getAudioClip();
        duration = (int) Math.Ceiling(audioClip.length);
    }

    public int getDuration(){
        return duration;
    }

    public AudioClip getAudioClip(){
        string path = interview.getBasePath() + "/Audio/" + audioClipPath;
        Debug.Log("loading from path: " + path);
        return Resources.Load<AudioClip>(path);
    }

    public AnimatorController getAnimatorController(){
        string path = interview.getBasePath() + "/Animations/" + animatorControllerPath;
        Debug.Log("loading from path: " + path);
        return Resources.Load<AnimatorController>(path);
    }

    
}
