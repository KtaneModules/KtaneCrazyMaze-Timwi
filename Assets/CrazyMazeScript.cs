using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CrazyMaze;
using UnityEngine;

using Rnd = UnityEngine.Random;

public class CrazyMazeScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Arrows;
    public KMSelectable Bridge;
    public TextMesh CurCellText;
    public TextMesh GoalCellText;
    public SpriteRenderer CurrentCell;
    public Sprite[] Sprites;
    public KMRuleSeedable RuleSeedable;

    private static int _moduleIdCounter = 1;
    private int _moduleID = 0;
    private bool _moduleSolved = false;

    private int _currentCell;
    private int _goalCell;
    private bool _showingGoal;

    private HashSet<int>[] _passable;
    private string[] _cellLetters;

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        for (int i = 0; i < Arrows.Length; i++)
            Arrows[i].OnInteract += ArrowPress(i, Arrows[i], "move");
        Bridge.OnInteract += ArrowPress(-1, Bridge, "bridge");
        _passable = Enumerable.Range(0, 26 * 26).Select(ix => new HashSet<int>()).ToArray();
    }

    private KMSelectable.OnInteractHandler ArrowPress(int pos, KMSelectable sel, string sound)
    {
        var isBridge = pos == -1;
        var upper = isBridge ? "Traversing bridge" : "Going";
        var lower = isBridge ? "traverse bridge" : "go";

        return delegate
        {
            Audio.PlaySoundAtTransform(sound, sel.transform);
            sel.AddInteractionPunch(.3f);
            if (_moduleSolved)
                return false;

            var goingTo = isBridge
                ? CellTransitions.All[_currentCell].BridgeDestination
                : CellTransitions.All[_currentCell].Neighbors[pos].ToCell;

            if (goingTo != null && _passable[_currentCell].Contains(goingTo.Value))
            {
                Debug.LogFormat(@"[Crazy Maze #{0}] {3} from {1} to {2}.", _moduleID, _cellLetters[_currentCell], _cellLetters[goingTo.Value], upper);
                if (isBridge)
                    _showingGoal = !_showingGoal;
                SetCell(goingTo.Value);
                if (goingTo.Value == _goalCell)
                {
                    Debug.LogFormat(@"[Crazy Maze #{0}] Module solved.", _moduleID);
                    Module.HandlePass();
                    _moduleSolved = true;
                }
            }
            else if (goingTo != null)
            {
                Debug.LogFormat(@"[Crazy Maze #{0}] Attempt to {3} from {1} to {2}. Strike.", _moduleID, _cellLetters[_currentCell], _cellLetters[goingTo.Value], lower);
                Module.HandleStrike();
            }
            else
            {
                Debug.LogFormat(@"[Crazy Maze #{0}] Attempt to cross a non-existent bridge from {1}. Strike.", _moduleID, _cellLetters[_currentCell]);
                Module.HandleStrike();
            }

            return false;
        };
    }

    void Start()
    {
        // ## RULE SEED
        var rnd = RuleSeedable.GetRNG();
        var cells = Enumerable.Range(0, 676).ToList();
        var links = Enumerable.Range(0, 676)
            .Select(cellIx => CellTransitions.All[cellIx])
            .Select(tr => tr.BridgeDestination == null ? tr.Neighbors.Select(n => n.ToCell) : tr.Neighbors.Select(n => n.ToCell).Concat(new[] { tr.BridgeDestination.Value }))
            .Select(cs => cs.OrderBy(c => c).ToArray())
            .ToArray();

        // Find a random starting cell
        var startCellIx = rnd.Next(0, cells.Count);
        var startCell = cells[startCellIx];

        // Maze algorithm starts here
        var todo = new List<int> { startCell };
        cells.RemoveAt(startCellIx);

        while (cells.Count > 0)
        {
            var ix = rnd.Next(0, todo.Count);
            var cell = todo[ix];

            var availableLinks = links[cell].Where(otherCell => cells.Contains(otherCell)).ToArray();
            if (availableLinks.Length == 0)
                todo.RemoveAt(ix);
            else
            {
                var otherCell = availableLinks[availableLinks.Length == 1 ? 0 : rnd.Next(0, availableLinks.Length)];
                _passable[cell].Add(otherCell);
                _passable[otherCell].Add(cell);
                cells.Remove(otherCell);
                todo.Add(otherCell);
            }
        }

        var letters = Enumerable.Range(0, 26).Select(c => (char) ('A' + c));
        _cellLetters = rnd.ShuffleFisherYates(letters.SelectMany(ltr => letters.Select(ltr2 => ltr + "" + ltr2)).ToArray());
        // End rule seed

        // Decide on a start cell
        SetCell(Rnd.Range(0, 26 * 26));

        // Decide on a goal cell that is a certain distance away
        const int minAllowedDistance = 12;
        const int maxAllowedDistance = 17;
        var visited = new HashSet<int> { _currentCell };
        var chooseFrom = new HashSet<int>();
        var curDist = 0;
        while (curDist <= maxAllowedDistance)
        {
            var newCells = visited.SelectMany(cel => _passable[cel]).Except(visited).ToArray();
            if (curDist >= minAllowedDistance)
                chooseFrom.UnionWith(newCells);
            visited.UnionWith(newCells);
            curDist++;
        }
        _goalCell = chooseFrom.PickRandom();

        Debug.LogFormat(@"[Crazy Maze #{0}] Start cell: {1}", _moduleID, _cellLetters[_currentCell]);
        Debug.LogFormat(@"[Crazy Maze #{0}] Goal cell: {1}", _moduleID, _cellLetters[_goalCell]);

        StartCoroutine(CellTextAnimation(CurCellText.transform, 0, adjustX: -.02f, adjustZ: .02f));
        StartCoroutine(CellTextAnimation(GoalCellText.transform, 1, adjustX: .02f, adjustZ: -.02f));
    }

    void SetCell(int cell)
    {
        var cellTransitions = CellTransitions.All[cell];
        var transitions = cellTransitions.Neighbors;

        for (int arIx = 0; arIx < Arrows.Length; arIx++)
        {
            if (arIx >= transitions.Length)
                Arrows[arIx].gameObject.SetActive(false);
            else
            {
                Arrows[arIx].transform.localPosition = new Vector3(transitions[arIx].ArrowX, .0154f, -transitions[arIx].ArrowY);
                Arrows[arIx].transform.localEulerAngles = new Vector3(0, transitions[arIx].ArrowAngle, 0);
                Arrows[arIx].gameObject.SetActive(true);
            }
        }
        _currentCell = cell;
        CurrentCell.sprite = Sprites[cell];
    }

    private IEnumerator CellTextAnimation(Transform trf, float offset, float duration = 2f, float intensity = 0.018f, float adjustX = 0.02f, float adjustZ = 0.02f)
    {
        while (true)
        {
            yield return null;
            var timer = Time.time;
            float x = Mathf.Cos((timer + offset) / duration * 2 * Mathf.PI) * intensity;
            float z = Mathf.Sin((timer + offset) / duration * 2 * Mathf.PI) * intensity;
            trf.localPosition = new Vector3(x + adjustX, trf.localPosition.y, z + adjustZ);
            CurCellText.text = _moduleSolved ? "G" : _showingGoal ? "??" : _cellLetters[_currentCell];
            GoalCellText.text = _moduleSolved ? "G" : _showingGoal ? _cellLetters[_goalCell] : "??";
        }
    }
}
