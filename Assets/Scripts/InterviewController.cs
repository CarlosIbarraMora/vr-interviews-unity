using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterviewController : MonoBehaviour
{
    private Animator animator;
    private AudioSource audioSource;
    private AudioSource recordSource;

    private int cont; // Contador para nombrar las grabaciones.

    // Animaciones disponibles para cada estado del entrevistador.
    private string[] talkingAnimations =
    {"talking1", "talking2", "talking3", "talking4", "talking5" };

    private string[] listeningAnimations =
    { "listeningIdle1", "listeningIdle2", "listeningIdle3", "listeningIdle4", "listeningIdle5" };

    // Setup para conservar posicion y orientacion del modelo.
    private Transform interviewerTransform;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private bool interviewFinished = false;
    private bool isRecording = false;
    private string currentRecordDevice;

    // Tiempo maximo disponible para una respuesta.
    // La respuesta tambien puede terminar antes presionando ESPACIO o ENTER.
    private int maxWait = 20;

    [Header("Wizard of Oz - Preguntas")]
    [Tooltip("Agrega aqui cada pregunta con su texto corto, transcripcion y AudioClip.")]
    public List<InterviewQuestion> questions = new List<InterviewQuestion>();

    private int selectedQuestionIndex = 0;
    private bool interviewReady = false;
    private bool questionInProgress = false;

    void Start()
    {
        GameObject interviewer = GameObject.FindGameObjectWithTag("Interviewer");

        if (interviewer == null)
        {
            Debug.LogError("No se encontro un GameObject con el tag 'Interviewer'.");
            return;
        }

        // Inicializacion de componentes del entrevistador.
        interviewerTransform = interviewer.transform;
        animator = interviewer.GetComponent<Animator>();
        audioSource = interviewer.GetComponent<AudioSource>();
        recordSource = interviewer.GetComponent<AudioSource>();

        cont = 1;

        // Se guarda la posicion y rotacion original del entrevistador.
        originalPosition = interviewerTransform.position;
        originalRotation = interviewerTransform.rotation;

        // La introduccion se conserva como antes:
        // 1) reproduce Intro 1
        // 2) pasa a escucha y espera a que el operador presione ESPACIO/ENTER
        // IMPORTANTE: Intro 1 ahora se carga desde la carpeta OldAudios.
        Interview introInterview = new Interview("1 To 1", "personal");
        introInterview.addStep(
            new InterviewStep(
                GetRandomAnimation(talkingAnimations),
                "Intro 1",
                "OldAudios",
                introInterview
            )
        );

        introInterview.addStep(
            new InterviewStep(
                maxWait,
                GetRandomAnimation(listeningAnimations),
                introInterview
            )
        );

        Debug.Log("Starting interview introduction...");
        StartCoroutine(RunIntroduction(introInterview));
    }

    void Update()
    {
         // ESC puede terminar la entrevista en cualquier momento.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EndInterview();
            return;
        }

        // Si la entrevista ya terminó, ignoramos cualquier otro control.
        if (interviewFinished)
        {
            return;
        }

        // Hasta que termine la introduccion, el operador no puede seleccionar preguntas.
        if (!interviewReady || questionInProgress || questions.Count == 0)
        {
            return;
        }

        // Flecha abajo: siguiente pregunta.
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedQuestionIndex = (selectedQuestionIndex + 1) % questions.Count;
            PrintSelectedQuestion();
        }

        // Flecha arriba: pregunta anterior.
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedQuestionIndex--;

            if (selectedQuestionIndex < 0)
            {
                selectedQuestionIndex = questions.Count - 1;
            }

            PrintSelectedQuestion();
        }

        // ENTER reproduce la pregunta seleccionada.
        if (Input.GetKeyDown(KeyCode.Return))
        {
            InterviewQuestion selectedQuestion = questions[selectedQuestionIndex];

            if (selectedQuestion.audioClip == null)
            {
                Debug.LogWarning(
                    "La pregunta " + selectedQuestion.id +
                    " no tiene AudioClip asignado."
                );
                return;
            }

            StartCoroutine(AskQuestion(selectedQuestion));
        }
    }

    // Ejecuta solamente la introduccion inicial y la espera de preparacion.
    private IEnumerator RunIntroduction(Interview interview)
    {
        List<InterviewStep> steps = interview.getSteps();

        foreach (InterviewStep step in steps)
        {
            AudioClip clip = step.getAudioClip();

            if (clip != null)
            {
                PlaySmoothAnimations(GetRandomAnimation(talkingAnimations));
                FixInterviewerTransform();

                audioSource.clip = clip;
                audioSource.Play();

                yield return new WaitForSeconds(clip.length);

                audioSource.Stop();
                audioSource.clip = null;
            }
            else
            {
                PlaySmoothAnimations(GetRandomAnimation(listeningAnimations));
                FixInterviewerTransform();

                Debug.Log(
                    "Introduccion terminada. Esperando a que todos esten listos... " +
                    "Presiona ESPACIO o ENTER para comenzar la entrevista."
                );

                while (!(Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
                {
                    yield return null;
                }
            }
        }

        interviewReady = true;

        Debug.Log("========================================");
        Debug.Log("     LISTA DE CONTROLES PARA EL OPERADOR     "); 
        Debug.Log("========================================");
        Debug.Log("Flecha ARRIBA / ABAJO = cambiar pregunta");
        Debug.Log("ENTER = hacer pregunta seleccionada");
        Debug.Log("ESPACIO = terminar respuesta del participante");
        Debug.Log("ESC = terminar entrevista");
        Debug.Log("========================================");

        PrintQuestionList();
        PrintSelectedQuestion();
    }

    // Reproduce una pregunta elegida por el operador y despues graba la respuesta.
    private IEnumerator AskQuestion(InterviewQuestion question)
    {
        questionInProgress = true;

        Debug.Log("----------------------------------------");
        Debug.Log("PREGUNTA " + question.id + ": " + question.shortDescription);

        if (!string.IsNullOrWhiteSpace(question.fullQuestion))
        {
            Debug.Log("Texto completo: " + question.fullQuestion);
        }

        // El entrevistador habla.
        PlaySmoothAnimations(GetRandomAnimation(talkingAnimations));
        FixInterviewerTransform();

        audioSource.clip = question.audioClip;
        audioSource.Play();

        yield return new WaitForSeconds(question.audioClip.length);

        audioSource.Stop();
        audioSource.clip = null;

        // El entrevistador pasa a escuchar.
        PlaySmoothAnimations(GetRandomAnimation(listeningAnimations));
        FixInterviewerTransform();

        // Inicia la grabacion de la respuesta.
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("No se encontro ningun microfono. No se grabara la respuesta.");
        }
        else
        {
            currentRecordDevice = Microphone.devices[0];

            recordSource.clip = Microphone.Start(
                currentRecordDevice,
                true,
                maxWait + 1,
                44100
            );

            isRecording = true;

            Debug.Log("Grabando respuesta... Presiona ESPACIO para terminar.");

            float elapsedTime = 0f;

            // Se conserva un maximo para evitar una grabacion infinita,
            // pero el operador puede terminarla antes con ESPACIO.
            while (!Input.GetKeyDown(KeyCode.Space) && elapsedTime < maxWait)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            Microphone.End(currentRecordDevice);
            isRecording = false;

            SavWav.Save(
                "Record_Answer_Q" + question.id + "_" + cont,
                recordSource.clip
            );

            cont++;
        }

        question.alreadyAsked = true;
        questionInProgress = false;

        Debug.Log("Respuesta terminada. Selecciona la siguiente pregunta.");
        PrintQuestionList();
        PrintSelectedQuestion();
    }

    // Muestra todas las preguntas de forma legible para el operador.
    private void PrintQuestionList()
    {
        Debug.Log("========== LISTA DE PREGUNTAS ==========");

        for (int i = 0; i < questions.Count; i++)
        {
            InterviewQuestion question = questions[i];
            string usedMark = question.alreadyAsked ? "[X]" : "[ ]";

            Debug.Log(
                usedMark + " " +
                question.id + " - " +
                question.shortDescription
            );
        }

        Debug.Log("=========================================");
    }

    // Muestra la pregunta sobre la que esta parado actualmente el selector.
    private void PrintSelectedQuestion()
    {
        if (questions.Count == 0)
        {
            Debug.LogWarning(
                "No hay preguntas configuradas. Agregalas en el Inspector " +
                "dentro de InterviewController > Questions."
            );
            return;
        }

        InterviewQuestion question = questions[selectedQuestionIndex];

        Debug.Log(
            "> SELECTED: " +
            question.id + " - " +
            question.shortDescription
        );
    }

    // Corrige la posicion y orientacion del modelo despues de cambiar animacion.
    private void FixInterviewerTransform()
        {
            interviewerTransform.position = originalPosition + new Vector3(0, 0.15f, 0);
            interviewerTransform.rotation = originalRotation * Quaternion.Euler(0, 90, 0);
        }

        // Obtiene una animacion aleatoria segun el estado.
        private string GetRandomAnimation(string[] animations)
        {
            string animation = animations[Random.Range(0, animations.Length)];
            Debug.Log("current animation: " + animation);
            return animation;
        }

        // Suaviza la transicion entre animaciones.
        private void PlaySmoothAnimations(string animationName)
    {
        int layerIndex = 0;
        int stateHash = Animator.StringToHash(animationName);

        if (animator.HasState(layerIndex, stateHash))
        {
            animator.CrossFade(stateHash, 0.35f, layerIndex);
        }
        else
        {
            Debug.LogWarning(
                "No existe la animación: " + animationName
            );
        }
    }
    private void EndInterview()
    {
        // Evita ejecutarlo dos veces.
        if (interviewFinished)
        {
            return;
        }

        interviewFinished = true;

        Debug.Log("========== ENTREVISTA TERMINADA ==========");

        // Detiene cualquier coroutine:
        // intro, pregunta, espera de respuesta, etc.
        StopAllCoroutines();

        // Detiene el audio del entrevistador.
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        // Si el participante estaba grabando una respuesta,
        // detenemos correctamente el micrófono.
        if (isRecording && !string.IsNullOrEmpty(currentRecordDevice))
        {
            Microphone.End(currentRecordDevice);
            isRecording = false;

            Debug.Log("Grabación detenida porque terminó la entrevista.");
        }

        // Dejamos al entrevistador en una animación de escucha/idle.
        PlaySmoothAnimations(
            GetRandomAnimation(listeningAnimations)
        );

        Debug.Log("La sesión ha finalizado.");
    }   
}
