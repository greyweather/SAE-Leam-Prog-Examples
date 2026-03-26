using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Unity.VisualScripting;
using UnityEngine;

public class MidPointDisplacementTest : MonoBehaviour
{
    [SerializeField] private GameObject gridPoint;
    [SerializeField] private float _gridSize = 1f;
    [SerializeField, Range(3, 100)] private float _maxHeight = 2f;
    [Header("Grid Square Root 250 tested \nLoads in ~30 seconds with high end pc")]
    [SerializeField] private int _GridSquareRoot = 11;

    [Header("Higher is less random")]
    [SerializeField, Range(500f, 50)] private float _randomness = 10f;
    [SerializeField] private bool _extraVariation = false;
    
    private readonly float _spawnDelayTime = 0.1f;
    private MP_GridPoint[,] _masterArray;
    private MP_GridPoint[,] _cornerArray;


    


    void Start()
    {
        StartCoroutine(SpawnGrid());
    }
    

    private IEnumerator SpawnGrid()
    { //Spawn all the points based on the size of the grid
        int _count = 0;
        _masterArray = new MP_GridPoint[_GridSquareRoot, _GridSquareRoot];

        for(int col = 0; col < _GridSquareRoot; col++)
        {
            for(int row = 0; row < _GridSquareRoot; row++)
            {
                Vector3 _spawnPos = new(row * _gridSize, 0, col * _gridSize); 
                MP_GridPoint _gridPoint = Instantiate(gridPoint, _spawnPos, Quaternion.identity).GetComponent<MP_GridPoint>();
                _count++;
                _masterArray[col, row] = _gridPoint;
                _gridPoint.CurrentPos = new Vector2(col, row);
            }
        }
        StartCoroutine(SetHeight());
        yield break;
    }

    private IEnumerator SetHeight()
    {
        //Get first set of corners
        _cornerArray = new MP_GridPoint[,]
        {
            {_masterArray[0,0], _masterArray[_masterArray.GetLength(0) -1, 0]},
            {_masterArray[0, _masterArray.GetLength(1) -1], _masterArray[_masterArray.GetLength(0) -1, _masterArray.GetLength(1) -1]}
        };

        // Set Height Of Corners
        for(int col = 0; col < _cornerArray.GetLength(0); col++)
        {
            for(int row = 0; row < _cornerArray.GetLength(1); row++)
            {   
                _cornerArray[col, row].UpdateHeight(Random.Range(0, _maxHeight));
                yield return new WaitForSeconds(_spawnDelayTime);
            }
        }


    StartCoroutine(SetHeightChild(_cornerArray, 0));
    yield break;
    }

