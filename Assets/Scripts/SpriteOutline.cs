using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteOutline : MonoBehaviour {

	public int OutlineSize;
	public float OutlineFade;
	public bool UpdateOutline;
	public GameObject OutlineObject;
	public Material OutlineMaterial;

	private Sprite currentSprite;
	private Sprite outline;

	private SpriteRenderer sr;


	private int lastSize;
	private float lastFade;

	void Start() {
		//sr = GetComponent<SpriteRenderer>();
		//currentSprite = sr.sprite;
		//if (!UpdateOutline)
		//	CreateOutline(sr.sprite, OutlineSize, OutlineFade);
	}

	private void Update()
	{
		//if(UpdateOutline)
		//{
		//	Sprite nextSprite = sr.sprite;
		//	if(nextSprite != currentSprite)
		//	{
		//		currentSprite = nextSprite;
		//		CreateOutline(currentSprite, OutlineSize, OutlineFade);
		//	}
		//	if(lastSize != OutlineSize || lastFade != OutlineFade)
		//	{
		//		CreateOutline(currentSprite, OutlineSize, OutlineFade);
		//	}
		//	lastSize = OutlineSize;
		//	lastFade = OutlineFade;
		//}
		
	}

	public void CreateOutline()
	{
		CreateOutline(GetComponent<SpriteRenderer>().sprite);
	}

	void CreateOutline(Sprite sprite)
	{
		CreateOutline(sprite, OutlineSize, OutlineFade);
	}

	void CreateOutline(Sprite sprite, int size, float fade)
	{
		if (size > 10)
			size = 10;
		else if (size < 0)
			size = 1;
		if (fade <= 0.05f)
			fade = 0.25f;

		int width = (int)sprite.rect.width;
		int height = (int)sprite.rect.height;

		int outWidth = width * 2;
		int outHeight = height * 2;

		int dx = outWidth / 4;
		int dy = outHeight / 4;

		Rect rect = new Rect(0, 0, outWidth, outHeight);
		int sx = (int)sprite.rect.x;
		int sy = (int)sprite.rect.y;
		Texture2D texA = new Texture2D(outWidth, outHeight);
		for (int x = 0; x < outWidth; x++)
			for (int y = 0; y < outHeight; y++)
			{
				if (x >= dx && y >= dy && x < outWidth - dx && y < outHeight - dy)
					texA.SetPixel(x, y, sprite.texture.GetPixel(sx + x - dx, sy + y - dy));
				else
					texA.SetPixel(x, y, new Color(0, 0, 0, 0));
			}
		texA.Apply();
		Sprite copy = Sprite.Create(texA, rect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
		copy.texture.filterMode = FilterMode.Point;

		Texture2D texB = new Texture2D(outWidth, outHeight);
		for (int x = 0; x < outWidth; x++)
			for (int y = 0; y < outHeight; y++)
				texB.SetPixel(x, y, new Color(0, 0, 0, 0));
		texB.Apply();
		outline = Sprite.Create(texB, rect, new Vector2(0.5f, 0.5f), sprite.pixelsPerUnit);
		outline.texture.filterMode = FilterMode.Point;

		HashSet<Vector2Int> nextPixels = new HashSet<Vector2Int>();
		bool[,] donePixels = new bool[outWidth, outHeight];

		float currentAlpha = 1 - OutlineFade;

		Color first = new Color(1, 1, 1, currentAlpha);

		for (int x = 0; x < outWidth; x++)
		{
			for (int y = 0; y < outHeight; y++)
			{
				Color pixel = copy.texture.GetPixel(x, y);
				if (pixel.a == 0)
					continue;

				if (x > 0 && !donePixels[x - 1, y])
					SetOutlinePixel(outline, copy, x - 1, y, first, nextPixels, donePixels);

				if (x < outWidth - 1 && !donePixels[x + 1, y])
					SetOutlinePixel(outline, copy, x + 1, y, first, nextPixels, donePixels);

				if (y > 0 && !donePixels[x, y - 1])
					SetOutlinePixel(outline, copy, x, y - 1, first, nextPixels, donePixels);

				if (y < outHeight - 1 && !donePixels[x, y + 1])
					SetOutlinePixel(outline, copy, x, y + 1, first, nextPixels, donePixels);

				outline.texture.SetPixel(x, y, new Color(0, 0, 0, 0));
				donePixels[x, y] = true;
			}
		}
		while (size > 0)
		{
			size--;
			currentAlpha -= OutlineFade;
			Color currentColor = new Color(1, 1, 1, currentAlpha);
			HashSet<Vector2Int> currentPixels = new HashSet<Vector2Int>(nextPixels);
			nextPixels.Clear();
			foreach (var pixel in currentPixels)
			{
				if (pixel.x > 0 && !donePixels[pixel.x - 1, pixel.y])
					SetOutlinePixel(outline, copy, pixel.x - 1, pixel.y, currentColor, nextPixels, donePixels);

				if (pixel.x < outWidth - 1 && !donePixels[pixel.x + 1, pixel.y])
					SetOutlinePixel(outline, copy, pixel.x + 1, pixel.y, currentColor, nextPixels, donePixels);

				if (pixel.y > 0 && !donePixels[pixel.x, pixel.y - 1])
					SetOutlinePixel(outline, copy, pixel.x, pixel.y - 1, currentColor, nextPixels, donePixels);

				if (pixel.y < outWidth - 1 && !donePixels[pixel.x, pixel.y + 1])
					SetOutlinePixel(outline, copy, pixel.x, pixel.y + 1, currentColor, nextPixels, donePixels);
			}

		}
		outline.texture.Apply();
		ApplyOutline(outline);
	}

	private void ApplyOutline(Sprite outline)
	{
		SpriteRenderer outlinesr;
		if (!OutlineObject)
		{
			OutlineObject = new GameObject("Outline");
			OutlineObject.transform.parent = gameObject.transform;
			outlinesr = OutlineObject.AddComponent<SpriteRenderer>();
			if(OutlineMaterial)
				outlinesr.material = OutlineMaterial;
		}
		else
		{
			outlinesr = OutlineObject.GetComponent<SpriteRenderer>();
			if(!outlinesr)
			{
				outlinesr = OutlineObject.AddComponent<SpriteRenderer>();
				if (OutlineMaterial)
					outlinesr.material = OutlineMaterial;
			}
			outlinesr.sortingOrder = 2;
		}
		//Add material with shader to that
		outlinesr.sprite = outline;
		OutlineObject.transform.localPosition = Vector3.zero;
	}

	private void SetOutlinePixel(Sprite outline, Sprite copy, int x, int y, Color col, HashSet<Vector2Int> nextPixels, bool[,] donePixels)
	{
		Color pix = copy.texture.GetPixel(x, y);

		if (pix.a == 0)
		{
			Vector2Int C = new Vector2Int(x, y);
			outline.texture.SetPixel(x, y, col);
			if (!nextPixels.Contains(C))
				nextPixels.Add(C);
			donePixels[x, y] = true;
		}
	}
}
