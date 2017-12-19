using UnityEngine;
using System.Collections.Generic;

public class MultiUnitGroup {
    List<MultiUnit> units = new List<MultiUnit>();

    MultiList multiList;

    #region interfaces for convenience
    Rect bound {
        get {
            return multiList.bound;
        }
    }

    RectTransform content {
        get {
            return multiList.content;
        }
    }

    Vector2 padding {
        get {
            return multiList.padding;
        }
    }

    bool isHorizontalFirst {
        get {
            return multiList.isHorizontalFirst;
        }
    }

    Vector2 contentStartPos {
        get {
            return multiList.contentStartPos;
        }
    }

    public RectTransform lastUnitTrans {
        get {
            if (!hasUnits || units.Count == 0)
                return null;
            return units[units.Count - 1].rectTrans;
        }
    }

    public bool hasUnits {
        get {
            return unitCount > 0;
        }
    }

    public int unitCount {
        get {
            return units.Count;
        }
    }

    public int startSibIndex {
        private set;
        get;
    }
    #endregion

    public float curBioDirMax {
        private set;
        get;
    }

    //the position of first unit in group drawing on screen 
    Vector3 startPosition;
    public Vector2 lastDragDis = Vector2.zero;
    public Vector3 startPos {
        private set {
            this.startPosition = value;
        }
        get {
            return startPosition;
        }
    }

    public MultiUnitGroup(MultiList multiList, int startSibIndex, bool isAddFromEnd = true) {
        this.multiList = multiList;
        this.startSibIndex = startSibIndex;
    }

    public void SetNewGroupStartPos(Vector3 startPos) {
        this.startPos = startPos;
    }

    public int TryAppendWhenDrag(Vector2 dragDis) {
        int isAppendSuccess = 0;
        if (IsLeftDrag(dragDis) || IsTopDrag(dragDis)) {
            //may need append unit to end, display next datas
            isAppendSuccess = TryAppendUnit() ? 1 : 0;
        }
        else {
            //may need append unit to head, display previous datas
            isAppendSuccess = TryAppendUnit(false) ? -1 : 0;
        }
        //lastDragDis.Set(dragDis.x, dragDis.y);
        return isAppendSuccess;
    }

    public int TryDeleteWhenDrag(Vector2 dragDis) {
        int isDeleteSuccess = 0;
        //when deleteing unit, strict to the principle that deleting 
        //only starts at both side of datas
        if (IsLeftDrag(dragDis) || IsTopDrag(dragDis)) {
            if (units[0].dataIndex == multiList.startIndex)
                isDeleteSuccess = TryRemoveUnit() ? 1 : 0;
        }
        else {
            if (units[units.Count - 1].dataIndex == multiList.endIndex)
                isDeleteSuccess = TryRemoveUnit(false) ? -1 : 0;
        }
        return isDeleteSuccess;
    }

    bool IsLeftDrag(Vector2 dragDis) {
        return dragDis.x - lastDragDis.x > 0;
    }

    bool IsTopDrag(Vector2 dragDis) {
        return dragDis.y - lastDragDis.y < 0;
    }

    bool TryAppendUnit(bool isAddFromEnd = true) {
        GameObject obj = isAddFromEnd ? multiList.GetNextDataTemplateObjRec() :
            multiList.GetPreDataTemplateObjRec();
        if (obj == null) return false;
        RectTransform trans = obj.transform as RectTransform;
        ///追加unit 时，需要判断当前该组是否可以容纳当前的unit
        if (IsNewTransBioBiggerThanMax(trans)) {
            FailAddNewUnit(obj);
            return false;
        }
        return TryAddUnit(obj, isAddFromEnd ? multiList.endIndex + 1 :
            multiList.startIndex - 1, isAddFromEnd);
    }

    bool TryRemoveUnit(bool isDeleteFromHead = true) {
        MultiUnit unit = isDeleteFromHead ? units[0] : units[units.Count - 1];
        if (IsUnitVisible(unit))
            return false;
        DestroyAndRemoveUnit(unit);
        return true;
    }

    public bool TryAddUnit(GameObject obj, int dataIndex, bool isAddFromEnd = true) {
        if (!IsNewUnitVisible(isAddFromEnd)) {
            FailAddNewUnit(obj);
            return false;
        }
        FillItemView(CreateAndAddUnit(obj, isAddFromEnd), dataIndex);
        return true;
    }

    /// <summary>
    /// 由于不能预知组的第二方向最大显示长度，需要在增加完毕后，修正绘制的起始位置
    /// </summary>
    public void FixStartPosWithCurrentBioMax() {
        if (isHorizontalFirst)
            startPosition.y += curBioDirMax;
        else
            startPosition.x -= curBioDirMax;
    }