    private IEnumerator SetHeightChild(MP_GridPoint[,] __cornerArray, int _reoccurred)
    {

        float _avHeightNorth;
        float _avHeightEast;
        float _avHeightSouth;
        float _avHeightWest;

        float _avHeightCen;


        Vector2 _midPointNorthPos;
        Vector2 _midPointEastPos;
        Vector2 _midPointSouthPos;
        Vector2 _midPointWestPos;

        Vector2 _midPointPos;


        MP_GridPoint _midPointNorth;
        MP_GridPoint _midPointEast;
        MP_GridPoint _midPointSouth;
        MP_GridPoint _midPointWest;
        
        MP_GridPoint _midPointCenter;
        MP_GridPoint[,] _currentCornerArray = __cornerArray;

                //MidPoints
                //Get average Height 
                _avHeightNorth = (_currentCornerArray[0, 1].Height + _currentCornerArray[1, 1].Height) / 2;
                _avHeightEast = (_currentCornerArray[1, 0].Height + _currentCornerArray[1, 1].Height) / 2;
                _avHeightSouth = (_currentCornerArray[0, 0].Height +_currentCornerArray[0, 1].Height) / 2;
                _avHeightWest = (_currentCornerArray[0, 0].Height + _currentCornerArray[1, 0].Height) / 2;
                //Add some randomness
                if(_reoccurred == 0 && _extraVariation)
                {//More randomess on first time
                    _avHeightNorth = Random.Range(_maxHeight / 3, _maxHeight - (_maxHeight / 3));
                    _avHeightEast = Random.Range(_maxHeight / 3, _maxHeight - (_maxHeight / 3)); 
                    _avHeightSouth = Random.Range(_maxHeight / 3, _maxHeight - (_maxHeight / 3));
                    _avHeightWest = Random.Range(_maxHeight / 3, _maxHeight - (_maxHeight / 3));
                }
                else
                {
                    _avHeightNorth = Random.Range(_avHeightNorth - (_maxHeight / _randomness), _avHeightNorth + (_maxHeight / _randomness));
                    _avHeightEast = Random.Range(_avHeightEast - (_maxHeight / _randomness), _avHeightEast + (_maxHeight / _randomness)); 
                    _avHeightSouth = Random.Range(_avHeightSouth - (_maxHeight / _randomness), _avHeightSouth + (_maxHeight / _randomness));
                    _avHeightWest = Random.Range(_avHeightWest - (_maxHeight / _randomness), _avHeightWest + (_maxHeight / _randomness));
                }
                //Get Midpoints
                _midPointNorthPos = (_currentCornerArray[0, 1].CurrentPos + _currentCornerArray[1, 1].CurrentPos) / 2;
                _midPointEastPos = (_currentCornerArray[1, 0].CurrentPos + _currentCornerArray[1, 1].CurrentPos) / 2;
                _midPointSouthPos = (_currentCornerArray[0, 0].CurrentPos +_currentCornerArray[0, 1].CurrentPos) / 2;
                _midPointWestPos = (_currentCornerArray[0, 0].CurrentPos + _currentCornerArray[1, 0].CurrentPos) / 2;
                //Set MidPoints
                _midPointNorth = _masterArray[Mathf.RoundToInt(_midPointNorthPos.x), Mathf.RoundToInt(_midPointNorthPos.y)];
                _midPointEast = _masterArray[Mathf.RoundToInt(_midPointEastPos.x), Mathf.RoundToInt(_midPointEastPos.y)];
                _midPointSouth = _masterArray[Mathf.RoundToInt(_midPointSouthPos.x), Mathf.RoundToInt(_midPointSouthPos.y)];
                _midPointWest = _masterArray[Mathf.RoundToInt(_midPointWestPos.x), Mathf.RoundToInt(_midPointWestPos.y)];
                //SetHeight
                _midPointNorth.UpdateHeight(_avHeightNorth);
                yield return new WaitForSeconds(_spawnDelayTime);
                _midPointEast.UpdateHeight(_avHeightEast);
                yield return new WaitForSeconds(_spawnDelayTime);
                _midPointSouth.UpdateHeight(_avHeightSouth);
                yield return new WaitForSeconds(_spawnDelayTime);
                _midPointWest.UpdateHeight(_avHeightWest);
                yield return new WaitForSeconds(_spawnDelayTime);


                //CentrePoint  
                //Get average Height
                _avHeightCen = (_midPointNorth.Height + _midPointEast.Height + _midPointSouth.Height + _midPointWest.Height) / 4;
                //Add Randomness
                if(_reoccurred == 0 && _extraVariation)
                {//More randomess on first time
                    _avHeightCen = Random.Range(_maxHeight / 3, _maxHeight - (_maxHeight / 3));
                }
                else
                {
                    _avHeightCen = Random.Range(_avHeightCen - (_maxHeight / _randomness), _avHeightCen + (_maxHeight / _randomness));
                }
                //Get CentrePoint0
                _midPointPos = (_midPointNorth.CurrentPos + _midPointEast.CurrentPos + _midPointSouth.CurrentPos + _midPointWest.CurrentPos) / 4;
                //Set CentrePoint 
                _midPointCenter = _masterArray[Mathf.RoundToInt(_midPointPos.x), Mathf.RoundToInt(_midPointPos.y)];

                if(_midPointCenter.Height != 0)
                {//Stop Recuring if center point already done, the grid is so small the corner is also the centre
                    yield break;
                }

                //SetHeight
                _midPointCenter.UpdateHeight(_avHeightCen);
                yield return new WaitForSeconds(_spawnDelayTime);

                //Recursion for new corners from sides and center 
               MP_GridPoint[,]  _childCornerArray = new MP_GridPoint[,]
                {
                    {_currentCornerArray[0, 0], _midPointSouth,},
                    {_midPointWest, _midPointCenter} 
                };
                StartCoroutine(SetHeightChild(_childCornerArray, _reoccurred + 1));


                _childCornerArray = new MP_GridPoint[,]
                {
                    {_midPointSouth, _midPointCenter},
                    {_currentCornerArray[0, 1], _midPointNorth}
                };
                StartCoroutine(SetHeightChild(_childCornerArray, _reoccurred + 1));


                _childCornerArray = new MP_GridPoint[,]
                {
                    {_midPointCenter, _midPointEast},
                    {_midPointNorth, _currentCornerArray[1, 1]}
                };
                StartCoroutine(SetHeightChild(_childCornerArray, _reoccurred + 1));


                _childCornerArray = new MP_GridPoint[,]
                {
                    {_midPointWest, _currentCornerArray[1, 0]},
                    {_midPointCenter, _midPointEast}
                };
                StartCoroutine(SetHeightChild(_childCornerArray, _reoccurred + 1));
                yield break;
               
    }

}

