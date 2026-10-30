var m = UnityEngine.Object.FindFirstObjectByType<PotionBarManager>();
if (m == null) return "no manager";
var f = typeof(PotionBarManager).GetField("slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var arr = (PotionSlotView[])f.GetValue(m);
var parts = new System.Collections.Generic.List<string>();
parts.Add("slots=" + arr.Length);
for (int i = 0; i < arr.Length; i++)
{
    parts.Add(i + ": trigger=" + (arr[i].trigger == null ? "null" : arr[i].trigger.triggers.Count.ToString())
        + " frame=" + (arr[i].frame == null ? "null" : arr[i].frame.gameObject.name)
        + " label=" + (arr[i].nameLabel == null ? "null" : arr[i].nameLabel.text)
        + " color=" + (arr[i].frame == null ? "null" : arr[i].frame.color.ToString()));
}
return string.Join(" | ", parts);
