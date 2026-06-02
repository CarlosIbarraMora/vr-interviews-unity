using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterviewController : MonoBehaviour
{
    private Animator animator;
    private AudioSource audioSource;
    private AudioSource recordSource; //Objeto para manejar el la grabacion de respuestas
    private List<Interview> interviews;
    private int cont; //Contador para enlistar grabaciones
    private bool condition; //Condicional para definir cuando guardar las grabaciones

    //rutas de animaciones para interviewer
    
    // private string listeningAnim = "listening";
    // private string listeningAnim2 = "listening2";
    // private string speakingAnim = "speaking";
    // private string speakingAnim2 = "speaking2";
    // private string nodAnim = "nod";

    //Definición de colecciones de animaciones para cada estado
    private string[] talkingAnimations = 
    {"talking0","talking1", "talking2", "talking3", "talking4", "talking5"};
    private string[] listeningAnimations = 
    {"listeningIdle1", "listeningIdle2", "listeningIdle3", "listeningIdle4", "listeningIdle5" };
    //Setup para modificar la orientacion del modelo al correr animaciones
    private Transform interviewerTransform;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private int maxWait = 20;

    void Start()
    {
        GameObject interviewer = GameObject.FindGameObjectWithTag("Interviewer");

        //Inicializacion de componentes del entrevistador
        interviewerTransform = interviewer.transform;
        animator = interviewer.GetComponent<Animator>();
        audioSource = interviewer.GetComponent<AudioSource>();
        recordSource = interviewer.GetComponent<AudioSource>(); //Inicializacion

        Interview interview1;
        cont = 1; //Inicializacion
        condition = true; //Inicializacion

        //Se guarda la posicion y rotacion original del entrevistador
        originalPosition = interviewerTransform.position;
        originalRotation = interviewerTransform.rotation;

        interview1 = new Interview("1 To 1", "personal");
        interview1.addStep(new InterviewStep(GetRandomAnimation(talkingAnimations), "Intro 1", interview1));
        interview1.addStep(new InterviewStep(maxWait, GetRandomAnimation(listeningAnimations), interview1));
        interview1.addStep(new InterviewStep(GetRandomAnimation(talkingAnimations), "EA 1", interview1));
        interview1.addStep(new InterviewStep(maxWait, GetRandomAnimation(listeningAnimations), interview1));
        interview1.addStep(new InterviewStep(GetRandomAnimation(talkingAnimations), "EA 2.1", interview1));
        interview1.addStep(new InterviewStep(maxWait, GetRandomAnimation(listeningAnimations), interview1));
        interview1.addStep(new InterviewStep(GetRandomAnimation(talkingAnimations), "EA 3", interview1));
        interview1.addStep(new InterviewStep(maxWait, GetRandomAnimation(listeningAnimations), interview1));
        interview1.addStep(new InterviewStep(GetRandomAnimation(talkingAnimations), "EA 4", interview1));
        interview1.addStep(new InterviewStep(maxWait, GetRandomAnimation(listeningAnimations), interview1));

        print("starting interview");
        StartCoroutine(runInterview(interview1));
    }
    private IEnumerator runInterview(Interview interview)
    {
        //Se obtiene la lista de pasos de la entrevista
        List<InterviewStep> steps = interview.getSteps();

        //Se recorren todos los pasos de la entrevista
        foreach (InterviewStep step in steps)
        {
            // Si es un paso con audio, el entrevistador habla
            if(step.getAudioClip() != null)
            {
                PlaySmoothAnimations(GetRandomAnimation(talkingAnimations));
            }
            // Si no, el entrvistador escucha
            else
            {
                PlaySmoothAnimations(GetRandomAnimation(listeningAnimations));
            }

            //Se espera un frame para permitir que Unity cargue correctamente la animacion
            yield return null;

            //Se corrige la posicion y orientacion del modelo
            interviewerTransform.position = originalPosition + new Vector3(0, 0.15f, 0);
            interviewerTransform.rotation = originalRotation * Quaternion.Euler(0, 90, 0);

            //Si el paso tiene audio, significa que el entrevistador hablara
            if (step.getAudioClip() != null)
            {
                //Se asigna y reproduce el audio del entrevistador
                audioSource.clip = step.getAudioClip();
                audioSource.Play();

                //La condicional se vuelve true para indicar que NO se grabara respuesta
                condition = true;

                //Se espera hasta que termine el audio antes de pasar al siguiente paso
                yield return new WaitForSeconds(step.getAudioClip().length);
            }
            else
            {
                //Si no hay audio, significa que es turno del usuario para responder

                //Se obtiene el primer dispositivo de grabacion disponible
                string recordDevice = Microphone.devices[0];

                //Se inicia la grabacion del microfono
                recordSource.clip = Microphone.Start(
                    recordDevice,
                    true,
                    step.getDuration() + 1,
                    44100
                );

                //La condicional se vuelve false para permitir guardar la grabacion
                condition = false;

                print("Grabando respuesta... Presiona ESPACIO o ENTER para continuar.");

                //El sistema permanece esperando hasta que el operador presione ESPACIO
                while (!(Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
                {
                    yield return null;
                }

                //Se detiene la grabacion del microfono
                Microphone.End(recordDevice);
            }

            //Si el paso fue una respuesta del usuario, se guarda el archivo de audio
            if (!condition)
            {
                SavWav.Save("Record_Answer_" + cont, recordSource.clip);
                cont++;
            }

            //Se limpian los componentes antes de pasar al siguiente paso
            audioSource.Stop();
            audioSource.clip = null;
        }
    }

    //Función para obtener una animación random según el estado
    private string GetRandomAnimation(string[] animations)
    {
        string animation = animations[Random.Range(0, animations.Length)];
        print("current animation: " + animation);
        return animation;
    }
    //Función para suavizar las transiciones entre animaciones
    private void PlaySmoothAnimations(string animationName)
    {
        animator.CrossFade(animationName, 0.35f);
    }
}