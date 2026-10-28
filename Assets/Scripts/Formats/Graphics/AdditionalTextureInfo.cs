// Moved from Data/TextureData.cs (Unity-free, Formats assembly). The code is unchanged.
public class AdditionalTextureInfo
{
	public bool Write;
	public int Width;
	public int Height;
	public int AuxPalette;
	public int SizeA;
	public int SizeB;

	public AdditionalTextureInfo(bool write, int w, int h, int sa, int sb)
	{
		Write = write;
		Width = w;
		Height = h;
		SizeA = sa;
		SizeB = sb;
	}
}
