using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class PolygonalMazeScript : MonoBehaviour
{
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Arrows;
    public TextMesh[] Numbers;

    private int CurrentShape = 3;

    private enum ShapeIxs
    {
        Square,
        Triangle,
        Hexagon,
        Octagon,
        Diamond,
        Rhombus,
        Kite,
        Cairo
    }
    private static readonly string[] Geometries = new string[] { "Square", "Triangle", "Hexagon", "Octagon", "Rhombus", "Kite", "Cairo" };
    private struct Arrow
    {
        /// <summary>
        /// Position of the arrow.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// Rotation of the arrow.
        /// </summary>
        public Vector3 Rotation;
        public Arrow(Vector3 position, Vector3 rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
    private static readonly Dictionary<int, Arrow[]> ArrowPositions = new Dictionary<int, Arrow[]>
    {
        { (int)ShapeIxs.Square, new Arrow[]{ new Arrow(new Vector3(0, 0.0154f, 0.05f), new Vector3(90, 0, 0)), new Arrow(new Vector3(0.05f, 0.0154f, 0), new Vector3(90, 90, 0)), new Arrow(new Vector3(0, 0.0154f, -0.05f), new Vector3(90, 180, 0)), new Arrow(new Vector3(-0.05f, 0.0154f, 0), new Vector3(90, 270, 0)) } },
        { (int)ShapeIxs.Octagon, new Arrow[]{ new Arrow(new Vector3(0, 0.0154f, 0.05f), new Vector3(90, 0, 0)), new Arrow(new Vector3(1 / Mathf.Sqrt(2) * 0.05f, 0.0154f, 1 / Mathf.Sqrt(2) * 0.05f), new Vector3(90, 45, 0)), new Arrow(new Vector3(0.05f, 0.0154f, 0), new Vector3(90, 90, 0)), new Arrow(new Vector3(1 / Mathf.Sqrt(2) * 0.05f, 0.0154f, -1 / Mathf.Sqrt(2) * 0.05f), new Vector3(90, 135, 0)), new Arrow(new Vector3(0, 0.0154f, -0.05f), new Vector3(90, 180, 0)), new Arrow(new Vector3(-1 / Mathf.Sqrt(2) * 0.05f, 0.0154f, -1 / Mathf.Sqrt(2) * 0.05f), new Vector3(90, 225, 0)), new Arrow(new Vector3(-0.05f, 0.0154f, 0), new Vector3(90, 270, 0)), new Arrow(new Vector3(-1 / Mathf.Sqrt(2) * 0.05f, 0.0154f, 1 / Mathf.Sqrt(2) * 0.05f), new Vector3(90, 315, 0)) } }
    };

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        var contents = Module.transform.Find("Contents").transform;
        PlaceArrows();
        for (int i = 0; i < Arrows.Length; i++)
        {
            int x = i;
            Arrows[x].OnInteract += delegate { ArrowPress(x); return false; };
        }
        for (int i = 0; i < Numbers.Length; i++)
            StartCoroutine(NumbersAnim(i, i / 2f));
        contents.localScale = Vector3.zero;
        Module.OnActivate += delegate { contents.localScale = Vector3.one; };
    }

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void ArrowPress(int pos)
    {
        Audio.PlaySoundAtTransform("move", Arrows[pos].transform);
    }

    void PlaceArrows()
    {
        for (int i = 0; i < Arrows.Length; i++)
        {
            if (ArrowPositions[CurrentShape].Length > i)
            {
                Arrows[i].transform.localScale = new Vector3(0.5f, 0.5f, 0.005f);
                Arrows[i].transform.localPosition = ArrowPositions[CurrentShape][i].Position;
                Arrows[i].transform.localEulerAngles = ArrowPositions[CurrentShape][i].Rotation;
            }
            else
                Arrows[i].transform.localScale = new Vector3();
        }
    }

    private IEnumerator NumbersAnim(int ix, float offset, float duration = 2f, float intensity = 0.0003f, float adjustX = 0.02f, float adjustZ = 0.02f, float scaleX = 2f, float scaleZ = 1f)
    {
        offset *= duration;
        while (true)
        {
            float timer = 0;
            while (timer < duration)
            {
                yield return null;
                timer += Time.deltaTime;
                float x = Mathf.Rad2Deg * Mathf.Cos(((timer + offset) % duration) / duration * 2 * Mathf.PI) * intensity;
                float z = Mathf.Rad2Deg * Mathf.Sin(((timer + offset) % duration) / duration * 2 * Mathf.PI) * intensity;
                Numbers[ix].transform.localPosition = new Vector3((x * scaleX) + new float[] { -adjustX, adjustX }[ix], Numbers[ix].transform.localPosition.y, (z * scaleZ) + new float[] { adjustZ, -adjustZ }[ix]);
            }
        }
    }
}
