using System.Text;
using StyleBro.Migrate;

namespace StyleBro.Tests;

/// <summary>What trial 62 found in bro.</summary>
public sealed partial class MigrationTests
{
    // jbevain/mono.linq.expressions commits its Directory.Build.props as UTF-16 with a BOM and CRLF: init read it as UTF-8 and
    // wrote the preset version back as UTF-8 with LF, so git showed the whole file as changed (a binary diff).
    [Theory]
    [InlineData("utf-16")]
    [InlineData("utf-16BE")]
    [InlineData("utf-32")]
    public void EditFile_KeepsAUtf16Or32FilesEncodingAndLineEndings(string name)
    {
        var encoding = Encoding.GetEncoding(name);
        var path = Path.Combine(root, "Directory.Build.props");
        File.WriteAllText(path, "<Project>\r\n  <PropertyGroup>\r\n    <A>1</A>\r\n  </PropertyGroup>\r\n</Project>\r\n", encoding);
        string? seen = null;

        PreviewCommand.EditFile(path, text => Migration.SetPreset(seen = text, "1.0", "comment")!);

        Assert.StartsWith("<Project>\r\n", seen, StringComparison.Ordinal);
        Assert.Equal(encoding.GetPreamble(), File.ReadAllBytes(path).Take(encoding.GetPreamble().Length));
        Assert.Equal(
            "<Project>\r\n  <PropertyGroup>\r\n    <StyleBroPreset>1.0</StyleBroPreset>\r\n    <A>1</A>\r\n  </PropertyGroup>\r\n</Project>\r\n",
            File.ReadAllText(path));
    }
}
