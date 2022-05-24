using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public interface YTableViewSource
{
    public int TableViewNumbers();
    public Vector2 CellSizeForIndex(int index);
    public YTableViewCell CellForIndex(int index, RectTransform parentTransform, YTableViewCell yTableViewCell);
}

public interface YTableViewDelegate
{
    public void OnTouchDown(YTableView yTableView);
    public void OnScrolling(YTableView yTableView);
    public void OnScrollEnd(YTableView yTableView);
}

public class YTableViewCell : BaseMonoBehaviour
{

}

public class YTableView : ScrollRect
{
    public bool EnablePage = false;
    public bool TableHorizontal = false;
    private RectTransform ContentTransform;

    public YTableViewSource TableViewSource { get; set; } = null;
    public YTableViewDelegate TableViewDelegate { get; set; } = null;
    public float spaceing { get; set; } = 0;
    public bool ObjectPoolManagerCell { get; set; } = false;

    public Vector2 ContentOffset
    {
        get
        {
            return ContentTransform.anchoredPosition;
        }
    }

    private TouchEventListener touchEventListener = null;

    private float maxWidthOrHeight = 0;
    private float allWidthOrHeight = 0;
    private Dictionary<int, Vector2> eachItemPos = new Dictionary<int, Vector2>();
    private int visibleStartIndex = 0;
    private int visibleEndIndex = 0;

    private Dictionary<int, YTableViewCell> usedTableViewCells = new Dictionary<int, YTableViewCell>();
    private List<YTableViewCell> unusedTableViewCells = new List<YTableViewCell>();

    private bool calledLoadData = false;

    private const float autoMoveTime = (float)0.15;
    private const float initSpeed = (float)100;

    private int pageIndex = 0;
    public int PageIndex { get { return pageIndex; } }
    private Vector2 onClickDownPosition;
    private bool inAutoMoving = false;
    private float currentSpeed = initSpeed;
    private float targetPos = 0;

    protected override void Awake()
    {
        touchEventListener = gameObject.GetComponent<TouchEventListener>();
        if (touchEventListener == null)
            touchEventListener = gameObject.AddComponent<TouchEventListener>();
        ContentTransform = viewport.GetChild(0).GetComponent<RectTransform>();
        onValueChanged.AddListener((v) =>
        {
            if (!calledLoadData) return;
            CalcuVisibleIndex();
            LoadCell();
            if (TableViewDelegate != null)
                TableViewDelegate.OnScrolling(this);
        });

        touchEventListener.OnClickDownCallback = OnClickDown;
        touchEventListener.OnStartDragCallback = OnDragBegain;
        touchEventListener.OnDragCallback = OnTouchMoved;
        touchEventListener.OnEndDragCallback = OnTouchEnd;
    }

    protected override void OnDestroy()
    {
        if (ObjectPoolManagerCell)
        {
            foreach (var keyValue in usedTableViewCells)
            {
                ObjectManager.Instance.ReleaseGameObject(keyValue.Value.gameObject, 0, false);
            }
            for (int i = 0; i < unusedTableViewCells.Count; i++)
            {
                ObjectManager.Instance.ReleaseGameObject(unusedTableViewCells[i].gameObject, 0, false);
            }

        }
    }

    public YTableViewCell GetVisibleTableCell(int index)
    {
        if (usedTableViewCells.ContainsKey(index)) return usedTableViewCells[index];
        return null;
    }

    public IEnumerator CallScrollEnd()
    {
        yield return null;
        if (TableViewDelegate != null)
            TableViewDelegate.OnScrollEnd(this);
    }

    public void Update()
    {
        if (inAutoMoving)
        {
            StopMovement();
            float newPosition;
            if (TableHorizontal)
            {
                newPosition = Mathf.SmoothDamp(ContentTransform.anchoredPosition.x, targetPos, ref currentSpeed, (float)autoMoveTime);
                ContentTransform.anchoredPosition = new Vector2(newPosition, 0);
            }
            else
            {
                newPosition = Mathf.SmoothDamp(ContentTransform.anchoredPosition.y, targetPos, ref currentSpeed, (float)autoMoveTime);
                ContentTransform.anchoredPosition = new Vector2(0, newPosition);
            }
            if (Math.Abs(currentSpeed) <= 1)
            {
                if (TableHorizontal)
                {
                    ContentTransform.anchoredPosition = new Vector2(targetPos, 0);
                }
                else
                {
                    ContentTransform.anchoredPosition = new Vector2(0, targetPos);
                }
                inAutoMoving = false;
                StartCoroutine(CallScrollEnd());
            }
            else
            {
                //if (TableViewDelegate != null)
                //    TableViewDelegate.OnScrolling(this);
            }
        }
    }

