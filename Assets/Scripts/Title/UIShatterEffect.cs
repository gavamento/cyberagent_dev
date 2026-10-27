using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// UI 要素を格子状の破片に割って飛び散らせる
public class UIShatterEffect : MonoBehaviour
{
    [SerializeField] private int columns = 4;
    [SerializeField] private int rows = 3;
    [SerializeField] private float duration = 0.7f;
    [SerializeField] private float gravity = 3000f;
    [SerializeField] private Vector2 speedRange = new Vector2(400f, 1100f);
    [SerializeField] private float upwardBoost = 500f;
    [SerializeField] private float maxAngularSpeed = 720f;
    [SerializeField] private RectTransform shakeTarget;
    [SerializeField] private float shakeStrength = 14f;
    [SerializeField] private float shakeDuration = 0.25f;

    private class Piece
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Vector2 velocity;
        public float angularSpeed;
    }

    public void Shatter(RectTransform target, Action onComplete)
    {
        StartCoroutine(ShatterRoutine(target, onComplete));
    }

    IEnumerator ShatterRoutine(RectTransform target, Action onComplete)
    {
        var parent = (RectTransform)target.parent;
        Vector2 size = target.rect.size;
        Vector2 center = parent.InverseTransformPoint(target.TransformPoint(target.rect.center));
        var cell = new Vector2(size.x / columns, size.y / rows);

        var pieces = new List<Piece>();
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                var offset = new Vector2((x + 0.5f) * cell.x - size.x * 0.5f, (y + 0.5f) * cell.y - size.y * 0.5f);
                pieces.Add(CreatePiece(target, parent, center + offset, offset, cell, size));
            }
        }
        target.gameObject.SetActive(false);

        Vector3 shakeBase = shakeTarget != null ? shakeTarget.localPosition : Vector3.zero;
        float time = 0f;
        while (time < duration)
        {
            float dt = Time.unscaledDeltaTime;
            time += dt;

            foreach (var piece in pieces)
            {
                piece.velocity.y -= gravity * dt;
                piece.rect.localPosition += (Vector3)(piece.velocity * dt);
                piece.rect.Rotate(0f, 0f, piece.angularSpeed * dt);
                // 後半でフェードアウト
                piece.group.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 1f, time / duration));
            }

            if (shakeTarget != null)
            {
                float power = Mathf.Clamp01(1f - time / shakeDuration);
                shakeTarget.localPosition = shakeBase + (Vector3)(UnityEngine.Random.insideUnitCircle * shakeStrength * power);
            }
            yield return null;
        }

        if (shakeTarget != null) shakeTarget.localPosition = shakeBase;
        foreach (var piece in pieces) Destroy(piece.rect.gameObject);
        onComplete?.Invoke();
    }

    // マスクで元の見た目の一部だけを切り出した破片を作る
    Piece CreatePiece(RectTransform target, RectTransform parent, Vector2 position, Vector2 offset, Vector2 cell, Vector2 size)
    {
        var go = new GameObject("ShatterPiece", typeof(RectTransform), typeof(Image), typeof(Mask), typeof(CanvasGroup));
        go.layer = target.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.sizeDelta = cell;
        rect.localPosition = position;
        go.GetComponent<Mask>().showMaskGraphic = false;

        var group = go.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var clone = Instantiate(target.gameObject, rect);
        // 破片ごと消えるので無効化だけにする。ButtonSe など Button を必須にする部品があると Destroy できない
        foreach (var selectable in clone.GetComponentsInChildren<Selectable>())
            selectable.enabled = false;
        var cloneRect = (RectTransform)clone.transform;
        cloneRect.anchorMin = cloneRect.anchorMax = cloneRect.pivot = new Vector2(0.5f, 0.5f);
        cloneRect.sizeDelta = size;
        cloneRect.anchoredPosition = -offset;
        cloneRect.localRotation = Quaternion.identity;
        cloneRect.localScale = Vector3.one;

        Vector2 direction = offset.sqrMagnitude > 0.01f ? offset.normalized : Vector2.up;
        direction = (direction + UnityEngine.Random.insideUnitCircle * 0.5f).normalized;
        return new Piece
        {
            rect = rect,
            group = group,
            velocity = direction * UnityEngine.Random.Range(speedRange.x, speedRange.y) + Vector2.up * upwardBoost,
            angularSpeed = UnityEngine.Random.Range(-maxAngularSpeed, maxAngularSpeed),
        };
    }
}
