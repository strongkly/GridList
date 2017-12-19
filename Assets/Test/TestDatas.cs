using UnityEngine;
using System.Collections;

public class TestDatas : IMultipleChoice {
    public int choice {
        get;
        private set;
    }

    public int dataIndex {
        get;
        private set;
    }

    public TestDatas(int choice, int dataIndex) {
        this.choice = choice;
        this.dataIndex = dataIndex;
    }

    public int GetChoice() {
        return choice;
    }
}
