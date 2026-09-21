using UnityEngine;

// Representa una pregunta disponible para la entrevista Wizard of Oz.
// La idea es que el operador vea shortDescription para identificar rapidamente
// la pregunta, mientras que fullQuestion conserva la transcripcion completa.
[System.Serializable]
public class InterviewQuestion
{
    public int id;

    // Texto corto que se mostrara al operador.
    // Ejemplo: "Trabajo en equipo / conflicto"
    public string shortDescription;

    // Transcripcion completa del audio.
    // Se puede ir llenando conforme transcribamos cada pregunta.
    [TextArea(2, 5)]
    public string fullQuestion;

    // Audio que escuchara la persona dentro de VR.
    public AudioClip audioClip;

    // Sirve para indicar visualmente si la pregunta ya fue utilizada.
    [HideInInspector]
    public bool alreadyAsked;
}
