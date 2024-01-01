using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    [Header("UI Elements")]
    [SerializeField] private Transform levelSelectPanel;
    [SerializeField] private List<Texture2D> imageTextures;
    [SerializeField] private Image levelSelectPrefab;
    [SerializeField] private GameObject playAgainButton;

    [Header("Game Elements")]
    [Range(2, 10)]
    [SerializeField] private int difficulty = 10;
    [SerializeField] private Transform gameHolder;
    [SerializeField] private Transform piecePrefab;
    [SerializeField] private AudioSource clickSound;

    private List<Transform> pieces;
    private Vector2Int dimensions;
    private float width;
    private float height;
    private Transform draggingPiece = null;

    private int piecesCorrect;
    private int levelsBeaten = 0;

    void Start()
    {
        // clickSound = GetComponent<AudioSource>();
        int index = 0;
        foreach (Texture2D texture in imageTextures)
        {
            Image image = Instantiate(levelSelectPrefab, levelSelectPanel);
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            Button button = image.GetComponent<Button>();
            button.onClick.AddListener(delegate { StartGame(texture); });
            if (index > levelsBeaten) {
                image.color = new Color32(255, 255, 255, 90);
                button.interactable = false;
            }
            index++;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButton(0) && !draggingPiece)
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit)
            {
                draggingPiece = hit.transform;
                foreach (Transform piece in pieces) {
                    float x = piece.position.x;
                    float y = piece.position.y;
                    float z = -1;
                    piece.position = new Vector3(x, y, z);
                }
                draggingPiece.position = new Vector3(draggingPiece.position.x,
                                                     draggingPiece.position.y,
                                                     -2);
            }
        }

        if (draggingPiece)
        {
            Vector3 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            newPosition.z = draggingPiece.position.z;
            draggingPiece.position = newPosition;
        }

        if (draggingPiece && Input.GetMouseButtonUp(0))
        {
            SnapAndDisableIfCorrect();
            draggingPiece = null;
        }
    }

    public void StartGame(Texture2D jigsawTexture)
    {
        // hide UI
        levelSelectPanel.gameObject.SetActive(false);

        // list of transforms for each piece
        pieces = new List<Transform>();

        // get size of each piece based on difficulty
        dimensions = GetDimensions(jigsawTexture, difficulty);

        // Create the pieces
        CreateJigsawPieces(jigsawTexture);
        Scatter();
        UpdateBorder();
        piecesCorrect = 0;

    }

    private void SnapAndDisableIfCorrect() {
        int pieceIndex = pieces.IndexOf(draggingPiece);

        int col = pieceIndex % dimensions.x;
        int row = pieceIndex / dimensions.x;

        Vector2 targetPosition = new(
            (-width * dimensions.x / 2) + (width * col) + (width / 2),
            (-height * dimensions.y / 2) + (height * row) + (height / 2)
        );

        if (Vector2.Distance(draggingPiece.localPosition, targetPosition) < (width / 4)) {
            draggingPiece.localPosition = new Vector3(targetPosition.x, targetPosition.y, 0);
            draggingPiece.GetComponent<BoxCollider2D>().enabled = false;
            piecesCorrect++;
            clickSound.Play();
            if (piecesCorrect == pieces.Count) {
                playAgainButton.SetActive(true);
                levelsBeaten++;
            }
        }
    }
    Vector2Int GetDimensions(Texture2D jigsawTexture, int difficulty)
    {
        Vector2Int dimensions = Vector2Int.zero;
        if (jigsawTexture.width < jigsawTexture.height)
        {
            dimensions.x = difficulty;
            dimensions.y = difficulty * jigsawTexture.height / jigsawTexture.width;

        }
        else
        {
            dimensions.x = difficulty * jigsawTexture.width / jigsawTexture.height;
            dimensions.y = difficulty;
        }

        return dimensions;
    }

    void CreateJigsawPieces(Texture2D jigsawTexture)
    {
        height = 1f / dimensions.y;
        float aspect = (float)jigsawTexture.width / jigsawTexture.height;
        width = aspect / dimensions.x;
        for (int row = 0; row < dimensions.y; row++)
        {
            for (int col = 0; col < dimensions.x; col++)
            {
                // Create the piece in the right location of the right size
                Transform piece = Instantiate(piecePrefab, gameHolder);
                piece.localPosition = new Vector3(
                   (-width * dimensions.x / 2) + (width * col) + (width / 2),
                   (-height * dimensions.x / 2) + (height * row) + (height / 2),
                   -1);
                piece.localScale = new Vector3(width, height, 1f);
                piece.name = $"Piece {(row * dimensions.x) + col}";
                pieces.Add(piece);

                // Assign correct part of texture image to piece
                // Normalize width and height
                float width1 = 1f / dimensions.x;
                float height1 = 1f / dimensions.y;

                Vector2[] uv = new Vector2[4];
                uv[0] = new Vector2(width1 * col, height1 * row);
                uv[1] = new Vector2(width1 * (1 + col), height1 * row);
                uv[2] = new Vector2(width1 * col, height1 * (1 + row));
                uv[3] = new Vector2(width1 * (1 + col), height1 * (1 + row));

                Mesh mesh = piece.GetComponent<MeshFilter>().mesh;
                mesh.uv = uv;
                piece.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", jigsawTexture);

            }
        }

    }

    private void Scatter()
    {
        // Calculate the visible screen size
        float orthoHeight = Camera.main.orthographicSize;
        float screenAspect = (float)Screen.width / Screen.height;
        float orthoWidth = (screenAspect * orthoHeight);

        // confine pieces to edges of screen
        float pieceWidth = width * gameHolder.localScale.x;
        float pieceHeight = height * gameHolder.localScale.y;
        orthoHeight -= pieceHeight;
        orthoWidth -= pieceWidth;

        foreach (Transform piece in pieces)
        {
            float x = Random.Range(-orthoWidth, orthoWidth);
            float y = Random.Range(-orthoHeight, orthoHeight);
            piece.position = new Vector3(x, y, -1);
        }
    }

    private void UpdateBorder()
    {
        LineRenderer lineRenderer = gameHolder.GetComponent<LineRenderer>();
        Debug.Log(width);
        Debug.Log(dimensions.x);
        float halfWidth = width * dimensions.x / 2f;
        float halfHeight = height * dimensions.y / 2f;
        float borderZ = 0f;
        Debug.Log(halfWidth);

        lineRenderer.SetPosition(0, new Vector3(-halfWidth, halfHeight, borderZ));
        lineRenderer.SetPosition(1, new Vector3(halfWidth, halfHeight, borderZ));
        lineRenderer.SetPosition(2, new Vector3(halfWidth, -halfHeight, borderZ));
        lineRenderer.SetPosition(3, new Vector3(-halfWidth, -halfHeight, borderZ));
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;

        lineRenderer.enabled = true;
    }

    public void RestartGame() {
        foreach (Transform piece in pieces) {
            Destroy(piece.gameObject);
        }
        pieces.Clear();
        gameHolder.GetComponent<LineRenderer>().enabled = false;
        playAgainButton.SetActive(false);
        levelSelectPanel.gameObject.SetActive(true);
    }
}