    bool IsUnitVisible(MultiUnit unit) {
        if (unit.rectTrans.localPosition.x + unit.width < contentStartPos.x)//左侧隐藏
            return false;
        else if (unit.rectTrans.localPosition.x > contentStartPos.x + bound.width)//右侧隐藏
            return false;
        else if (unit.rectTrans.localPosition.y - unit.height > contentStartPos.y)//上方隐藏
            return false;
        else if (unit.rectTrans.localPosition.y < contentStartPos.y - bound.height)//下方隐藏
            return false;
        else
            return true;
    }

    /// 判断新增Unit 是否可见
    bool IsNewUnitVisible(bool isAddFromEnd = true) {
        //first item set to visible
        if (!hasUnits) return true;
        //TODO: 改进或废除 firstUnitPos 等接口。由于组内增加和删除Unit 时不会立即更新绘制位置，所以不能简单使用firstUnitPos 等接口
        if (isHorizontalFirst) {
            if (isAddFromEnd) {
                return GetLastUnitPosByStartPos().x + lastUnitTrans.rect.width + padding.x <
                    contentStartPos.x + bound.width;
            }
            else {
                return startPos.x - padding.x > contentStartPos.x;
            }
        }
        else {
            if (isAddFromEnd) {
                return GetLastUnitPosByStartPos().y - lastUnitTrans.rect.height - padding.y >
                    contentStartPos.y - bound.height;
            }
            else {
                return startPos.y + padding.y < contentStartPos.y;
            }
        }
    }

    public Vector3 GetLastUnitPosByStartPos() {
        Vector3 result = new Vector3(startPos.x, startPos.y, startPos.z);
        for (int i = 0; i < units.Count - 1; i++) {
            if (isHorizontalFirst)
                result.x += units[i].width;
            else
                result.y -= units[i].height;
        }
        if (isHorizontalFirst)
            result.x = result.x + padding.x * (units.Count - 1);
        else
            result.y = result.y - padding.y * (units.Count - 1);
        return result;
    }

    void FailAddNewUnit(GameObject obj) {
        multiList.RemoveTemplateObject(obj);
    }

    bool IsNewTransBioBiggerThanMax(RectTransform trans) {
        if (isHorizontalFirst)
            return trans.rect.height > curBioDirMax;
        else
            return trans.rect.width > curBioDirMax;
    }

    MultiUnit CreateAndAddUnit(GameObject obj, bool isAddFromEnd = true) {
        MultiUnit result = new MultiUnit(obj);
        CreateUnit(obj, isAddFromEnd);

        if (isAddFromEnd)
            units.Add(result);
        else {
            units.Insert(0, result);
            UpdateStartPosWhenAddUnitFromHead(result);
        }
        UpdateCurrentBioDirectionMaxLength(result.rectTrans);

        return result;
    }

    void UpdateStartPosWhenAddUnitFromHead(MultiUnit unit) {
        if (isHorizontalFirst)
            startPosition.x = startPosition.x - padding.x - unit.width;
        else
            startPosition.y = startPosition.y + padding.y + unit.height;
    }

    void DestroyAndRemoveUnit(MultiUnit unit, bool isRemoveFromHead = true) {
        units.Remove(unit);

        if (isRemoveFromHead)
            UpdateStartPosWhenRemoveUnitFromHead(unit);

        DestroyUnit(unit.gameObject);
    }

    void UpdateStartPosWhenRemoveUnitFromHead(MultiUnit unit) {
        if (isHorizontalFirst)
            startPosition.x = startPosition.x + unit.width + padding.x;
        else
            startPosition.y = startPosition.y - unit.height - padding.y;
    }

    void CreateUnit(GameObject obj, bool isAddFromEnd = true) {
        RectTransform trans = obj.transform as RectTransform;
        trans.SetParent(content, false);
        if (isAddFromEnd)
            trans.SetSiblingIndex(startSibIndex + units.Count);
        else
            trans.SetSiblingIndex(startSibIndex);
        trans.localScale = Vector3.one;
        obj.SetActive(true);
    }

    void DestroyUnit(GameObject obj) {
        multiList.RemoveTemplateObject(obj);
    }

    void FillItemView(MultiUnit unit, int dataIndex) {
        unit.SetDataIndex(dataIndex);
        ViewBase view = unit.gameObject.GetComponent<ViewBase>();
        view.FillView(multiList.datas[dataIndex]);
    }

    void UpdateCurrentBioDirectionMaxLength(RectTransform trans) {
        if (isHorizontalFirst) {
            if (trans.rect.height > curBioDirMax)
                curBioDirMax = trans.rect.height;
        }
        else {
            if (trans.rect.width > curBioDirMax)
                curBioDirMax = trans.rect.width;
        }
    }

    public void SetLastDragDis(Vector2 lastDragDis) {
        this.lastDragDis.Set(lastDragDis.x, lastDragDis.y);
    }
}