    public void ScrollToPage(int index, bool animation = true)
    {
        if (TableViewSource == null) return;
        if (!EnablePage) return;
        if (index < 0 || index >= TableViewSource.TableViewNumbers()) return;

        currentSpeed = initSpeed;
        pageIndex = index;
        if (TableHorizontal)
        {
            targetPos = -eachItemPos[pageIndex].x;
        }
        else
        {
            targetPos = -eachItemPos[pageIndex].x;
        }
        if (animation)
        {
            inAutoMoving = true;

        }
        else
        {
            if (TableHorizontal)
            {
                ContentTransform.anchoredPosition = new Vector2(targetPos, 0);
            }
            else
            {
                ContentTransform.anchoredPosition = new Vector2(0, targetPos);
            }
            CalcuVisibleIndex();
            LoadCell();
            StartCoroutine(CallScrollEnd());
        }
    }

    private void OnClickDown(PointerEventData obj)
    {
        inAutoMoving = false;
        onClickDownPosition = obj.position;
        if (TableViewDelegate != null)
            TableViewDelegate.OnTouchDown(this);
    }

    private void OnDragBegain(PointerEventData obj)
    {
        OnClickDown(obj);
    }

    private void OnTouchMoved(PointerEventData obj)
    {
    }

    private void OnTouchEnd(PointerEventData obj)
    {
        if (EnablePage)
        {
            float changedValue = 0;
            int indexChange = 0;
            if (TableHorizontal)
            {
                changedValue = obj.position.x - onClickDownPosition.x;
                if (changedValue < 0)
                {
                    indexChange = 1;
                }
                else
                {
                    indexChange = -1;
                }
            }
            else
            {
                changedValue = obj.position.y - onClickDownPosition.y;
                if (changedValue > 0)
                {
                    indexChange = 1;
                }
                else
                {
                    indexChange = -1;
                }
            }
            if (Math.Abs(changedValue) <= 80) indexChange = 0;
            pageIndex += indexChange;
            if (pageIndex < 0) pageIndex = 0;
            if (pageIndex > TableViewSource.TableViewNumbers() - 1) pageIndex = TableViewSource.TableViewNumbers() - 1;
            StopMovement();
            ScrollToPage(pageIndex);
        }
        else
        {
            StartCoroutine(CallScrollEnd());
        }

    }

    public void ReloadData()
    {
        ResetState();
        if (TableViewSource == null) return;
        ResetContent();
        CalcuVisibleIndex();
        LoadCell();
        calledLoadData = true;
    }

    private void CalcuVisibleIndex()
    {
        float startPosition = 0;
        float endPosition = 0;
        if (eachItemPos.Count == 0) return;
        if (TableHorizontal)
        {
            startPosition = -ContentTransform.anchoredPosition.x;
            endPosition = startPosition + viewRect.rect.width;
        }
        else
        {
            startPosition = ContentTransform.anchoredPosition.y;
            endPosition = startPosition + viewRect.rect.height;
        }
        visibleStartIndex = 0;
        visibleEndIndex = visibleStartIndex;
        if (eachItemPos[eachItemPos.Count - 1].x <= startPosition)
        {
            visibleStartIndex = eachItemPos.Count - 1;
        }
        else
        {
            // first cell which start position in the view port.
            for (int i = 0; i < TableViewSource.TableViewNumbers(); i++)
            {
                if (startPosition <= eachItemPos[i].x && eachItemPos[i].x <= endPosition)
                {
                    visibleStartIndex = i;
                    break;
                }
            }
        }
        for (int i = visibleStartIndex + 1; i < TableViewSource.TableViewNumbers(); i++)
        {
            // last cell which end position in the view port.
            if (startPosition <= eachItemPos[i].y && eachItemPos[i].y <= endPosition)
            {
                if (i > visibleEndIndex) visibleEndIndex = i;
            }
            if (eachItemPos[i].x > endPosition) break;
        }
        if (visibleEndIndex < visibleStartIndex) visibleEndIndex = visibleStartIndex;
    }

