using UnityEngine.UI;

public class TestListItem : ViewBase {
    Text text;
    public TestDatas data {
        private set;
        get;
    }

    public void Awake() {
        text = transform.GetChild(0).GetComponent<Text>();
    }

    public override void FillView(object data) {
        this.data = data as TestDatas;
        text.text = string.Format("s:{0}  i:{1}", this.data.choice, this.data.dataIndex);
        gameObject.name = string.Format("{0}dataIdx:{1}", gameObject.name, this.data.dataIndex);
    }
}

