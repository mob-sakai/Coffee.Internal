using System.Collections;
using Coffee.Internal;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class EditorTests
{
    [Test]
    public void GetActualTexture()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tests/Editor/TestSprite.png");
        var texture = sprite.GetActualTexture();
        Debug.Log(texture);
    }

    [Test]
    public void GetActiveAtlas()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tests/Editor/TestSpriteAtlas.png");
        var spriteAtlas = sprite.GetActiveAtlas();
        Debug.Log(spriteAtlas);

    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
