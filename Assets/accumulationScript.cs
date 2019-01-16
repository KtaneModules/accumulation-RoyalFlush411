using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using accumulation;

public class accumulationScript : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] keypad;
    public KMSelectable clearButton;
    public KMSelectable submitButton;

    public TextMesh screenInput;
    public Material[] colourOptions;
    public Material blackMat;
    public Renderer background;
    public Renderer[] buttons;
    public ButtonValues[] buttonValues;
    public Renderer border;
    private int borderValue = 0;
    public int[] colourValue;
    public Renderer[] stageIndicators;
    public Material[] stageIndicatorOptions;

    public Material[] chosenBackgroundColours;
    public int[] chosenValues;
    private int targetAnswer = 0;
    private string targetAnswerString = "";

    private int baseNumber = 0;
    private int keyPressTotal = 0;
    private List<Material> pressedColours = new List<Material>();

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    int stage = 0;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable number in keypad)
        {
            KMSelectable pressedNumber = number;
            number.OnInteract += delegate () { keypadPress(pressedNumber); return false; };
        }
        clearButton.OnInteract += delegate () { OnClearButton(); return false; };
        submitButton.OnInteract += delegate () { OnSubmitButton(); return false; };
    }

    void Start()
    {
        screenInput.text = "";
        int index = UnityEngine.Random.Range(0,10);
        border.material = colourOptions[index];
        borderValue = colourValue[index];
        baseNumber = Bomb.GetBatteryCount() + Bomb.GetPortPlates().Count() + Bomb.GetIndicators().Count() + borderValue;
        targetAnswer = baseNumber;
        Debug.LogFormat("[Accumulation #{0}] The border colour is {1} (value: {2}).", moduleId, colourOptions[index].name, borderValue);
        Debug.LogFormat("[Accumulation #{0}] The base number is {1}.", moduleId, baseNumber);

        for(int i = 0; i <= 4; i++)
        {
            int index3 = UnityEngine.Random.Range(0,10);
            chosenBackgroundColours[i] = colourOptions[index3];
            chosenValues[i] = colourValue[index3];
        }
        StartCoroutine(FirstFlicker());
        SetBackgroundColour();
    }

    IEnumerator FirstFlicker()
    {
        foreach(Renderer stageLight in stageIndicators)
        {
            stageLight.material = stageIndicatorOptions[0];
        }
        int flicker = 0;
        while(flicker < 7)
        {
            yield return new WaitForSeconds(0.05f);
            stageIndicators[0].material = stageIndicatorOptions[0];
            yield return new WaitForSeconds(0.05f);
            stageIndicators[0].material = stageIndicatorOptions[1];
            flicker++;
        }
        moduleSolved = false;
    }

    void SetBackgroundColour()
    {
        for(int i = 0; i <= 9; i++)
        {
            int index2 = UnityEngine.Random.Range(0,10);
            buttons[i].material = colourOptions[index2];
            buttonValues[i].buttonValue = colourValue[index2];
        }
        background.material = chosenBackgroundColours[stage];
        targetAnswer += (chosenValues[stage] * (stage + 1)) + keyPressTotal;
        targetAnswer = (targetAnswer % 1000);
        targetAnswerString = targetAnswer.ToString();
        Debug.LogFormat("[Accumulation #{0}] The background colour for stage {1} is {2}.", moduleId, stage + 1, chosenBackgroundColours[stage].name);
        Debug.LogFormat("[Accumulation #{0}] The value of {1} is ({2} * {3}) for a total of {4}.", moduleId, chosenBackgroundColours[stage].name, chosenValues[stage], stage + 1, chosenValues[stage] * (stage + 1));
        Debug.LogFormat("[Accumulation #{0}] The target answer for stage {1} is {2}.", moduleId, stage + 1, targetAnswerString);
    }

    void keypadPress(KMSelectable number)
    {
        GetComponent<KMSelectable>().AddInteractionPunch(0.5f);
        Audio.PlaySoundAtTransform("keyStroke", transform);
        if(moduleSolved)
        {
            return;
        }
        if(screenInput.text.Length < 3)
        {
            screenInput.text += number.GetComponentInChildren<TextMesh>().text;
            keyPressTotal += number.GetComponentInChildren<ButtonValues>().buttonValue;
            pressedColours.Add(number.GetComponent<Renderer>().material);
        }
    }

    void OnClearButton()
    {
        GetComponent<KMSelectable>().AddInteractionPunch();
        Audio.PlaySoundAtTransform("keyStroke", transform);
        if(moduleSolved)
        {
            return;
        }
        screenInput.text = "";
        keyPressTotal = 0;
        pressedColours.Clear();
    }

    void OnSubmitButton()
    {
        GetComponent<KMSelectable>().AddInteractionPunch();
        Audio.PlaySoundAtTransform("keyStroke", transform);
        if(moduleSolved)
        {
            return;
        }
        if(screenInput.text == targetAnswerString)
        {
            Debug.LogFormat("[Accumulation #{0}] You typed {1}. That is correct.", moduleId, screenInput.text);
            Audio.PlaySoundAtTransform("correct", transform);
            stage++;
            if(stage == 5)
            {
                stageIndicators[stage-1].material = stageIndicatorOptions[2];
                Debug.LogFormat("[Accumulation #{0}] Module disarmed.", moduleId);
                GetComponent<KMBombModule>().HandlePass();
                moduleSolved = true;
                background.material = colourOptions[8];
                border.material = blackMat;
                foreach(KMSelectable keypad in keypad)
                {
                    keypad.GetComponent<Renderer>().material = colourOptions[8];
                    keypad.GetComponentInChildren<TextMesh>().text = "";
                }
                submitButton.GetComponent<Renderer>().material = colourOptions[8];
                submitButton.GetComponentInChildren<TextMesh>().text = "";
                clearButton.GetComponent<Renderer>().material = colourOptions[8];
                clearButton.GetComponentInChildren<TextMesh>().text = "";
            }
            else
            {
                Debug.LogFormat("[Accumulation #{0}] The colours pressed at stage {1} were {2}.", moduleId, stage, String.Join(", ",pressedColours.Select((x) => x.name.Replace("(Instance)", "")).ToArray()));
                Debug.LogFormat("[Accumulation #{0}] The key press total at stage {1} is {2}.", moduleId, stage, keyPressTotal);
                StartCoroutine(StageFlicker());
                SetBackgroundColour();
            }
        }
        else
        {
            stage = 0;
            Debug.LogFormat("[Accumulation #{0}] Strike! You typed {1}. That is incorrect.", moduleId, screenInput.text);
            GetComponent<KMBombModule>().HandleStrike();
            StartCoroutine(StrikeFlicker());
        }
        screenInput.text = "";
        keyPressTotal = 0;
        pressedColours.Clear();
    }

    IEnumerator StageFlicker()
    {
        moduleSolved = true;
        stageIndicators[stage-1].material = stageIndicatorOptions[2];
        int flicker = 0;
        while(flicker < 7)
        {
            yield return new WaitForSeconds(0.05f);
            stageIndicators[stage].material = stageIndicatorOptions[0];
            yield return new WaitForSeconds(0.05f);
            stageIndicators[stage].material = stageIndicatorOptions[1];
            flicker++;
        }
        moduleSolved = false;
    }

    IEnumerator StrikeFlicker()
    {
        moduleSolved = true;
        int flicker = 0;
        while(flicker < 15)
        {
            yield return new WaitForSeconds(0.05f);
            stageIndicators[0].material = stageIndicatorOptions[3];
            stageIndicators[1].material = stageIndicatorOptions[3];
            stageIndicators[2].material = stageIndicatorOptions[3];
            stageIndicators[3].material = stageIndicatorOptions[3];
            stageIndicators[4].material = stageIndicatorOptions[3];
            yield return new WaitForSeconds(0.05f);
            stageIndicators[0].material = stageIndicatorOptions[0];
            stageIndicators[1].material = stageIndicatorOptions[0];
            stageIndicators[2].material = stageIndicatorOptions[0];
            stageIndicators[3].material = stageIndicatorOptions[0];
            stageIndicators[4].material = stageIndicatorOptions[0];
            flicker++;
        }
        Start();
    }
}
