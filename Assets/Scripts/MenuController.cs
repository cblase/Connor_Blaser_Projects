using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MenuController : MonoBehaviour
{
    public TMP_InputField inputRow;
    public TMP_InputField inputCol;
    public TMP_InputField inputCell;
    public TextMeshProUGUI row, col, size;
    public Player player;
    private int Width, Height, CellSize;

    public void Start()
    {
        //starting size
        Width = 50;
        Height = 50;
        CellSize = 1;
        CreateNewGrid();
    }

    public void CreateNewGrid()
    {
        player.Grid.GenerateNavGrid(Width, Height, CellSize);
        UpdateUI();
    }

    private void UpdateUI()
    {
        row.text = Width.ToString();
        col.text = Height.ToString();
        size.text = CellSize.ToString();
    }

    public void ReadRowData()
    {
        int row;
        if (int.TryParse(inputRow.text, out row))
        {
            Width = row;
        }
        Debug.Log("Row Data: " + inputRow.text);
    }

    public void ReadColumnData()
    {
        int col;
        if (int.TryParse(inputCol.text, out col))
        {
            Height = col;
        }
        Debug.Log("Collumn Data: " + inputCol.text);
    }

    public void ReadCellSizeData()
    {
        int cell;
        if (int.TryParse(inputCell.text, out cell))
        {
            CellSize = cell;
        }
    }
}
