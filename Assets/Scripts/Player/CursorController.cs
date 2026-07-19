using UnityEngine;
using System.Collections.Generic;

public class CursorController : MonoBehaviour
{
    bool isMouseDown = false;
    LineRenderer lineRenderer;
    public ParticleSystem cursor;

    Vector3 startPos;
    Vector3 endPos = Vector3.zero;
    Vector3 RightUp;
    Vector3 LeftDown;

    Vector3 worldStartPos;
    RaycastHit hitInfo;

    Vector3 center;
    Vector3 halfExtents;

    public List<CharacterObj> selectedCharacters;

    public LayerMask groundLayer;
    public LayerMask characterLayer;

    [Header("队伍状态")]
    public float teamDistance = 2f;
    public float minMoveDistance = 1f;
    public int maxTeamates = 10;
    public Vector3 teamFront;
    public Vector3 teamRight;
    public Vector3 targetTeamDirection;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        selectedCharacters = new List<CharacterObj>();
    }

    void Update()
    {
        SelectCharacters();

        CharacterMove();
    }

    private void SelectCharacters()
    {
        DrawLine();

        if (Input.GetMouseButtonDown(0))
        {
            isMouseDown = true;
            startPos = Input.mousePosition;

            if (Physics.Raycast(Camera.main.ScreenPointToRay(startPos), out hitInfo, Camera.main.farClipPlane, groundLayer))
            {
                worldStartPos = hitInfo.point;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isMouseDown = false;
            lineRenderer.positionCount = 0;

            foreach (CharacterObj character in selectedCharacters)
            {
                character.IsSelected(false);
            }
            selectedCharacters.Clear();

            if (Physics.Raycast(Camera.main.ScreenPointToRay(endPos), out hitInfo, Camera.main.farClipPlane, groundLayer))
            {
                center = new Vector3((hitInfo.point.x + worldStartPos.x) / 2, 0, (hitInfo.point.z + worldStartPos.z) / 2);

                halfExtents = new Vector3(Mathf.Abs(hitInfo.point.x - worldStartPos.x) / 2, 100, Mathf.Abs(hitInfo.point.z - worldStartPos.z) / 2);

                Collider[] colliders = Physics.OverlapBox(center, halfExtents);

                for (int i = 0; selectedCharacters.Count < maxTeamates && i < colliders.Length; i++)
                {
                    CharacterObj character = colliders[i].transform.GetComponent<CharacterObj>();

                    if (character != null)
                    {
                        selectedCharacters.Add(character);
                        character.IsSelected(true);
                    }
                }
            }
        }
    }
    private void DrawLine()
    {
        if (isMouseDown)
        {
            startPos.z = 5;

            endPos = Input.mousePosition;
            endPos.z = 5;

            RightUp.x = endPos.x;
            RightUp.y = startPos.y;
            RightUp.z = 5;

            LeftDown.x = startPos.x;
            LeftDown.y = endPos.y;
            LeftDown.z = 5;

            lineRenderer.positionCount = 4;
            lineRenderer.SetPosition(0, Camera.main.ScreenToWorldPoint(startPos));
            lineRenderer.SetPosition(1, Camera.main.ScreenToWorldPoint(RightUp));
            lineRenderer.SetPosition(2, Camera.main.ScreenToWorldPoint(endPos));
            lineRenderer.SetPosition(3, Camera.main.ScreenToWorldPoint(LeftDown));
        }
    }

    private void CharacterMove()
    {
        if (Input.GetMouseButtonDown(1) && selectedCharacters.Count > 0)
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, Camera.main.farClipPlane, groundLayer))
            {
                transform.position = hitInfo.point;
                cursor.Play();

                Vector3 teamCenter = CalculateTeamCenter(selectedCharacters);
                if ((hitInfo.point - teamCenter).magnitude >= minMoveDistance)
                {
                    List<Vector3> targetPositions = GetTargetPositionList(hitInfo.point, teamCenter);
                    for (int i = 0, count = selectedCharacters.Count; i < count; i++)
                    {
                        selectedCharacters[i].Move(targetPositions[i]);
                        selectedCharacters[i].StartCoroutine(selectedCharacters[i].MoveIns());
                    }
                }
                else
                {
                    for (int i = 0, count = selectedCharacters.Count; i < count; i++)
                    {
                        selectedCharacters[i].agent.ResetPath();
                        selectedCharacters[i].StartCoroutine(selectedCharacters[i].MoveIns());
                    }
                }
            }
        }
    }

    private List<Vector3> GetTargetPositionList(Vector3 targetPosition, Vector3 teamCenter)
    {
        GetTeamDirection(targetPosition, teamCenter);

        List<Vector3> targetPositionList = new List<Vector3>(selectedCharacters.Count);

        int row = Mathf.FloorToInt(Mathf.Sqrt(selectedCharacters.Count));
        int column = Mathf.CeilToInt((float)selectedCharacters.Count / row);

        float rowOffset = (row - 1) / 2f;
        float columnOffset = (column - 1) / 2f;

        for (int i = 0; i < selectedCharacters.Count; i++)
        {
            int rowIndex = i / column;
            int columnIndex = i % column;
            Vector3 offset = teamFront * (rowIndex - rowOffset) * teamDistance + teamRight * (columnIndex - columnOffset) * teamDistance;
            targetPositionList.Add(targetPosition + offset);
        }

        return targetPositionList;
    }

    private void GetTeamDirection(Vector3 targetPosition, Vector3 teamCenter)
    {
        targetTeamDirection = (teamCenter - targetPosition).normalized;

        Vector3 teamDir = CalculateTeamDirection(selectedCharacters);
        Vector3 modifyDirection = new Vector3(Mathf.Abs(teamDir.x), Mathf.Abs(teamDir.y), Mathf.Abs(teamDir.z));

        if (Vector3.Dot(modifyDirection.normalized, targetTeamDirection.normalized) < 0)
        {
            teamFront = -targetTeamDirection;
        }
        else
        {
            teamFront = targetTeamDirection;
        }

        teamRight = Vector3.Cross(Vector3.up, teamFront);
    }

    private Vector3 CalculateTeamCenter(List<CharacterObj> characters)
    {
        Vector3 center = Vector3.zero;
        foreach (CharacterObj character in characters)
        {
            center += character.transform.position;
        }
        return center / characters.Count;
    }
    private Vector3 CalculateTeamDirection(List<CharacterObj> characters)
    {
        Vector3 center = Vector3.zero;
        foreach (CharacterObj character in characters)
        {
            center += character.transform.forward;
        }
        return center / characters.Count;
    }
}