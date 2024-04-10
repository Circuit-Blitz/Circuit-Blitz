using UnityEngine;
using UnityEngine.UI;

public class ScrollableTextList : MonoBehaviour
{
    [SerializeField] private RectTransform UsernameTag;
    [SerializeField] private RectTransform RT;
    
    public void Clear() {
        // Loop through and destroy all children (O_o)
        foreach (Transform child in RT)
        {
            Destroy(child.gameObject);
        }
        
        // Destroy is deferred to the end of the frame
        // Use DetachChildren to ensure that childCount is 0
        RT.DetachChildren();
    }

    public void AddText(string tagName, string text) {
        RectTransform tag = Instantiate(UsernameTag);
        tag.transform.SetParent(RT);
        tag.name = tagName;

        RT.sizeDelta = new Vector2(RT.sizeDelta.x, RT.childCount * 50 + 30);

        tag.offsetMin = new Vector2(0, 0);
        tag.offsetMax = new Vector2(0, (RT.childCount - 1) * -100 - 80);
        tag.sizeDelta = new Vector2(0, 50);
        tag.GetComponent<Text>().text = text;
    }

    public void SetText(string tagName, string text) {
        RT.Find(tagName).GetComponent<Text>().text = text;
    }
}