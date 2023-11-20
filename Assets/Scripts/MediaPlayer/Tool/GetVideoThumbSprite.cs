using UnityEngine;
public static class GetVideoThumbSprite
{
    public static Sprite GetSprites(string name)
    {
        string path = "VideoThumb/Cover{0:G}";
        Sprite sprite = Resources.Load<Sprite>(string.Format(path,name)) as Sprite;
        return sprite;
    }
}
