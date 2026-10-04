using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InterviewController : MonoBehaviour
{
    private Animator animator;
    private AudioSource audioSource;
    private AudioSource recordSource;

    private int cont;

    private readonly string[] talkingAnimations =
    {
        "talking1",
        "talking2",
        "talking3",
        "talking4",
        "talking5"
    };

    private readonly string[] listeningAnimations =
    {
        "listeningIdle1",
        "listeningIdle2",
        "listeningIdle3",
        "listeningIdle4",
        "listeningIdle5"
    };

    private Transform interviewerTransform;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    // Estado general de la entrevista
    private bool introductionFinished = false;
    private bool interviewReady = false;
    private bool interviewFinished = false;
    private bool startInterviewRequested = false;
    private bool questionInProgress = false;

    // Estado de grabacion
    private bool isRecording = false;
    private bool finishAnswerRequested = false;
    private string currentRecordDevice;

    private int maxWait = 20;

    [Header("Wizard of Oz - Preguntas")]
    [Tooltip("Agrega aqui cada pregunta con su texto corto, transcripcion y AudioClip.")]
    public List<InterviewQuestion> questions = new List<InterviewQuestion>();

    private int selectedQuestionIndex = 0;

    // ============================================================
    // EVENTOS PARA LA UI
    // ============================================================

    public event Action OnIntroductionFinished;
    public event Action OnInterviewReady;
    public event Action OnQuestionStarted;
    public event Action OnAnswerStarted;
    public event Action OnQuestionFinished;
    public event Action OnInterviewFinished;

    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        GameObject interviewer =
            GameObject.FindGameObjectWithTag("Interviewer");

        if (interviewer == null)
        {
            Debug.LogError(
                "No se encontro un GameObject con el tag 'Interviewer'."
            );
            return;
        }

        interviewerTransform = interviewer.transform;
        animator = interviewer.GetComponent<Animator>();
        audioSource = interviewer.GetComponent<AudioSource>();
        recordSource = interviewer.GetComponent<AudioSource>();

        if (animator == null)
        {
            Debug.LogWarning(
                "El entrevistador no tiene Animator."
            );
        }

        if (audioSource == null)
        {
            Debug.LogError(
                "El entrevistador no tiene AudioSource."
            );
            return;
        }

        cont = 1;

        originalPosition =
            interviewerTransform.position;

        originalRotation =
            interviewerTransform.rotation;

        // Introduccion inicial
        Interview introInterview =
            new Interview(
                "1 To 1",
                "personal"
            );

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

        Debug.Log(
            "Iniciando introduccion..."
        );

        StartCoroutine(
            RunIntroduction(introInterview)
        );
    }

    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        // ESC se conserva como respaldo durante desarrollo.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EndInterview();
        }
    }

    // ============================================================
    // INICIAR ENTREVISTA
    // ============================================================

    public bool StartInterview()
    {
        if (interviewFinished)
        {
            Debug.LogWarning(
                "La entrevista ya termino."
            );
            return false;
        }

        if (!introductionFinished)
        {
            Debug.LogWarning(
                "La introduccion todavia no ha terminado."
            );
            return false;
        }

        if (interviewReady)
        {
            Debug.LogWarning(
                "La entrevista ya fue iniciada."
            );
            return false;
        }

        startInterviewRequested = true;

        Debug.Log(
            "El operador solicito iniciar la entrevista."
        );

        return true;
    }

    // ============================================================
    // SELECCIONAR PREGUNTA
    // ============================================================

    public bool SelectQuestion(int questionNumber)
    {
        if (interviewFinished)
        {
            Debug.LogWarning(
                "La entrevista ya termino."
            );
            return false;
        }

        if (!interviewReady)
        {
            Debug.LogWarning(
                "La entrevista todavia no esta lista."
            );
            return false;
        }

        if (questionInProgress)
        {
            Debug.LogWarning(
                "Hay una pregunta en progreso."
            );
            return false;
        }

        if (questions.Count == 0)
        {
            Debug.LogWarning(
                "No hay preguntas configuradas."
            );
            return false;
        }

        int index = questionNumber - 1;

        if (index < 0 || index >= questions.Count)
        {
            Debug.LogWarning(
                "Pregunta invalida. Selecciona un numero entre 1 y "
                + questions.Count
                + "."
            );

            return false;
        }

        selectedQuestionIndex = index;

        PrintSelectedQuestion();

        return true;
    }

    // ============================================================
    // REALIZAR PREGUNTA
    // ============================================================

    public bool AskSelectedQuestion()
    {
        if (interviewFinished)
        {
            Debug.LogWarning(
                "La entrevista ya termino."
            );
            return false;
        }

        if (!interviewReady)
        {
            Debug.LogWarning(
                "La entrevista todavia no esta lista."
            );
            return false;
        }

        if (questionInProgress)
        {
            Debug.LogWarning(
                "Ya hay una pregunta en progreso."
            );
            return false;
        }

        if (questions.Count == 0)
        {
            Debug.LogWarning(
                "No hay preguntas configuradas."
            );
            return false;
        }

        InterviewQuestion selectedQuestion =
            questions[selectedQuestionIndex];

        if (selectedQuestion.audioClip == null)
        {
            Debug.LogWarning(
                "La pregunta "
                + selectedQuestion.id
                + " no tiene AudioClip asignado."
            );

            return false;
        }

        StartCoroutine(
            AskQuestion(selectedQuestion)
        );

        return true;
    }

    // ============================================================
    // TERMINAR RESPUESTA
    // ============================================================

    public void FinishCurrentAnswer()
    {
        if (!questionInProgress)
        {
            Debug.LogWarning(
                "No hay una pregunta en progreso."
            );
            return;
        }

        if (!isRecording)
        {
            Debug.LogWarning(
                "Todavia no se esta grabando una respuesta."
            );
            return;
        }

        finishAnswerRequested = true;

        Debug.Log(
            "El operador solicito terminar la respuesta."
        );
    }

    // ============================================================
    // GETTERS PARA LA UI
    // ============================================================

    public InterviewQuestion GetSelectedQuestion()
    {
        if (questions.Count == 0)
        {
            return null;
        }

        return questions[selectedQuestionIndex];
    }

    public int GetQuestionCount()
    {
        return questions.Count;
    }

    public bool IsQuestionInProgress()
    {
        return questionInProgress;
    }

    public bool IsInterviewReady()
    {
        return interviewReady && !interviewFinished;
    }

    public bool IsIntroductionFinished()
    {
        return introductionFinished;
    }

    public bool IsRecording()
    {
        return isRecording;
    }

    // ============================================================
    // INTRODUCCION
    // ============================================================

    private IEnumerator RunIntroduction(
        Interview interview
    )
    {
        List<InterviewStep> steps =
            interview.getSteps();

        foreach (InterviewStep step in steps)
        {
            if (interviewFinished)
            {
                yield break;
            }

            AudioClip clip =
                step.getAudioClip();

            if (clip != null)
            {
                PlaySmoothAnimations(
                    GetRandomAnimation(
                        talkingAnimations
                    )
                );

                FixInterviewerTransform();

                audioSource.clip = clip;
                audioSource.Play();

                yield return new WaitForSeconds(
                    clip.length
                );

                if (interviewFinished)
                {
                    yield break;
                }

                audioSource.Stop();
                audioSource.clip = null;
            }
            else
            {
                PlaySmoothAnimations(
                    GetRandomAnimation(
                        listeningAnimations
                    )
                );

                FixInterviewerTransform();

                introductionFinished = true;

                Debug.Log(
                    "Introduccion terminada. "
                    + "Esperando al operador."
                );

                // La UI ahora habilita el mismo boton
                // como INICIAR ENTREVISTA.
                OnIntroductionFinished?.Invoke();

                // Ya NO esperamos Enter ni Space.
                while (!startInterviewRequested)
                {
                    if (interviewFinished)
                    {
                        yield break;
                    }

                    yield return null;
                }
            }
        }

        if (interviewFinished)
        {
            yield break;
        }

        interviewReady = true;

        Debug.Log(
            "========================================"
        );

        Debug.Log(
            "ENTREVISTA INICIADA"
        );

        Debug.Log(
            "========================================"
        );

        PrintQuestionList();

        OnInterviewReady?.Invoke();
    }

    // ============================================================
    // FLUJO DE PREGUNTA
    // ============================================================

    private IEnumerator AskQuestion(
        InterviewQuestion question
    )
    {
        questionInProgress = true;
        finishAnswerRequested = false;

        // Estado UI -> PREGUNTANDO
        OnQuestionStarted?.Invoke();

        Debug.Log(
            "----------------------------------------"
        );

        Debug.Log(
            "PREGUNTA "
            + question.id
            + ": "
            + question.shortDescription
        );

        if (
            !string.IsNullOrWhiteSpace(
                question.fullQuestion
            )
        )
        {
            Debug.Log(
                "Texto completo: "
                + question.fullQuestion
            );
        }

        // ========================================================
        // ENTREVISTADORA HABLANDO
        // ========================================================

        PlaySmoothAnimations(
            GetRandomAnimation(
                talkingAnimations
            )
        );

        FixInterviewerTransform();

        audioSource.clip =
            question.audioClip;

        audioSource.Play();

        yield return new WaitForSeconds(
            question.audioClip.length
        );

        if (interviewFinished)
        {
            yield break;
        }

        audioSource.Stop();
        audioSource.clip = null;

        // ========================================================
        // ENTREVISTADORA ESCUCHANDO
        // ========================================================

        PlaySmoothAnimations(
            GetRandomAnimation(
                listeningAnimations
            )
        );

        FixInterviewerTransform();

        // ========================================================
        // GRABACION
        // ========================================================

        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning(
                "No se encontro ningun microfono. "
                + "No se grabara la respuesta."
            );
        }
        else
        {
            currentRecordDevice =
                Microphone.devices[0];

            recordSource.clip =
                Microphone.Start(
                    currentRecordDevice,
                    true,
                    maxWait + 1,
                    44100
                );

            isRecording = true;

            // Estado UI -> RESPONDIENDO
            OnAnswerStarted?.Invoke();

            Debug.Log(
                "Grabando respuesta..."
            );

            float elapsedTime = 0f;

            while (
                !finishAnswerRequested &&
                !Input.GetKeyDown(KeyCode.Space) &&
                elapsedTime < maxWait
            )
            {
                if (interviewFinished)
                {
                    yield break;
                }

                elapsedTime +=
                    Time.deltaTime;

                yield return null;
            }

            if (
                Microphone.IsRecording(
                    currentRecordDevice
                )
            )
            {
                Microphone.End(
                    currentRecordDevice
                );
            }

            isRecording = false;

            if (recordSource.clip != null)
            {
                SavWav.Save(
                    "Record_Answer_Q"
                    + question.id
                    + "_"
                    + cont,
                    recordSource.clip
                );

                cont++;
            }
        }

        // ========================================================
        // PREGUNTA TERMINADA
        // ========================================================

        question.alreadyAsked = true;

        questionInProgress = false;
        finishAnswerRequested = false;

        Debug.Log(
            "Respuesta terminada. "
            + "Lista para la siguiente pregunta."
        );

        PrintQuestionList();

        // Actualiza lista [ ] -> [X]
        OnQuestionFinished?.Invoke();
    }

    // ============================================================
    // LISTA
    // ============================================================

    private void PrintQuestionList()
    {
        Debug.Log(
            "========== LISTA DE PREGUNTAS =========="
        );

        foreach (
            InterviewQuestion question
            in questions
        )
        {
            string usedMark =
                question.alreadyAsked
                    ? "[X]"
                    : "[ ]";

            Debug.Log(
                usedMark
                + " "
                + question.id
                + " - "
                + question.shortDescription
            );
        }

        Debug.Log(
            "========================================="
        );
    }

    private void PrintSelectedQuestion()
    {
        if (questions.Count == 0)
        {
            return;
        }

        InterviewQuestion question =
            questions[selectedQuestionIndex];

        Debug.Log(
            "> SELECTED: "
            + question.id
            + " - "
            + question.shortDescription
        );
    }

    // ============================================================
    // ANIMACIONES
    // ============================================================

    private void FixInterviewerTransform()
    {
        if (interviewerTransform == null)
        {
            return;
        }

        interviewerTransform.position =
            originalPosition
            + new Vector3(
                0,
                0.15f,
                0
            );

        interviewerTransform.rotation =
            originalRotation
            * Quaternion.Euler(
                0,
                90,
                0
            );
    }

    private string GetRandomAnimation(
        string[] animations
    )
    {
        return animations[
            UnityEngine.Random.Range(
                0,
                animations.Length
            )
        ];
    }

    private void PlaySmoothAnimations(
        string animationName
    )
    {
        if (animator == null)
        {
            return;
        }

        int layerIndex = 0;

        int stateHash =
            Animator.StringToHash(
                animationName
            );

        if (
            animator.HasState(
                layerIndex,
                stateHash
            )
        )
        {
            animator.CrossFade(
                stateHash,
                0.35f,
                layerIndex
            );
        }
        else
        {
            Debug.LogWarning(
                "No existe la animacion: "
                + animationName
            );
        }
    }

    // ============================================================
    // TERMINAR ENTREVISTA
    // ============================================================

    public void EndInterview()
    {
        if (interviewFinished)
        {
            return;
        }

        interviewFinished = true;
        interviewReady = false;
        questionInProgress = false;
        finishAnswerRequested = true;

        Debug.Log(
            "========== ENTREVISTA TERMINADA =========="
        );

        StopAllCoroutines();

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        if (
            isRecording &&
            !string.IsNullOrEmpty(
                currentRecordDevice
            )
        )
        {
            if (
                Microphone.IsRecording(
                    currentRecordDevice
                )
            )
            {
                Microphone.End(
                    currentRecordDevice
                );
            }

            isRecording = false;

            Debug.Log(
                "Grabacion detenida porque termino la entrevista."
            );
        }

        if (animator != null)
        {
            PlaySmoothAnimations(
                GetRandomAnimation(
                    listeningAnimations
                )
            );
        }

        OnInterviewFinished?.Invoke();

        Debug.Log(
            "La sesion ha finalizado."
        );
    }
}