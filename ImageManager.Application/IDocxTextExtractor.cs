namespace ImageManager.Application;

// Extracts the plain text body from a .docx file's bytes.
public interface IDocxTextExtractor
{
    string Extract(byte[] docx);
}
