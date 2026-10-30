var m = UnityEngine.Object.FindFirstObjectByType<PotionBarManager>();
if (m == null) return "no manager";
m.SelectPotion(0);
return "selected=" + m.SelectedIndex + " potion=" + (m.SelectedPotion == null ? "null" : m.SelectedPotion.potionName);
