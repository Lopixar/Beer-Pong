using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class TablePlacer : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public Camera arCamera;
    public GameObject tablePrefab;
    public BallThrower ballThrower;
    public GameObject undoButton;
    public TextMeshProUGUI infoText;

    GameObject currentTable;
    List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        undoButton.GetComponentInChildren<TextMeshProUGUI>().text = GameTexts.UndoButton;
        undoButton.SetActive(false);
    }

    void Update()
    {
        if (currentTable != null)
            return;

        infoText.text = GameTexts.TapToPlaceTable;

        if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame)
            return;

        Vector2 tapPosition = Pointer.current.position.ReadValue();

        if (raycastManager.Raycast(tapPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            PlaceTable(hits[0].pose.position);
        }
    }

    void PlaceTable(Vector3 position)
    {
        // rotate it so the short side is facing us
        Vector3 direction = position - arCamera.transform.position;
        direction.y = 0;
        Quaternion rotation = Quaternion.LookRotation(direction);

        currentTable = Instantiate(tablePrefab, position, rotation);

        // the pivot of the table is at the legs, so move it down
        // until the top of the table is level with the selected surface
        Transform tableTop = currentTable.transform.Find("TableTop");
        if (tableTop != null)
        {
            // TableTop is a unit cube, so half of its scale is half of its height
            float topY = tableTop.position.y + tableTop.lossyScale.y * 0.5f;
            currentTable.transform.position += Vector3.down * (topY - position.y);
        }

        currentTable.AddComponent<ARAnchor>(); // so it stays in place

        SetPlanesActive(false);
        undoButton.SetActive(true);
        infoText.text = "";

        ballThrower.StartGame(currentTable);
    }

    // undo button
    public void UndoTable()
    {
        ballThrower.StopGame();

        Destroy(currentTable);
        currentTable = null;

        SetPlanesActive(true);
        undoButton.SetActive(false);
    }

    void SetPlanesActive(bool active)
    {
        planeManager.enabled = active;

        foreach (ARPlane plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(active);
        }
    }
}