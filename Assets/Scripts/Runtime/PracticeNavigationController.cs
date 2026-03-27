using UnityEngine;
using UnityEngine.UI;

public sealed class PracticeNavigationController : MonoBehaviour
{
    private GameObject homeContent;
    private GameObject tunerPanel;
    private GameObject metronomePanel;

    private void Awake()
    {
        homeContent = FindRequiredChild("HomeContent").gameObject;
        tunerPanel = FindPanelSibling("TunerPanel");
        metronomePanel = FindPanelSibling("MetronomePanel");

        FindRequiredButton("HomeContent/TunerEntryButton").onClick.AddListener(ShowTuner);
        FindRequiredButton("HomeContent/MetronomeEntryButton").onClick.AddListener(ShowMetronome);

        ShowHome();
    }

    public void ShowHome()
    {
        homeContent.SetActive(true);
        tunerPanel.SetActive(false);
        metronomePanel.SetActive(false);
    }

    public void ShowTuner()
    {
        homeContent.SetActive(false);
        tunerPanel.SetActive(true);
        metronomePanel.SetActive(false);
    }

    public void ShowMetronome()
    {
        homeContent.SetActive(false);
        tunerPanel.SetActive(false);
        metronomePanel.SetActive(true);
    }

    private GameObject FindPanelSibling(string panelName)
    {
        Transform panel = transform.parent != null ? transform.parent.Find(panelName) : null;
        if (panel == null)
        {
            throw new MissingReferenceException($"Cannot find panel '{panelName}' next to MainUI.");
        }

        return panel.gameObject;
    }

    private Button FindRequiredButton(string path)
    {
        Transform target = FindRequiredChild(path);
        Button button = target.GetComponent<Button>();
        if (button == null)
        {
            throw new MissingComponentException($"Missing Button on '{path}'.");
        }

        return button;
    }

    private Transform FindRequiredChild(string path)
    {
        Transform target = transform.Find(path);
        if (target == null)
        {
            throw new MissingReferenceException($"Cannot find child '{path}' under MainUI.");
        }

        return target;
    }
}
