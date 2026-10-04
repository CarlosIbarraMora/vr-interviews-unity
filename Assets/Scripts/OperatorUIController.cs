using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OperatorUIController : MonoBehaviour
{
    [Header("Interview Controller")]
    [SerializeField]
    private InterviewController interviewController;

    [Header("Text / Input")]
    [SerializeField]
    private TMP_InputField questionInput;

    [SerializeField]
    private TMP_Text selectedQuestionText;

    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private TMP_Text questionsListText;

    [Header("Buttons")]
    [SerializeField]
    private Button askButton;

    [SerializeField]
    private Button finishAnswerButton;

    // Este es el boton que YA tenias como EndInterviewButton.
    [SerializeField]
    private Button interviewControlButton;

    [SerializeField]
    private TMP_Text interviewControlButtonText;

    [SerializeField]
    private Image interviewControlButtonImage;

    [Header("Interview Button Colors")]
    [SerializeField]
    private Color startInterviewColor =
        new Color(0.20f, 0.65f, 0.30f);

    [SerializeField]
    private Color endInterviewColor =
        new Color(0.80f, 0.20f, 0.20f);

    private bool interviewStarted = false;

    // ============================================================
    // UNITY
    // ============================================================

    private void Start()
    {
        RefreshQuestionList();

        if (selectedQuestionText != null)
        {
            selectedQuestionText.text =
                "Seleccionada: Ninguna";
        }

        SetStatus(
            "Introducción en progreso..."
        );

        // No se pueden seleccionar preguntas.
        SetAskControls(false);

        // No hay respuesta que terminar.
        SetFinishAnswerButton(false);

        // El boton existente se prepara para iniciar,
        // pero permanece deshabilitado hasta que termine la intro.
        interviewStarted = false;

        if (interviewControlButtonText != null)
        {
            interviewControlButtonText.text =
                "Iniciar entrevista";
        }

        if (interviewControlButtonImage != null)
        {
            interviewControlButtonImage.color =
                startInterviewColor;
        }

        if (interviewControlButton != null)
        {
            interviewControlButton.interactable =
                false;
        }
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    // ============================================================
    // EVENTOS
    // ============================================================

    private void SubscribeToEvents()
    {
        if (interviewController == null)
        {
            return;
        }

        interviewController.OnIntroductionFinished +=
            HandleIntroductionFinished;

        interviewController.OnInterviewReady +=
            HandleInterviewReady;

        interviewController.OnQuestionStarted +=
            HandleQuestionStarted;

        interviewController.OnAnswerStarted +=
            HandleAnswerStarted;

        interviewController.OnQuestionFinished +=
            HandleQuestionFinished;

        interviewController.OnInterviewFinished +=
            HandleInterviewFinished;
    }

    private void UnsubscribeFromEvents()
    {
        if (interviewController == null)
        {
            return;
        }

        interviewController.OnIntroductionFinished -=
            HandleIntroductionFinished;

        interviewController.OnInterviewReady -=
            HandleInterviewReady;

        interviewController.OnQuestionStarted -=
            HandleQuestionStarted;

        interviewController.OnAnswerStarted -=
            HandleAnswerStarted;

        interviewController.OnQuestionFinished -=
            HandleQuestionFinished;

        interviewController.OnInterviewFinished -=
            HandleInterviewFinished;
    }

    // ============================================================
    // BOTON INICIAR / TERMINAR ENTREVISTA
    // ============================================================

    public void StartOrEndInterview()
    {
        if (interviewController == null)
        {
            SetStatus(
                "ERROR: InterviewController no asignado"
            );
            return;
        }

        // ============================================
        // TODAVIA NO HA INICIADO -> INICIAR
        // ============================================

        if (!interviewStarted)
        {
            bool started =
                interviewController.StartInterview();

            if (!started)
            {
                return;
            }

            // Evitar doble click mientras
            // InterviewController termina de iniciar.
            if (interviewControlButton != null)
            {
                interviewControlButton.interactable =
                    false;
            }

            SetStatus(
                "INICIANDO ENTREVISTA..."
            );

            return;
        }

        // ============================================
        // YA INICIO -> TERMINAR
        // ============================================

        interviewController.EndInterview();
    }

    // ============================================================
    // HACER PREGUNTA
    // ============================================================

    public void SelectAndAskQuestion()
    {
        if (interviewController == null)
        {
            SetStatus(
                "ERROR: InterviewController no asignado"
            );
            return;
        }

        if (questionInput == null)
        {
            SetStatus(
                "ERROR: QuestionInput no asignado"
            );
            return;
        }

        if (
            !int.TryParse(
                questionInput.text,
                out int questionNumber
            )
        )
        {
            SetStatus(
                "INTRODUCE UN NUMERO VALIDO"
            );
            return;
        }

        if (
            !interviewController.SelectQuestion(
                questionNumber
            )
        )
        {
            SetStatus(
                "NO SE PUDO SELECCIONAR LA PREGUNTA"
            );
            return;
        }

        InterviewQuestion selectedQuestion =
            interviewController.GetSelectedQuestion();

        if (
            selectedQuestion != null &&
            selectedQuestionText != null
        )
        {
            selectedQuestionText.text =
                "Seleccionada: "
                + selectedQuestion.id
                + " - "
                + selectedQuestion.shortDescription;
        }

        if (
            interviewController.AskSelectedQuestion()
        )
        {
            questionInput.text = "";
        }
    }

    // ============================================================
    // TERMINAR RESPUESTA
    // ============================================================

    public void FinishAnswer()
    {
        if (interviewController == null)
        {
            return;
        }

        if (!interviewController.IsRecording())
        {
            return;
        }

        interviewController.FinishCurrentAnswer();

        SetFinishAnswerButton(false);

        SetStatus(
            "FINALIZANDO RESPUESTA..."
        );
    }

    // ============================================================
    // INTRODUCCION TERMINADA
    // ============================================================

    private void HandleIntroductionFinished()
    {
        SetStatus(
            "Introducción terminada - listo para iniciar"
        );

        interviewStarted = false;

        if (interviewControlButtonText != null)
        {
            interviewControlButtonText.text =
                "Iniciar entrevista";
        }

        if (interviewControlButtonImage != null)
        {
            interviewControlButtonImage.color =
                startInterviewColor;
        }

        if (interviewControlButton != null)
        {
            interviewControlButton.interactable =
                true;
        }

        SetAskControls(false);
        SetFinishAnswerButton(false);
    }

    // ============================================================
    // ENTREVISTA INICIADA
    // ============================================================

    private void HandleInterviewReady()
    {
        interviewStarted = true;

        SetStatus(
            "Listo para hacer preguntas"
        );

        // El MISMO boton ahora sirve para terminar.
        if (interviewControlButtonText != null)
        {
            interviewControlButtonText.text =
                "Terminar Entrevista";
        }

        if (interviewControlButtonImage != null)
        {
            interviewControlButtonImage.color =
                endInterviewColor;
        }

        if (interviewControlButton != null)
        {
            interviewControlButton.interactable =
                true;
        }

        SetAskControls(true);
        SetFinishAnswerButton(false);

        RefreshQuestionList();
    }

    // ============================================================
    // PREGUNTANDO
    // ============================================================

    private void HandleQuestionStarted()
    {
        SetStatus(
            "PREGUNTANDO..."
        );

        SetAskControls(false);
        SetFinishAnswerButton(false);
    }

    // ============================================================
    // RESPONDIENDO
    // ============================================================

    private void HandleAnswerStarted()
    {
        SetStatus(
            "RESPONDIENDO..."
        );

        SetFinishAnswerButton(true);
    }

    // ============================================================
    // PREGUNTA TERMINADA
    // ============================================================

    private void HandleQuestionFinished()
    {
        RefreshQuestionList();

        SetStatus(
            "Listo para siguiente pregunta"
        );

        SetAskControls(true);
        SetFinishAnswerButton(false);

        if (questionInput != null)
        {
            questionInput.text = "";
        }
    }

    // ============================================================
    // ENTREVISTA TERMINADA
    // ============================================================

    private void HandleInterviewFinished()
    {
        SetStatus(
            "ENTREVISTA TERMINADA"
        );

        SetAskControls(false);
        SetFinishAnswerButton(false);

        if (interviewControlButton != null)
        {
            interviewControlButton.interactable =
                false;
        }

        if (interviewControlButtonText != null)
        {
            interviewControlButtonText.text =
                "ENTREVISTA TERMINADA";
        }
    }

    // ============================================================
    // LISTA DE PREGUNTAS
    // ============================================================

    private void RefreshQuestionList()
    {
        if (
            interviewController == null ||
            questionsListText == null
        )
        {
            return;
        }

        StringBuilder builder =
            new StringBuilder();

        foreach (
            InterviewQuestion question
            in interviewController.questions
        )
        {
            string mark =
                question.alreadyAsked
                    ? "[X]"
                    : "[ ]";

            builder.AppendLine(
                mark
                + " "
                + question.id
                + " - "
                + question.shortDescription
            );
        }

        questionsListText.text =
            builder.ToString();
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private void SetAskControls(
        bool enabled
    )
    {
        if (askButton != null)
        {
            askButton.interactable =
                enabled;
        }

        if (questionInput != null)
        {
            questionInput.interactable =
                enabled;
        }
    }

    private void SetFinishAnswerButton(
        bool enabled
    )
    {
        if (finishAnswerButton != null)
        {
            finishAnswerButton.interactable =
                enabled;
        }
    }

    private void SetStatus(
        string message
    )
    {
        if (statusText != null)
        {
            statusText.text =
                message;
        }

        Debug.Log(
            "[Operator UI] "
            + message
        );
    }
}