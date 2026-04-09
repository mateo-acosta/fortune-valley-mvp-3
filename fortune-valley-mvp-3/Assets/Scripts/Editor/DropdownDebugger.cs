using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

public static class DropdownDebugger
{
    [MenuItem("Debug/Check LotDropdown Raycast")]
    public static void CheckDropdown()
    {
        // Find the dropdown
        var allDropdowns = Resources.FindObjectsOfTypeAll<TMP_Dropdown>();
        TMP_Dropdown dropdown = null;
        foreach (var d in allDropdowns)
        {
            if (d.gameObject.name == "LotDropdown")
            {
                dropdown = d;
                break;
            }
        }

        if (dropdown == null)
        {
            Debug.LogError("[DropdownDebug] LotDropdown not found!");
            return;
        }

        var go = dropdown.gameObject;
        Debug.Log($"[DropdownDebug] Found LotDropdown. Active: {go.activeSelf}, ActiveInHierarchy: {go.activeInHierarchy}");
        Debug.Log($"[DropdownDebug] Interactable: {dropdown.interactable}");

        // Check Image raycast target
        var img = go.GetComponent<Image>();
        if (img != null)
            Debug.Log($"[DropdownDebug] Image raycastTarget: {img.raycastTarget}, enabled: {img.enabled}");
        else
            Debug.LogWarning("[DropdownDebug] No Image component on dropdown!");

        // Check CanvasGroups up the hierarchy
        var current = go.transform;
        while (current != null)
        {
            var cg = current.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                Debug.Log($"[DropdownDebug] CanvasGroup on '{current.name}': blocksRaycasts={cg.blocksRaycasts}, interactable={cg.interactable}, alpha={cg.alpha}");
            }

            var mask = current.GetComponent<RectMask2D>();
            if (mask != null)
            {
                Debug.Log($"[DropdownDebug] RectMask2D on '{current.name}'");
            }

            var uiMask = current.GetComponent<Mask>();
            if (uiMask != null)
            {
                Debug.Log($"[DropdownDebug] Mask on '{current.name}': showMaskGraphic={uiMask.showMaskGraphic}");
            }

            current = current.parent;
        }

        // Check if dropdown rect is inside parent rects
        var dropdownRect = go.GetComponent<RectTransform>();
        var dropdownCorners = new Vector3[4];
        dropdownRect.GetWorldCorners(dropdownCorners);
        Debug.Log($"[DropdownDebug] Dropdown world corners: BL={dropdownCorners[0]}, TL={dropdownCorners[1]}, TR={dropdownCorners[2]}, BR={dropdownCorners[3]}");

        // Check all raycast targets that overlap the dropdown position
        var allGraphics = go.transform.root.GetComponentsInChildren<Graphic>(false);
        var dropdownCenter = (dropdownCorners[0] + dropdownCorners[2]) / 2f;
        int blockingCount = 0;

        foreach (var g in allGraphics)
        {
            if (!g.raycastTarget) continue;
            if (g.gameObject == go) continue;
            if (!g.gameObject.activeInHierarchy) continue;

            var gRect = g.GetComponent<RectTransform>();
            var gCorners = new Vector3[4];
            gRect.GetWorldCorners(gCorners);

            // Simple 2D bounds check
            float gMinX = Mathf.Min(gCorners[0].x, gCorners[2].x);
            float gMaxX = Mathf.Max(gCorners[0].x, gCorners[2].x);
            float gMinY = Mathf.Min(gCorners[0].y, gCorners[2].y);
            float gMaxY = Mathf.Max(gCorners[0].y, gCorners[2].y);

            if (dropdownCenter.x >= gMinX && dropdownCenter.x <= gMaxX &&
                dropdownCenter.y >= gMinY && dropdownCenter.y <= gMaxY)
            {
                // Check if this graphic is rendered ON TOP (higher sibling depth)
                int gDepth = g.canvas != null ? g.canvas.sortingOrder : 0;
                Debug.Log($"[DropdownDebug] Overlapping raycast target: '{g.gameObject.name}' at path '{GetPath(g.transform)}', depth={g.depth}, sortingOrder={gDepth}");
                blockingCount++;
            }
        }

        Debug.Log($"[DropdownDebug] Total overlapping raycast targets: {blockingCount}");

        // Check EventSystem
        var es = EventSystem.current;
        if (es == null)
            Debug.LogError("[DropdownDebug] No EventSystem in scene!");
        else
            Debug.Log($"[DropdownDebug] EventSystem exists: {es.gameObject.name}");

        // Check GraphicRaycaster on parent Canvas
        var canvas = go.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            Debug.Log($"[DropdownDebug] Canvas '{canvas.name}': GraphicRaycaster={raycaster != null}, enabled={raycaster?.enabled}");
        }
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
