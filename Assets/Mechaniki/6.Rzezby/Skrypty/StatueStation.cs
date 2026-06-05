using System;
using System.Collections;
using UnityEngine;

public class StatueStation : MonoBehaviour
{
    [Serializable]
    public class HandTargetRotation
    {
        public Transform hand;
        public Vector3 rotationOffset;
        public bool rotateInLocalSpace = true;
    }

    [Header("Obrot calej rzezby")]
    public Transform statueRoot;
    public Vector3 statueRotationOffset = new Vector3(0f, 90f, 0f);
    public bool rotateStatueInLocalSpace = true;
    public float turnDuration = 1.1f;
    public AnimationCurve turnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Finalny obrot rak")]
    public HandTargetRotation[] hands;
    public float handRotationDuration = 0.8f;
    public AnimationCurve handCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private StatuePuzzleController controller;
    private Coroutine turnRoutine;
    private Coroutine handRoutine;

    public bool IsPressed { get; private set; }
    public bool IsTurnedToCenter { get; private set; }

    private Transform Root => statueRoot ? statueRoot : transform;

    private void Reset()
    {
        statueRoot = transform;
    }

    public void SetController(StatuePuzzleController newController)
    {
        controller = newController;
    }

    public void PressButton()
    {
        if (IsPressed)
            return;

        IsPressed = true;

        if (turnRoutine != null)
            StopCoroutine(turnRoutine);

        turnRoutine = StartCoroutine(TurnToCenterRoutine());
    }

    public void RotateHandsForFinale()
    {
        if (handRoutine != null)
            StopCoroutine(handRoutine);

        handRoutine = StartCoroutine(RotateHandsRoutine());
    }

    private IEnumerator TurnToCenterRoutine()
    {
        Transform root = Root;
        Quaternion startRotation = rotateStatueInLocalSpace ? root.localRotation : root.rotation;
        Quaternion targetRotation = GetTargetStatueRotation(startRotation);

        if (turnDuration <= 0f)
        {
            ApplyStatueRotation(root, targetRotation);
        }
        else
        {
            float timer = 0f;

            while (timer < turnDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / turnDuration);
                float smoothT = turnCurve.Evaluate(t);
                ApplyStatueRotation(root, Quaternion.Slerp(startRotation, targetRotation, smoothT));
                yield return null;
            }

            ApplyStatueRotation(root, targetRotation);
        }

        IsTurnedToCenter = true;
        controller?.StatueTurnedToCenter(this);
    }

    private Quaternion GetTargetStatueRotation(Quaternion startRotation)
    {
        Quaternion offsetRotation = Quaternion.Euler(statueRotationOffset);

        if (rotateStatueInLocalSpace)
            return startRotation * offsetRotation;

        return offsetRotation * startRotation;
    }

    private void ApplyStatueRotation(Transform root, Quaternion rotation)
    {
        if (rotateStatueInLocalSpace)
            root.localRotation = rotation;
        else
            root.rotation = rotation;
    }

    private IEnumerator RotateHandsRoutine()
    {
        if (hands == null || hands.Length == 0)
            yield break;

        Quaternion[] startRotations = new Quaternion[hands.Length];
        Quaternion[] targetRotations = new Quaternion[hands.Length];

        for (int i = 0; i < hands.Length; i++)
        {
            if (hands[i] == null || !hands[i].hand)
                continue;

            startRotations[i] = hands[i].hand.localRotation;
            targetRotations[i] = GetTargetHandRotation(hands[i], startRotations[i]);
        }

        if (handRotationDuration <= 0f)
        {
            ApplyHandRotations(targetRotations);
            yield break;
        }

        float timer = 0f;

        while (timer < handRotationDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / handRotationDuration);
            float smoothT = handCurve.Evaluate(t);

            for (int i = 0; i < hands.Length; i++)
            {
                if (hands[i] == null || !hands[i].hand)
                    continue;

                hands[i].hand.localRotation = Quaternion.Slerp(startRotations[i], targetRotations[i], smoothT);
            }

            yield return null;
        }

        ApplyHandRotations(targetRotations);
    }

    private void ApplyHandRotations(Quaternion[] rotations)
    {
        for (int i = 0; i < hands.Length; i++)
        {
            if (hands[i] == null || !hands[i].hand)
                continue;

            hands[i].hand.localRotation = rotations[i];
        }
    }

    private Quaternion GetTargetHandRotation(HandTargetRotation handTarget, Quaternion startRotation)
    {
        Quaternion offsetRotation = Quaternion.Euler(handTarget.rotationOffset);

        if (handTarget.rotateInLocalSpace)
            return startRotation * offsetRotation;

        return offsetRotation * startRotation;
    }
}