    private void LoadCell()
    {
        if (TableViewSource == null) return;
        List<int> needRemove = new List<int>();
        foreach (var keyValue in usedTableViewCells)
        {
            if (keyValue.Key < visibleStartIndex - 1 || keyValue.Key > visibleEndIndex + 1)
            {
                needRemove.Add(keyValue.Key);
            }
        }
        for (int i = 0; i < needRemove.Count; i++)
        {
            usedTableViewCells[needRemove[i]].gameObject.SetActive(false);
            unusedTableViewCells.Add(usedTableViewCells[needRemove[i]]);
            usedTableViewCells.Remove(needRemove[i]);
        }

        for (int i = visibleStartIndex - 1; i <= visibleEndIndex + 1; i++)
        {
            if (i < 0) continue;
            if (i > TableViewSource.TableViewNumbers() - 1) break;
            if (usedTableViewCells.ContainsKey(i)) continue;
            YTableViewCell cell = null;
            if (unusedTableViewCells.Count > 0)
            {
                cell = unusedTableViewCells[0];
                unusedTableViewCells.RemoveAt(0);
            }
            cell = TableViewSource.CellForIndex(i, ContentTransform, cell);
            cell.gameObject.SetActive(true);
            bool needAdd = false;
            RectTransform rectTransform = cell.gameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                cell.gameObject.AddComponent<RectTransform>();
                rectTransform = cell.gameObject.GetComponent<RectTransform>();
                needAdd = true;
            }
            if (needAdd)
            {
                rectTransform.SetParent(ContentTransform);
            }
            usedTableViewCells[i] = cell;
            rectTransform.anchorMin = new Vector2((float)0.5, (float)0.5);
            rectTransform.anchorMax = new Vector2((float)0.5, (float)0.5);
            rectTransform.pivot = new Vector2((float)0.5, (float)0.5);

            var halfPoint = new Vector3(0, ContentTransform.rect.height / 2, 0);
            if (TableHorizontal)
            {
                rectTransform.localPosition = new Vector3((eachItemPos[i].x + eachItemPos[i].y) / 2, viewRect.rect.height / 2, 0) - halfPoint;
            }
            else
            {
                rectTransform.localPosition = new Vector3(viewRect.rect.width / 2, (content.rect.height - eachItemPos[i].x - eachItemPos[i].y) / 2, 0) - halfPoint;
            }
        }
    }

    private void ResetContent()
    {
        if (TableHorizontal)
        {
            horizontal = true;
            vertical = false;
            ContentTransform.anchorMin = new Vector2(0, 0);
            ContentTransform.anchorMax = new Vector2(0, 1);
            ContentTransform.sizeDelta = new Vector2(allWidthOrHeight, 0);
        }
        else
        {
            horizontal = false;
            vertical = true;
            ContentTransform.anchorMin = new Vector2(0, 1);
            ContentTransform.anchorMax = new Vector2(1, 1);
            ContentTransform.sizeDelta = new Vector2(0, allWidthOrHeight);
        }
        ContentTransform.anchoredPosition = new Vector3(0, 0, 0);
    }

    private void ResetState()
    {
        eachItemPos.Clear();
        pageIndex = 0;
        maxWidthOrHeight = 0;
        usedTableViewCells.Clear();
        unusedTableViewCells.Clear();
        visibleStartIndex = 0;
        visibleEndIndex = 0;
        calledLoadData = false;

        while (ContentTransform.childCount > 0) GameObject.Destroy(ContentTransform.GetChild(0).gameObject);
        CalcuSize();
    }

    private void CalcuSize()
    {
        if (TableViewSource == null) return;
        for (int i = 0; i < TableViewSource.TableViewNumbers(); i++)
        {
            var cellSize = TableViewSource.CellSizeForIndex(i);
            if (TableHorizontal)
            {
                eachItemPos[i] = new Vector2(allWidthOrHeight, allWidthOrHeight + cellSize.x);
                allWidthOrHeight += cellSize.x;
                if (cellSize.y > maxWidthOrHeight) maxWidthOrHeight = cellSize.y;
            }
            else
            {
                eachItemPos[i] = new Vector2(allWidthOrHeight, allWidthOrHeight + cellSize.y);
                allWidthOrHeight += cellSize.y;
                if (cellSize.x > maxWidthOrHeight) maxWidthOrHeight = cellSize.x;
            }
            allWidthOrHeight += spaceing;
        }
        allWidthOrHeight -= spaceing;
    }

}
